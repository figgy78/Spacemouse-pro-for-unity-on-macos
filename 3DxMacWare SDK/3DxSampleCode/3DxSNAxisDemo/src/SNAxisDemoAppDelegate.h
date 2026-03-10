/*
 * SNAxisDemoAppDelegate.h
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

@interface SNAxisDemoAppDelegate : NSObject <NSApplicationDelegate>
{
  NSWindow *window;

  NSThread *connexionThread;
  TDxMouseConnexion *connexion;
  TDxEventData *dataObject;

  NSLock *accessLock;
  BOOL eventDataArrived;

  NSTextField *X;
  NSTextField *Y;
  NSTextField *Z;
  NSTextField *RX;
  NSTextField *RY;
  NSTextField *RZ;

  NSButton *buttonIndicator;

  BOOL Dom;
}

-(IBAction)setDomAction:(id)sender;

@property (assign) BOOL Dom;

@property (assign) IBOutlet NSWindow *window;
@property (assign) IBOutlet NSTextField *X;
@property (assign) IBOutlet NSTextField *Y;
@property (assign) IBOutlet NSTextField *Z;
@property (assign) IBOutlet NSTextField *RX;
@property (assign) IBOutlet NSTextField *RY;
@property (assign) IBOutlet NSTextField *RZ;
@property (assign) IBOutlet NSButton *buttonIndicator;

-(void)updateDeviceData:(TDxEventData *)devData;

@end
