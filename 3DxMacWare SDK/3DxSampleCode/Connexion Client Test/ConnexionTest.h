/*
 * ConnexionTest.h
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

#import <Cocoa/Cocoa.h>

//==============================================================================
@interface ConnexionTest : NSObject
//==============================================================================
{
    IBOutlet id		mtMainWindow;
    IBOutlet id		mtValueButtons;
    IBOutlet id		mtValueX;
    IBOutlet id		mtValueY;
    IBOutlet id		mtValueZ;
    IBOutlet id		mtValueRx;
    IBOutlet id		mtValueRy;
    IBOutlet id		mtValueRz;
	IBOutlet id		mtFWNotFound;
	IBOutlet id		mtValueSignature;
	IBOutlet id		mtValueAppName;
	IBOutlet id		mtValueDeviceID;
	IBOutlet id		mtValueMainSpeed;
	
	UInt16			fConnexionClientID;
}
//==============================================================================

- (void)		awakeFromNib;
- (void)		windowWillClose:	(NSNotification*)notification;

- (IBAction)	buttonDominant:		(id)sender;
- (IBAction)	buttonXYZOnly:		(id)sender;
- (IBAction)	buttonRxRyRzOnly:	(id)sender;
- (IBAction)	buttonDefault:		(id)sender;
- (IBAction)	button3dxSettings:	(id)sender;
- (IBAction)	buttonChangeLabels:	(id)sender;

//==============================================================================
@end
//==============================================================================
