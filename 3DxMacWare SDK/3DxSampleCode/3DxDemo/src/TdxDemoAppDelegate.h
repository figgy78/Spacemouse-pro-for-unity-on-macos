/*
 * 3DxDemoAppDelegate.h
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

#import <Cocoa/Cocoa.h>
#import "DemoView.h"

@interface TdxDemoAppDelegate : NSObject <NSApplicationDelegate>
{
  NSWindow *glWindow;

  NSThread *connexionThread;
  TDxMouseConnexion *connexion;
  TDxEventData *dataObject;

  NSLock *accessLock;
  BOOL eventDataArrived;

  DemoView *glView;
	NSTimer *renderTimer;
  
  BOOL Dom;
}

@property (assign) BOOL Dom;
@property (assign) IBOutlet NSWindow *glWindow;

-(IBAction)setDomAction:(id)sender;
-(void)updateDeviceData:(TDxEventData *)devData;

@end
