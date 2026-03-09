/*
 * SpaceMouseBridge.c
 * Native Unity plugin bridging 3DconnexionClient.framework to Unity Editor on macOS.
 *
 * Axis layout per 3DxWareMac SDK (data ordered as):
 *   axis[0] = Tx  (Pan Right/Left)
 *   axis[1] = Tz  (Pan Up/Down)
 *   axis[2] = Ty  (Zoom / Forward-Back)
 *   axis[3] = Rx  (Tilt / Pitch)
 *   axis[4] = Rz  (Spin / Yaw)
 *   axis[5] = Ry  (Roll)
 *
 * Typical axis range: ±500. Maximum range: ±1024.
 *
 * Build: see build.sh
 */

#include <stdint.h>
#include <stdbool.h>
#include <string.h>
#include <wchar.h>   /* required by ConnexionClientCmdExport.h */
#include <3DconnexionClient/ConnexionClientAPI.h>

/* ── Weak imports ──────────────────────────────────────────────────────────────
 * Declaring the framework symbols as weak_import means the dylib loads even
 * when the 3Dconnexion driver is not installed. We check for NULL at runtime.
 */
extern int16_t SetConnexionHandlers(
    ConnexionMessageHandlerProc, ConnexionAddedHandlerProc,
    ConnexionRemovedHandlerProc, bool)
    __attribute__((weak_import));

extern void CleanupConnexionHandlers(void) __attribute__((weak_import));

extern uint16_t RegisterConnexionClient(uint32_t, uint8_t*, uint16_t, uint32_t)
    __attribute__((weak_import));

extern void UnregisterConnexionClient(uint16_t) __attribute__((weak_import));

extern void SetConnexionClientButtonMask(uint16_t, uint32_t) __attribute__((weak_import));

/* ── State ─────────────────────────────────────────────────────────────────── */
static uint16_t g_clientID  = 0;
static volatile int g_connected = 0;
static volatile int g_dirty     = 0;
static volatile int16_t  g_axis[6]   = {0, 0, 0, 0, 0, 0};
static volatile uint32_t g_buttons   = 0;

/* ── Internal callbacks ────────────────────────────────────────────────────── */

static void OnDeviceAdded(unsigned int productID)
{
    g_connected = 1;
}

static void OnDeviceRemoved(unsigned int productID)
{
    g_connected = 0;
    memset((void*)g_axis, 0, sizeof(g_axis));
    g_buttons = 0;
    g_dirty   = 0;
}

static void OnMessage(unsigned int productID, unsigned int messageType, void *messageArgument)
{
    if (messageType != kConnexionMsgDeviceState) return;

    ConnexionDeviceState *state = (ConnexionDeviceState *)messageArgument;

    /* Do NOT filter by state->client here.
     * With kConnexionClientWildcard some drivers send button events with a
     * different (or zero) client value than axis events, causing buttons to
     * be silently dropped when the filter is active. */

    switch (state->command)
    {
        case kConnexionCmdHandleAxis:
            for (int i = 0; i < 6; i++) g_axis[i] = state->axis[i];
            g_buttons = state->buttons;
            g_dirty = 1;
            break;

        case kConnexionCmdHandleButtons:
            g_buttons = state->buttons;
            g_dirty   = 1;
            break;

        default:
            /* Catch-all: some firmware versions carry button state in other
             * command types (e.g. app-specific events). Always capture it. */
            if (state->buttons != 0)
            {
                g_buttons = state->buttons;
                g_dirty   = 1;
            }
            break;
    }
}

/* ── Exported API ──────────────────────────────────────────────────────────── */

/*
 * SMB_Initialize
 * Returns 0 on success, negative on failure:
 *   -1  framework not installed (SetConnexionHandlers is NULL)
 *   -2  SetConnexionHandlers failed
 *   -3  RegisterConnexionClient returned 0 (driver not running?)
 * Safe to call multiple times; cleans up previous registration first.
 */
__attribute__((visibility("default")))
int SMB_Initialize(void)
{
    if (SetConnexionHandlers == NULL) return -1;

    /* Clean up any leftover registration (e.g., after a domain reload) */
    if (g_clientID != 0) {
        if (UnregisterConnexionClient != NULL) UnregisterConnexionClient(g_clientID);
        g_clientID = 0;
    }
    if (CleanupConnexionHandlers != NULL) CleanupConnexionHandlers();

    int16_t err = SetConnexionHandlers(OnMessage, OnDeviceAdded, OnDeviceRemoved, false);
    if (err != 0) return -2;

    /* Pascal string: first byte is length, followed by ASCII chars.
     * "Unity SpaceMouse" = 16 characters */
    static uint8_t kName[] = {
        16,
        'U','n','i','t','y',' ',
        'S','p','a','c','e','M','o','u','s','e'
    };

    g_clientID = RegisterConnexionClient(
        kConnexionClientWildcard,
        kName,
        kConnexionClientModeTakeOver,
        kConnexionMaskAll
    );

    if (g_clientID == 0) {
        if (CleanupConnexionHandlers != NULL) CleanupConnexionHandlers();
        return -3;
    }

    /* Enable all 32 buttons. kConnexionMaskAll only covers the first 8;
     * SetConnexionClientButtonMask (added in driver v10) is required for
     * buttons 9-32 on devices like the SpaceMouse Pro. */
    if (SetConnexionClientButtonMask != NULL)
        SetConnexionClientButtonMask(g_clientID, 0xFFFFFFFF);

    return 0;
}

/*
 * SMB_Cleanup
 * Unregisters from the driver. Call before domain reload or app shutdown.
 * All SDK calls must happen on the same thread as Initialize — this is
 * guaranteed because Unity's EditorApplication.quitting and
 * AssemblyReloadEvents.beforeAssemblyReload fire on the main thread.
 */
__attribute__((visibility("default")))
void SMB_Cleanup(void)
{
    if (g_clientID != 0 && UnregisterConnexionClient != NULL) {
        UnregisterConnexionClient(g_clientID);
        g_clientID = 0;
    }
    if (CleanupConnexionHandlers != NULL) CleanupConnexionHandlers();
    g_connected = 0;
    g_dirty     = 0;
    memset((void*)g_axis, 0, sizeof(g_axis));
    g_buttons = 0;
}

/*
 * SMB_Poll
 * Copies current device state into the output parameters.
 * Returns 1 if new data arrived since the last call, 0 otherwise.
 *
 * Axis semantics (Camera Mode per SDK):
 *   panX  = axis[0] Tx  +right / -left
 *   panY  = axis[1] Tz  sign depends on driver; typically -up / +down
 *   zoom  = axis[2] Ty  +forward(zoom in) / -backward(zoom out)
 *   tilt  = axis[3] Rx  pitch
 *   spin  = axis[4] Rz  yaw
 *   roll  = axis[5] Ry  roll
 */
__attribute__((visibility("default")))
int SMB_Poll(
    float *panX, float *panY, float *zoom,
    float *tilt, float *spin, float *roll,
    uint32_t *buttons)
{
    *panX    = (float)g_axis[0];
    *panY    = (float)g_axis[1];
    *zoom    = (float)g_axis[2];
    *tilt    = (float)g_axis[3];
    *spin    = (float)g_axis[4];
    *roll    = (float)g_axis[5];
    *buttons = g_buttons;

    int was_dirty = g_dirty;
    g_dirty = 0;
    return was_dirty;
}

/*
 * SMB_DeviceConnected
 * Returns 1 if a device was seen via AddedHandler, 0 otherwise.
 */
__attribute__((visibility("default")))
int SMB_DeviceConnected(void)
{
    return g_connected;
}
