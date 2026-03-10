/*
 * 3DxClientConnection.m
 *
 * Opens the conection to 3DxWareMac / the devive
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

#import <3DConnexionClient/ConnexionClient.h>
#import <3DConnexionClient/ConnexionClientAPI.h>

#import "3DxEventData.h"
#import "3DxClientConnection.h"

#define k3DxApplicationID3DDemo 'WCBc'
#define k3DxApplicationName3DDemo  0
//==============================================================================
// Make the linker happy for the framework check (see link below for more info)
// http://developer.apple.com/documentation/MacOSX/Conceptual/BPFrameworks/Concepts/WeakLinking.html

extern int16_t SetConnexionHandlers(ConnexionMessageHandlerProc messageHandler, ConnexionAddedHandlerProc addedHandler, ConnexionRemovedHandlerProc removedHandler, bool useSeparateThread) __attribute__((weak_import));

//extern OSErr InstallConnexionHandlers() __attribute__((weak_import));

void messageHandler3DMouse(io_connect_t connection, natural_t messageType, void *messageArgument);

// Quick & dirty way to access our class variables from the C callback
TDxMouseConnexion	*gConnexionTest = 0L;

@interface TDxMouseConnexion (private_methods)
-(void)updateData:(TDxEventData *)data;
@end

//==============================================================================
@implementation TDxMouseConnexion

@synthesize fConnexionClientID;

//==============================================================================
-(id)initConnexion
{
  if (SetConnexionHandlers != NULL)
	{
    if (self = [super init])
    {
      accessLock = [[NSLock alloc] init];
      dataObj = [[TDxEventData alloc] init];
      gConnexionTest = self;

      return self;
    }
  }
  return nil;
}

-(void)dealloc
{
  [dataObj release];
  [super dealloc];
}

-(UInt16)connection
{
  return fConnexionClientID;
}

-(void)updateData:(TDxEventData *)data
{
  //NSLog(@"clientconnexion updateData %@", data);
  [accessLock lock];
  [dataObj setValuesFromEventData:data];
  [accessLock unlock];

  [[NSNotificationCenter defaultCenter] postNotificationName:
   [NSString stringWithFormat:@"TDxMouseEventArrived_%@", [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleName"]]
                                                      object:self];
}


-(void)getData:(TDxEventData *)data
{
  [accessLock lock];
  [data setValuesFromEventData:dataObj];
  [accessLock unlock];
}

-(void)connexionThreadMain:(id)argumentObject
{ 
  NSAutoreleasePool *pool = [[NSAutoreleasePool alloc] init];
  //NSLog(@"clientconnexion connexionThreadMain %@", [argumentObject className]);

  NSRunLoop* runLoop = [NSRunLoop currentRunLoop];
  
  [self start3DMouse];
  while ([[NSThread currentThread] isCancelled] == NO)//(moreWorkToDo && !exitNow)
  {
    // Run the run loop but timeout immediately if the input source isn't waiting to fire.
    [runLoop runMode:NSDefaultRunLoopMode beforeDate:[NSDate distantFuture]];
  }

  [pool drain];
}


- (void)start3DMouse
{
	OSErr	error;
  //NSLog(@"start3DMouse");

	// Quick hack to keep the sample as simple as possible, don't use in shipping code
	gConnexionTest = self;

	// Make sure the framework is installed
	if (SetConnexionHandlers != NULL)
	{
		// Install message handler and register our client
		error = SetConnexionHandlers(messageHandler3DMouse, 0L, 0L, NO);
		
		// This takes over system-wide
    fConnexionClientID = RegisterConnexionClient(k3DxApplicationID3DDemo, k3DxApplicationName3DDemo, kConnexionClientModeTakeOver, kConnexionMaskAxis);
  }
}

- (void)terminate3DMouse
{
	// Make sure the framework is installed
	if (SetConnexionHandlers != NULL)
	{
		//Unregister our client and clean up all handlers
		if (fConnexionClientID) UnregisterConnexionClient(fConnexionClientID);
		CleanupConnexionHandlers();
	}
  [[NSThread currentThread] cancel];
}

-(int)deviceID
{
    SInt32 result;
    
    if (ConnexionControl(kConnexionCtlGetDeviceID, 0, &result))
        return 0;
    
    result = (result & 0xFFFF);
    
    return (result & 0xFFFF);
}

//==============================================================================

int getButton(NSUInteger buttonMask)
{
  int button = 0;

  for (int i = 0; i < 32; i++)
  {
    if (buttonMask & (1 << i))
      button += i;
  }
  return button + 1;
}


void messageHandler3DMouse(io_connect_t connection, natural_t messageType, void *messageArgument)
{
	ConnexionDeviceState		*state;  

	switch(messageType)
  {
		case kConnexionMsgDeviceState:
			state = (ConnexionDeviceState*)messageArgument;
      if (state->client == gConnexionTest.fConnexionClientID)
      {
        // decipher what command/event is being reported by the driver
        switch (state->command)
        {
          case kConnexionCmdHandleAxis:
            {
             //NSLog(@"Device Axes: %i %i %i / %i %i %i",
             //       state->axis[0], state->axis[1], state->axis[2],
             //       state->axis[3], state->axis[4], state->axis[5]);

              TDxEventData *data = [[TDxEventData alloc] init];

              data.tx = state->axis[0];
              data.ty = state->axis[1];
              data.tz = state->axis[2];
              data.rx = state->axis[3];
              data.ry = state->axis[4];
              data.rz = state->axis[5];

              [gConnexionTest updateData:data];
              [data release];
            }
            break;
          case kConnexionCmdHandleButtons:
            // we ignore HID buttons
            break;
          case kConnexionCmdAppSpecific:
            {
              if (state->value > 0) // a command is always > 0, otherwise there is something wrong an we shouldn't do anything at all
              {
                TDxEventData *data = [[TDxEventData alloc] init];
      
                switch (state->value)
                {
                  case 1: // Menu
                    (void)ConnexionClientControl(0, kConnexionCtlOpenPrefPane, 1, NULL);
                    break;
                  case 2: // Fit
                    data.button = kButtonFit;
                    [gConnexionTest updateData:data];
                    break;
                  case 3: // Just as a demo we have a "self implemented "Toggle Dominant"
                    data.button = kButtonDominant;
                    [gConnexionTest updateData:data];
                    break;
                  default:
                    break;
                }
                // release the event date
                [data release];
              }
            }
            break;
          case kConnexionCmdHandleRawData:
            // we also do not work on raw device data
            break;
          default:
            break;
        }
      }
      break;
		case kConnexionMsgPrefsChanged:
      break;
		default:
			// other messageTypes can happen and should be ignored
			break;
	}
}
@end
