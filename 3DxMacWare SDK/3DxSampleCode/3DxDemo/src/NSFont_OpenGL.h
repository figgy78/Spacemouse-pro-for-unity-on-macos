/*
 * NSFont_OpenGL.h
 *
 * Extension / Category of NSFont for use with OpenGL
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
#import <OpenGL/gl.h>

@interface NSFont (withay_OpenGL)

+ (void) setOpenGLLogging:(BOOL)logEnabled;
- (BOOL) makeGLDisplayListFirst:(unichar)first count:(int)count base:(GLint)base;

@end
