using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace SpaceMousePro
{
    /// <summary>
    /// Low-level interface to the SpaceMouse native plugin (libSpaceMouseBridge.dylib).
    /// Wraps 3DconnexionClient.framework via P/Invoke.
    ///
    /// Raw axis layout (per 3DxWareMac SDK, data ordered as):
    ///   RawAxis[0] = Tx  — Right / Left
    ///   RawAxis[1] = Ty  — Zoom In / Out
    ///   RawAxis[2] = Tz  — Up / Down
    ///   RawAxis[3] = Rx  — Tilt (Pitch)
    ///   RawAxis[4] = Ry  — Roll
    ///   RawAxis[5] = Rz  — Spin (Yaw)
    ///
    /// Values are normalized: raw ±500 → ±1.0 (driver range ≈ ±500 at normal speed).
    /// Use SpaceMouseSettings.GetChannel() / GetPanX() etc. for remapped + inverted values.
    /// </summary>
    [InitializeOnLoad]
    public static class SpaceMouseDevice
    {
        // Raw ÷ 500 normalises typical speed to ±1.
        const float AxisScale = 1f / 500f;

        static int     _initResult = -99;
        static float[] _rawAxes   = new float[6];
        static uint    _buttons;

        // ── Raw axis names (for UI) ──────────────────────────────────────────

        public static readonly string[] AxisNames = {
            "0 · Tx — Right / Left",
            "1 · Ty — Zoom In / Out",
            "2 · Tz — Up / Down",
            "3 · Rx — Tilt (Pitch)",
            "4 · Ry — Roll",
            "5 · Rz — Spin (Yaw)",
        };

        // ── Properties ──────────────────────────────────────────────────────

        /// <summary>True if the native plugin loaded and the framework is present.</summary>
        public static bool IsAvailable => _initResult == 0;

        /// <summary>True if a SpaceMouse is physically connected.</summary>
        public static bool IsConnected => IsAvailable && SMB_DeviceConnected() != 0;

        /// <summary>Raw normalised value for device axis index 0–5. Not remapped or inverted.</summary>
        public static float GetRawAxis(int index) =>
            (index >= 0 && index < 6) ? _rawAxes[index] : 0f;

        /// <summary>Bitmask of all 32 buttons on the device.</summary>
        public static uint Buttons => _buttons;

        /// <summary>Returns true if the given button index (0-based) is currently pressed.</summary>
        public static bool GetButton(int index) =>
            index >= 0 && index < 32 && (_buttons & (1u << index)) != 0;

        // ── Lifecycle ────────────────────────────────────────────────────────

        static SpaceMouseDevice()
        {
            Initialize();
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
            EditorApplication.quitting               += Cleanup;
        }

        static void Initialize()
        {
            _initResult = SMB_Initialize();
            if (_initResult == 0)
            {
                Debug.Log("[SpaceMouse] Initialized. Device " +
                          (SMB_DeviceConnected() != 0 ? "connected." : "not yet detected."));
            }
            else
            {
                string reason = _initResult switch
                {
                    -1 => "3DconnexionClient.framework not found — is 3DxWareMac installed?",
                    -2 => "SetConnexionHandlers failed.",
                    -3 => "RegisterConnexionClient returned 0 — is the 3Dconnexion driver running?",
                    _  => $"Unknown error ({_initResult})."
                };
                Debug.LogWarning($"[SpaceMouse] Init failed: {reason}");
            }
        }

        static void Cleanup()
        {
            if (_initResult == 0)
            {
                SMB_Cleanup();
                Debug.Log("[SpaceMouse] Cleaned up.");
            }
            _initResult = -99;
        }

        // ── Poll ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the latest device state. Returns true if new data arrived since
        /// the last call. Should be called once per editor update.
        /// </summary>
        public static bool Poll()
        {
            if (!IsAvailable) return false;

            int dirty = SMB_Poll(
                out float a0, out float a1, out float a2,
                out float a3, out float a4, out float a5,
                out uint buttons);

            _rawAxes[0] = a0 * AxisScale;
            _rawAxes[1] = a1 * AxisScale;
            _rawAxes[2] = a2 * AxisScale;
            _rawAxes[3] = a3 * AxisScale;
            _rawAxes[4] = a4 * AxisScale;
            _rawAxes[5] = a5 * AxisScale;
            _buttons    = buttons;

            return dirty != 0;
        }

        // ── P/Invoke ─────────────────────────────────────────────────────────
        // Unity on macOS searches for 'libSpaceMouseBridge.dylib' in Assets/Plugins/macOS/

        [DllImport("SpaceMouseBridge")]
        static extern int SMB_Initialize();

        [DllImport("SpaceMouseBridge")]
        static extern void SMB_Cleanup();

        [DllImport("SpaceMouseBridge")]
        static extern int SMB_Poll(
            out float a0, out float a1, out float a2,
            out float a3, out float a4, out float a5,
            out uint buttons);

        [DllImport("SpaceMouseBridge")]
        static extern int SMB_DeviceConnected();
    }
}
