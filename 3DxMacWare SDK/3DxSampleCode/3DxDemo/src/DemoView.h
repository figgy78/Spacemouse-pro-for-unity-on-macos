/*
 * DemoView.h
 *
 * Header of subclassed NSOpenGLView
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

@interface DemoView : NSOpenGLView
{
  int colorBits, depthBits;
  BOOL solidPolys;   // Polygon flag
  GLenum texFormat[1];   // Format of texture (GL_RGB, GL_RGBA)
  NSSize texSize[1];     // Width and height
  char *texBytes[1];     // Texture data
  GLfloat xrot;       // tilt(x) rotation
  GLfloat yrot;       // spin(y) rotation
  GLfloat zrot;       // roll(z) rotation
  GLfloat Tx;
  GLfloat Ty;
  GLfloat Tz;
  GLuint texture[1];     // Storage for one texture
  GLint base;
  
  GLuint theCube;
}

-(id)initWithFrame:(NSRect)frame colorBits:(int)numColorBits depthBits:(int)numDepthBits fullscreen:(BOOL)runFullScreen;
-(void)reshape;
-(void)drawRect:(NSRect)rect;
-(void)dealloc;
-(void)processMotionEvent;
-(void)setXRot:(GLfloat)Xr YRot:(GLfloat)Yr ZRot:(GLfloat)Zr;
-(void)setXt:(GLfloat)Xt Yt:(GLfloat)Yt Zt:(GLfloat)Zt;
-(void)reset;
-(void)Font;

@end
