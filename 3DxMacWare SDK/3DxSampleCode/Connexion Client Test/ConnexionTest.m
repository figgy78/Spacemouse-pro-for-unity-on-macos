/*
 * ConnexionTest.m
 *
 * Opens the conection to 3DxWareMac / the devive and provides a simple UI
 *
 *
 * Copyright notice:
 * (c) 3Dconnexion. All rights reserved.
 *
 * This file and source code are an integral part of the "3Dconnexion
 * Software Developer Kit", including all accompanying documentation,
 * and is protected by intellectual property laws. All use of the
 * 3Dconnexion Software Developer Kit is subject to the License
 * Agreement found in the "LicenseAgreementSDK.txt" file.
 * All rights not expressly granted by 3Dconnexion are reserved.
 */

//==============================================================================

#import <3DconnexionClient/ConnexionClientAPI.h>
#import "ConnexionTest.h"

//==============================================================================

#define TEST_WITH_SEPARATE_THREAD	false		// set to true or false
#define TEST_HANDLER_DELAY_IN_MS	200			// set to 0 to remove delay

//==============================================================================
// Make the linker happy for the framework check (see link below for more info)
// http://developer.apple.com/documentation/MacOSX/Conceptual/BPFrameworks/Concepts/WeakLinking.html

extern int16_t SetConnexionHandlers(ConnexionMessageHandlerProc messageHandler, ConnexionAddedHandlerProc addedHandler, ConnexionRemovedHandlerProc removedHandler, bool useSeparateThread) __attribute__((weak_import));

UInt16			clientID;

//==============================================================================
// Quick & dirty way to access our class variables from the C callback

ConnexionTest	*gConnexionTest = 0L;

//==============================================================================

static void	TestDeviceAddedHandler		(unsigned int connection);
static void	TestDeviceRemovedHandler	(unsigned int connection);
static void	TestMessageHandler			(unsigned int connection, natural_t messageType, void *messageArgument);

//==============================================================================
@implementation ConnexionTest
//==============================================================================

- (void) awakeFromNib
{
	OSErr	error;
	
	// Quick hack to keep the sample as simple as possible, don't use in shipping code
	gConnexionTest = self;
	
	// Make sure the framework is installed
	if(SetConnexionHandlers != NULL)
	{
		// Install message handler and register our client
		error = SetConnexionHandlers(TestMessageHandler, TestDeviceAddedHandler, TestDeviceRemovedHandler, TEST_WITH_SEPARATE_THREAD);
		
		// Either use this to take over in our application only...
//  fConnexionClientID = RegisterConnexionClient('MCTt', "\pConnexion Client Test", kConnexionClientModeTakeOver, kConnexionMaskAll);

		// ...or use this to take over system-wide
		fConnexionClientID = RegisterConnexionClient(kConnexionClientWildcard, NULL, kConnexionClientModeTakeOver, kConnexionMaskAll);
		
		// A separate API call is required to capture buttons beyond the first 8
		SetConnexionClientButtonMask(fConnexionClientID, kConnexionMaskAllButtons);
		
		// Remove warning message about the framework not being available
		[mtFWNotFound removeFromSuperview];
	}
}

//==============================================================================

- (void) windowWillClose: (NSNotification*)notification
{
	// Make sure the framework is installed
	if(InstallConnexionHandlers != NULL)
	{
		// Unregister our client and clean up all handlers
		if(fConnexionClientID) UnregisterConnexionClient(fConnexionClientID);
		CleanupConnexionHandlers();
	}
}

//==============================================================================

- (IBAction) buttonDominant: (id)sender
{
	(void)ConnexionClientControl(fConnexionClientID, kConnexionCtlSetSwitches, kConnexionSwitchDominant | kConnexionSwitchEnableAll, NULL);
}

//==============================================================================

- (IBAction) buttonRxRyRzOnly: (id)sender
{
	(void)ConnexionClientControl(fConnexionClientID, kConnexionCtlSetSwitches, kConnexionSwitchEnableRot, NULL);
}

//==============================================================================

- (IBAction) buttonXYZOnly: (id)sender
{
	(void)ConnexionClientControl(fConnexionClientID, kConnexionCtlSetSwitches, kConnexionSwitchEnableTrans, NULL);
}

//==============================================================================

- (IBAction) buttonDefault: (id)sender
{
	(void)ConnexionClientControl(fConnexionClientID, kConnexionCtlSetSwitches, kConnexionSwitchesDisabled, NULL);
}

//==============================================================================

- (IBAction) button3dxSettings: (id)sender
{
	(void)ConnexionClientControl(fConnexionClientID, kConnexionCtlOpenPrefPane, 0, NULL);
}

//==============================================================================

- (IBAction) buttonChangeLabels: (id)sender
{
	static UInt8 *labels = NULL;
	UInt8 *labels1 = (UInt8*)"\0\0\0\0\0\0\0\0\0\0\0\0Test 1\0Test 2\0Test 3\0Test 4\0Test 5\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
	UInt8 *labels2 = (UInt8*)"\0\0\0\0\0\0\0\0\0\0\0\0Very Long Function Name 1\0Very Long Function Name 2\0Very Long Function Name 3\0Very Long Function Name 4\0Very Long Function Name 5\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";
	
	UInt16 size = (labels == labels1 ? 157 : 62);
	labels = (labels == labels1 ? labels2 : labels1);
	(void)ConnexionSetButtonLabels(labels, size);
}

//==============================================================================

static void TestDeviceAddedHandler(unsigned int connection)
{
	NSLog(@"Device added %x", (int)connection);
}

//==============================================================================

static void TestDeviceRemovedHandler(unsigned int connection)
{
	NSLog(@"Device removed %x", (int)connection);
}

//==============================================================================

static void TestMessageHandler(unsigned int connection, natural_t messageType, void *messageArgument)
{
  static ConnexionDeviceState	lastState;
  ConnexionDeviceState		*state;
  int32_t						vidPid;
  UInt32						signature;
  NSString					*string;
  ConnexionDevicePrefs		prefs;
  OSErr						error;

  switch(messageType)
  {
    case kConnexionMsgDeviceState:
      state = (ConnexionDeviceState*)messageArgument;
      if(state->client == gConnexionTest->fConnexionClientID)
      {
        if(state->axis[0] != lastState.axis[0])	[gConnexionTest->mtValueX		setStringValue: [NSString stringWithFormat: @"%d", (int)state->axis[0]]];
        if(state->axis[1] != lastState.axis[1])	[gConnexionTest->mtValueY		setStringValue: [NSString stringWithFormat: @"%d", (int)state->axis[1]]];
        if(state->axis[2] != lastState.axis[2])	[gConnexionTest->mtValueZ		setStringValue: [NSString stringWithFormat: @"%d", (int)state->axis[2]]];
        if(state->axis[3] != lastState.axis[3])	[gConnexionTest->mtValueRx		setStringValue: [NSString stringWithFormat: @"%d", (int)state->axis[3]]];
        if(state->axis[4] != lastState.axis[4])	[gConnexionTest->mtValueRy		setStringValue: [NSString stringWithFormat: @"%d", (int)state->axis[4]]];
        if(state->axis[5] != lastState.axis[5])	[gConnexionTest->mtValueRz		setStringValue: [NSString stringWithFormat: @"%d", (int)state->axis[5]]];
        NSLog(@"Motion state = %i %i %i / %i %i %i", state->axis[0], state->axis[1], state->axis[2], state->axis[3], state->axis[4], state->axis[5]);

        if(state->buttons != lastState.buttons)
        {
          [gConnexionTest->mtValueButtons	setStringValue: [NSString stringWithFormat: @"%08x", (int)state->buttons]];
          NSLog(@"Button state = %08x", (int)state->buttons);
        }
        memmove(&lastState, state, (long)sizeof(ConnexionDeviceState));
      }
      break;
      
    case kConnexionMsgPrefsChanged:
      signature = (UInt32)((long)messageArgument); // note that 4-byte values are passed by value, not by pointer
      if(signature < 10) string = [NSString stringWithFormat: @"%d", (unsigned int)signature];
      else string = [NSString stringWithFormat: @"%c%c%c%c", (int)(signature >> 24) & 0xFFU, (int)(signature >> 16) & 0xFFU, (int)(signature >> 8) & 0xFFU, (int)signature & 0xFFU];
      [gConnexionTest->mtValueSignature setStringValue: string];
      (void)ConnexionControl(kConnexionCtlGetDeviceID, 0, &vidPid);
      error = ConnexionGetCurrentDevicePrefs(kDevID_AnyDevice, &prefs);
      if(error == noErr)
      {
        NSString *appName = [[[NSString  alloc] initWithBytes: (char*)&prefs.appName[1] length: prefs.appName[0] encoding: NSMacOSRomanStringEncoding] autorelease];
        
        NSLog(@"prefs-ver: %x / %i, %i, - %x %i", prefs.version, prefs.version, prefs.deviceID, vidPid, vidPid);
        [gConnexionTest->mtValueAppName setStringValue: appName];
        [gConnexionTest->mtValueDeviceID setStringValue: [NSString stringWithFormat: @"%d (%04x/%04x)", prefs.deviceID, (int)(vidPid >> 16), (int)(vidPid & 0xFFFF)]];
        [gConnexionTest->mtValueMainSpeed setStringValue: [NSString stringWithFormat: @"%d", prefs.mainSpeed]];
      }
      else
      {
        [gConnexionTest->mtValueAppName setStringValue: [NSString stringWithFormat: @"(error %d)", error]];
        [gConnexionTest->mtValueDeviceID setStringValue: @"-"];
        [gConnexionTest->mtValueMainSpeed setStringValue: @"-"];
      }
			break;

    default:
      // other messageTypes can happen and should be ignored
      break;
	}
  
#if TEST_HANDLER_DELAY_IN_MS
	usleep(TEST_HANDLER_DELAY_IN_MS * 1000);
#endif
}

//==============================================================================
@end
//==============================================================================
