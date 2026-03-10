/*
 * 3DxDemoAppDelegate.m
 *
 * Application delegate
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

#import "3DxEventData.h"
#import "3DxClientConnection.h"
#import "TdxDemoAppDelegate.h"


@implementation TdxDemoAppDelegate

@synthesize glWindow;
@synthesize Dom;

-(void)tdxNotificationAction:(NSNotification *)aNotification
{
  if (!eventDataArrived)
  {
    eventDataArrived = YES;
    [connexion getData:dataObject];

    [self performSelectorOnMainThread:@selector(updateDeviceData:) withObject:dataObject waitUntilDone:NO];
  }
}

-(void)applicationDidFinishLaunching:(NSNotification *)aNotification
{
  dataObject = [[TDxEventData alloc] init];
  eventDataArrived = NO;
  connexion = [[TDxMouseConnexion alloc] initConnexion];
  
   
  [NSApp setDelegate:(id) self];   // We want delegate notifications
  renderTimer = nil;
  
  glView = [[DemoView alloc] initWithFrame:[glWindow frame]
                                 colorBits:32 depthBits:32 fullscreen:FALSE];
  
  if (glView != nil)
  {
    [glWindow setContentView:glView];
    [glWindow makeKeyAndOrderFront:self];
    //[self setupRenderTimer];
  }
  else
    [self createFailed];
  
  
  if (connexion)
  {
    //NSLog(@"-applicationDidFinishLaunching, connecting to 3D Mouse. Connection");
    //NSLog(@"-applicationDidFinishLaunching...%p", [NSThread currentThread]);

    [[NSNotificationCenter defaultCenter] addObserver:self
                                             selector:@selector(tdxNotificationAction:)
                                                 name:[NSString stringWithFormat:@"TDxMouseEventArrived_%@", [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleName"]]
                                               object:connexion];
    
   
    // now activate the device(thread)
    connexionThread = [[NSThread alloc] initWithTarget:connexion selector:@selector(connexionThreadMain:) object:self];
    [connexionThread start];
  }
  else
  {
    NSLog(@"Error: Failed to init 3DconnexionClient.framework!");
    [NSApp terminate:self];
  }
}


/*
 * Setup timer to update the OpenGL view.
 */
- (void) setupRenderTimer
{
  NSTimeInterval timeInterval = 0.005;
  
  renderTimer = [[NSTimer scheduledTimerWithTimeInterval:timeInterval
                                                  target:self
                                                selector:@selector(updateGLView:)
                                                userInfo:nil repeats:YES] retain];
  [[NSRunLoop currentRunLoop] addTimer:renderTimer
                               forMode:NSEventTrackingRunLoopMode];
  [[NSRunLoop currentRunLoop] addTimer:renderTimer
                               forMode:NSModalPanelRunLoopMode];
}


/*
 * Called by the rendering timer.
 */
- (void) updateGLView:(NSTimer *)timer
{
  if (glView != nil)
    [glView drawRect:[glView frame]];
}


// Called if we fail to create a valid OpenGL view
- (void) createFailed
{
  NSWindow *infoWindow;
  
  infoWindow = NSGetCriticalAlertPanel(@"Initialization failed",
                                       @"Failed to initialize OpenGL",
                                       @"OK", nil, nil);
  [NSApp runModalForWindow:infoWindow];
  [infoWindow close];
  [NSApp terminate:self];
}

- (void)applicationWillTerminate:(NSNotification *)notification
{
	// End connection to 3dmouse driver
	if (connexion)
	{
		[connexion terminate3DMouse];
		[connexion release];
	}
}


-(BOOL)applicationShouldTerminateAfterLastWindowClosed:(NSApplication *)theApplication
{
  return YES;
}


- (void)applicationDidResignActive:(NSNotification *)aNotification
{
}


- (void)applicationDidBecomeActive:(NSNotification *)aNotification
{
}

#pragma mark -
#pragma mark event updater
-(void)filterDominantAxis:(TDxEventData *)event
{
  int high = 0;
  
  int axisArray[6] = { event.tx, event.ty, event.tz, event.rx, event.ry, event.rz };
  for(int i = 0; i < 6; i++)
  {
    if (abs(axisArray[high]) < abs(axisArray[i]))
      high = i;
  }
  
  switch(high)
  {
    case 0:
      event.ty = event.tz = event.rx = event.ry = event.rz = 0;
      break;
    case 1:
      event.tx = event.tz = event.rx = event.ry = event.rz = 0;
      break;
    case 2:
      event.tx = event.ty = event.rx = event.ry = event.rz = 0;
      break;
      
    case 3:
      event.tx = event.ty = event.tz = event.ry = event.rz = 0;
      break;
    case 4:
      event.tx = event.ty = event.tz = event.rx = event.rz = 0;
      break;
    case 5:
      event.tx = event.ty = event.tz = event.rx = event.ry = 0;
      break;
  }

}

#define kFactor 0.001

-(void)updateDeviceData:(TDxEventData *)devData
{
  [accessLock lock];

  TDxEventData *mydata = [[TDxEventData alloc] initWithTDxEventData:devData];
  
  if ([mydata button] == kButtonFit)
  {
    [glView reset];
  }
  else if ([mydata button] == kButtonDominant)
  {
    if (Dom == YES)
    {
      Dom = NO;
    }
    else
    {
      Dom = YES;
    }
  }
  
  if (Dom)
    [self filterDominantAxis:mydata];

  [glView setXRot:([mydata rx] * kFactor) YRot:-([mydata rz] * kFactor) ZRot:([mydata ry] * kFactor)];
  [glView setXt:([mydata tx] * kFactor) Yt:-([mydata tz] * kFactor) Zt:([mydata ty] * kFactor)];
  [mydata release];
  
  [accessLock unlock];
  [self updateGLView:nil];
  eventDataArrived = NO;
}

- (IBAction)setDomAction:(id)sender
{
	if (self.Dom == YES)
		self.Dom = NO;
	else
		self.Dom = YES;
}

@end
