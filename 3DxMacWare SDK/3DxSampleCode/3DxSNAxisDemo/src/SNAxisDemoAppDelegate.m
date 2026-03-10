/*
 * SNAxisDemoAppDelegate.m
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
#import "SNAxisDemoAppDelegate.h"


@implementation SNAxisDemoAppDelegate

@synthesize window;

@synthesize X;
@synthesize Y;
@synthesize Z;
@synthesize RX;
@synthesize RY;
@synthesize RZ;
@synthesize buttonIndicator;
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

-(void)updateDeviceData:(TDxEventData *)devData
{
  [accessLock lock];

  TDxEventData *mydata = [[TDxEventData alloc] initWithTDxEventData:devData];
  
  if (Dom)
    [self filterDominantAxis:mydata];

  X.intValue  = [mydata tx];
  Y.intValue  = [mydata ty];
  Z.intValue  = [mydata tz];
  RX.intValue = [mydata rx];
  RY.intValue = [mydata ry];
  RZ.intValue = [mydata rz];
  
  // clear out the button-indication
  [buttonIndicator setTitle:@"Button -"];
  [buttonIndicator setEnabled:NO];
  
  if (mydata.button > 0)
  {
    [buttonIndicator setTitle:[NSString stringWithFormat:@"Button %i", mydata.button]];
    [buttonIndicator setEnabled:YES];
  }

  [mydata release];
  
  [accessLock unlock];
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
