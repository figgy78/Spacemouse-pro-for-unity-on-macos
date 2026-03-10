/*
 * DemoView.m
 *
 * Implementation of subclassed NSOpenGLView
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
#import <OpenGL/OpenGL.h>
#import <OpenGL/glu.h>
#import <math.h>

#import "NSFont_OpenGL.h"
#import "DemoView.h"
				 
GLfloat worldtransform[16];

/*
 * Category to get the NSBitmapImageRepresentation from the NSImage
 * for texture loading
 */
@implementation NSImage (custom)
- (NSBitmapImageRep *)bitmapImageRepresentation
{
  int width = [self size].width;
  int height = [self size].height;
  
  if (width < 1 || height < 1)
    return nil;
  
  NSBitmapImageRep *rep = [[NSBitmapImageRep alloc]
                           initWithBitmapDataPlanes: NULL
                           pixelsWide: width
                           pixelsHigh: height
                           bitsPerSample: 8
                           samplesPerPixel: 4
                           hasAlpha: YES
                           isPlanar: NO
                           colorSpaceName: NSDeviceRGBColorSpace
                           bytesPerRow: width * 4
                           bitsPerPixel: 32];
  
  NSGraphicsContext *ctx = [NSGraphicsContext graphicsContextWithBitmapImageRep: rep];
  [NSGraphicsContext saveGraphicsState];
  [NSGraphicsContext setCurrentContext: ctx];
  [self drawAtPoint: NSZeroPoint fromRect: NSZeroRect operation: NSCompositeCopy fraction: 1.0];
  [ctx flushGraphics];
  [NSGraphicsContext restoreGraphicsState];
  
  return [rep autorelease];
}
@end

@interface DemoView (InternalMethods)
-(NSOpenGLPixelFormat *)createPixelFormat:(NSRect)frame;
-(void)switchToOriginalDisplayMode;
-(BOOL)initGL;
-(BOOL)loadGLTextures;
-(BOOL)loadBitmap:(NSString *)filename intoIndex:(int)texIndex;
-(void)buildFont;
-(void)glPrint:(NSString *)fmt, ...;
-(void)setMax;
-(void)DrawCube;
-(void)WriteText;
@end

@implementation DemoView

- (BOOL)acceptsFirstResponder
{
  return YES;
}

- (BOOL)becomeFirstResponder
{
  return  YES;
}

- (BOOL)resignFirstResponder
{
  return YES;
}

-(id)initWithFrame:(NSRect)frame colorBits:(int)numColorBits
       depthBits:(int)numDepthBits fullscreen:(BOOL)runFullScreen
{
   NSOpenGLPixelFormat *pixelFormat;

   colorBits = numColorBits;
   depthBits = numDepthBits;

   xrot = yrot = zrot = 0;
   
   // initialize transformation matrix
   memset(worldtransform, 0, sizeof(worldtransform));
   worldtransform[0] = worldtransform[5] = worldtransform[10] = worldtransform[15] = 1.0;
   
   pixelFormat = [self createPixelFormat:frame];
   if (pixelFormat != nil)
   {
      self = [super initWithFrame:frame pixelFormat:pixelFormat];
      [pixelFormat release];
      if (self)
      {
         [[self openGLContext] makeCurrentContext];

        [self reshape];
         if (![self initGL])
         {
            [self clearGLContext];
            self = nil;
         }
      }
   }
   else
      self = nil;

   return self;
}

- (void)keyDown:(NSEvent *)theEvent
{
//  NSLog(@"-keyDown, received the (key)event: %@", theEvent);
}

/*
 * Create a pixel format and possible switch to full screen mode
 */
-(NSOpenGLPixelFormat *)createPixelFormat:(NSRect)frame
{
   NSOpenGLPixelFormatAttribute pixelAttribs[16];
   int pixNum = 0;

    NSOpenGLPixelFormat *pixelFormat;

   pixelAttribs[pixNum++] = NSOpenGLPFADoubleBuffer;
   pixelAttribs[pixNum++] = NSOpenGLPFAAccelerated;
   pixelAttribs[pixNum++] = NSOpenGLPFAColorSize;
   pixelAttribs[pixNum++] = colorBits;
   pixelAttribs[pixNum++] = NSOpenGLPFADepthSize;
   pixelAttribs[pixNum++] = depthBits;

    pixelAttribs[pixNum] = 0;
   pixelFormat = [[NSOpenGLPixelFormat alloc]
                   initWithAttributes:pixelAttribs];

   return pixelFormat;
}


/*
 * Enable/disable full screen mode
 */
-(BOOL)setFullScreen:(BOOL)enableFS inFrame:(NSRect)frame
{
   BOOL success = FALSE;
   NSOpenGLPixelFormat *pixelFormat;
   NSOpenGLContext *newContext;

   [[self openGLContext] clearDrawable];
  
   pixelFormat = [self createPixelFormat:frame];
   if (pixelFormat != nil)
   {
      newContext = [[NSOpenGLContext alloc] initWithFormat:pixelFormat
                     shareContext:nil];
      if (newContext != nil)
      {
         [super setFrame:frame];
         [super setOpenGLContext:newContext];
         [newContext makeCurrentContext];

         [self reshape];
         if ([self initGL])
            success = TRUE;
      }
      [pixelFormat release];
   }

   return success;
}


/*
 * Initial OpenGL setup
 */
-(BOOL)initGL
{
  if (![self loadGLTextures])
    return FALSE;

  glShadeModel(GL_SMOOTH);               // Enable smooth shading
  glClearColor(0.0f, 0.0f, 0.0f, 0.5f);   // Black background
  glClearDepth(1.0f);                    // Depth buffer setup
  // glEnable(GL_DEPTH_TEST);            // Enable depth testing
  glDepthFunc(GL_LEQUAL);               // Type of depth test to do
  // Really nice perspective calculations
  glHint(GL_PERSPECTIVE_CORRECTION_HINT, GL_NICEST);
  xrot = 0.0f;
  yrot = 0.0f;
  zrot = 0.0f;

  [self Font];

  theCube = glGenLists (1);
  glNewList(theCube, GL_COMPILE);
  [self DrawCube];
  glEndList();

  return TRUE;
}


/*
 * Setup a texture from our model
 */
-(BOOL)loadGLTextures
{
  BOOL status = FALSE;

  if ([self loadBitmap:[NSString stringWithFormat:@"%@/%s",[[NSBundle mainBundle] resourcePath], "Crate.bmp"] intoIndex:0])
  {
    status = TRUE;

    glGenTextures(1, &texture[0]);   // Create the texture

    // Typical texture generation using data from the bitmap
    glBindTexture(GL_TEXTURE_2D, texture[0]);

    glTexImage2D(GL_TEXTURE_2D, 0, 3, texSize[0].width,
                 texSize[0].height, 0, texFormat[0],
                 GL_UNSIGNED_BYTE, texBytes[0]);
    // Linear filtering
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    free(texBytes[0]);
  }

  return status;
}


/*
 * The NSBitmapImageRep is going to load the bitmap, but it will be
 * setup for the opposite coordinate system than what OpenGL uses, so
 * we copy things around.
 */
-(BOOL)loadBitmap:(NSString *)filename intoIndex:(int)texIndex
{
  BOOL success = FALSE;
  NSBitmapImageRep *theImage;
  NSInteger bitsPPixel, bytesPRow;
  unsigned char *theImageData;
  int rowNum, destRowNum;
  theImage = [[[NSImage alloc] initWithContentsOfFile:filename] bitmapImageRepresentation];
  if (theImage != nil)
  {
      bitsPPixel = [theImage bitsPerPixel];
      bytesPRow = [theImage bytesPerRow];
      if (bitsPPixel == 24)        // No alpha channel
         texFormat[texIndex] = GL_RGB;
      else if (bitsPPixel == 32)   // There is an alpha channel
         texFormat[texIndex] = GL_RGBA;
      texSize[texIndex].width = [theImage pixelsWide];
      texSize[texIndex].height = [theImage pixelsHigh];
      texBytes[texIndex] = calloc(bytesPRow * texSize[texIndex].height,
                                     1);
      if (texBytes[texIndex] != NULL)
      {
         success = TRUE;
         theImageData = [theImage bitmapData];
         destRowNum = 0;
         for(rowNum = texSize[texIndex].height - 1; rowNum >= 0;
              rowNum--, destRowNum++)
         {
            // Copy the entire row in one shot
            memcpy(texBytes[texIndex] + (destRowNum * bytesPRow),
                    theImageData + (rowNum * bytesPRow),
                    bytesPRow);
         }
      }
   }
   return success;
}


/*
 * Resize ourself
 */
-(void)reshape
{ 
  NSRect sceneBounds;

  [[self openGLContext] update];
  sceneBounds = [self bounds];

  // Reset current viewport
  glViewport(0, 0, sceneBounds.size.width, sceneBounds.size.height);
  glMatrixMode(GL_PROJECTION);   // Select the projection matrix
  glLoadIdentity();                // and reset it

  // Calculate the aspect ratio of the view
  float zNear = 0.1f;
  float zFar  = 100.0f;
  float aspRatio = sceneBounds.size.width / sceneBounds.size.height;
  float fov   = 45.0f;
  float cotan = 1.0f / tanf(fov / 2.0f);
  float m[16] = // 4x4 as replacement for deprecated gluPerspective()
  {
    cotan / aspRatio, 0.0f, 0.0f, 0.0f,
    0.0f, cotan, 0.0f, 0.0f,
    0.0f, 0.0f, (zFar + zNear) / (zNear - zFar), -1.0f,
    0.0f, 0.0f, (2.0f * zFar * zNear) / (zNear - zFar), 0.0f
  };
  glMultMatrixf(m);

  glMatrixMode(GL_MODELVIEW);    // Select the modelview matrix
  glLoadIdentity();                // and reset it
}


-(void)WriteText
{
  glColor4f(1.0f, 1.0f, 1.0f, 0.8f);
  glTranslatef(-2.0f, 1.3f, -10.0f);      // Move into screen 10 units

  glRasterPos2f(-4.95f, 3.5f);
  [self glPrint:@"3Dconnexion 3D Demo"];
  glRasterPos2f(-4.95f, 2.9f);
  [self glPrint:@"Default: Button 1 / Menu opens 3Dconnexion.prefPane"];
  glRasterPos2f(-4.95f, 2.3f);
  [self glPrint:@"(see \"3DxDemo\" config. in the prefs for current mapping)"];

  glMatrixMode(GL_MODELVIEW);
  glLoadIdentity();
}


-(void)DrawCube
{
 glEnable(GL_DEPTH_TEST);
 glEnable(GL_TEXTURE_2D);                // Enable texture mapping
 glBindTexture(GL_TEXTURE_2D, texture[0]);   // Select our texture
 glBegin(GL_QUADS);
    // Front face
    glTexCoord2f(0.0f, 0.0f);
    glVertex3f(-1.0f, -1.0f,  1.0f);   // Bottom left
    glTexCoord2f(1.0f, 0.0f);
    glVertex3f(1.0f, -1.0f,  1.0f);   // Bottom right
    glTexCoord2f(1.0f, 1.0f);
    glVertex3f(1.0f,  1.0f,  1.0f);   // Top right
    glTexCoord2f(0.0f, 1.0f);
    glVertex3f(-1.0f,  1.0f,  1.0f);   // Top left
    
    // Back face
    glTexCoord2f(1.0f, 0.0f);
    glVertex3f(-1.0f, -1.0f, -1.0f);   // Bottom right
    glTexCoord2f(1.0f, 1.0f);
    glVertex3f(-1.0f,  1.0f, -1.0f);   // Top right
    glTexCoord2f(0.0f, 1.0f);
    glVertex3f(1.0f,  1.0f, -1.0f);   // Top left
    glTexCoord2f(0.0f, 0.0f);
    glVertex3f(1.0f, -1.0f, -1.0f);   // Bottom left
    
    // Top face
    glTexCoord2f(0.0f, 1.0f);
    glVertex3f(-1.0f,  1.0f, -1.0f);   // Top left
    glTexCoord2f(0.0f, 0.0f);
    glVertex3f(-1.0f,  1.0f,  1.0f);   // Bottom left
    glTexCoord2f(1.0f, 0.0f);
    glVertex3f(1.0f,  1.0f,  1.0f);   // Bottom right
    glTexCoord2f(1.0f, 1.0f);
    glVertex3f(1.0f,  1.0f, -1.0f);   // Top right
    
    // Bottom face
    glTexCoord2f(1.0f, 1.0f);
    glVertex3f(-1.0f, -1.0f, -1.0f);   // Top right
    glTexCoord2f(0.0f, 1.0f);
    glVertex3f(1.0f, -1.0f, -1.0f);   // Top left
    glTexCoord2f(0.0f, 0.0f);
    glVertex3f(1.0f, -1.0f,  1.0f);   // Bottom left
    glTexCoord2f(1.0f, 0.0f);
    glVertex3f(-1.0f, -1.0f,  1.0f);   // Bottom right
    
    // Right face
    glTexCoord2f(1.0f, 0.0f);
    glVertex3f(1.0f, -1.0f, -1.0f);   // Bottom right
    glTexCoord2f(1.0f, 1.0f);
    glVertex3f(1.0f,  1.0f, -1.0f);   // Top right
    glTexCoord2f(0.0f, 1.0f);
    glVertex3f(1.0f,  1.0f,  1.0f);   // Top left
    glTexCoord2f(0.0f, 0.0f);
    glVertex3f(1.0f, -1.0f,  1.0f);   // Bottom left
    
    // Left face
    glTexCoord2f(0.0f, 0.0f);
    glVertex3f(-1.0f, -1.0f, -1.0f);   // Bottom left
    glTexCoord2f(1.0f, 0.0f);
    glVertex3f(-1.0f, -1.0f,  1.0f);   // Bottom right
    glTexCoord2f(1.0f, 1.0f);
    glVertex3f(-1.0f,  1.0f,  1.0f);   // Top right
    glTexCoord2f(0.0f, 1.0f);
    glVertex3f(-1.0f,  1.0f, -1.0f);   // Top left
  glEnd();
	glDisable(GL_TEXTURE_2D);                // Enable texture mapping
    
  glBegin(GL_LINES);
    glColor3f(1.0f,0.0f,0.0f);
    glVertex3f(0.0f,0.0f,0.0f);
    glVertex3f(1000.0f,0.0f,0.0f);
    
    glColor3f(0.0f,1.0f,0.0f);
    glVertex3f(0.0f,0.0f,0.0f);
    glVertex3f(0.0f,1000.0f,0.0f);
    
    glColor3f(0.0f,0.0f,1.0f);
    glVertex3f(0.0f,0.0f,0.0f);
    glVertex3f(0.0f,0.0f,1000.0f);
 glEnd();
 glDisable(GL_DEPTH_TEST);

}

/*
 * Called when the system thinks we need to draw.
 */
- (void) drawRect:(NSRect)rect
{
  // Clear the screen and depth buffer
  glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
  glLoadIdentity();   // Reset the current modelview matrix
  [self WriteText];

  glTranslatef(0.0f, 0.0, -8.0f); // Move into scene 8 units
  glTranslatef(Tx,Ty,Tz);
  [self processMotionEvent];
  glMultMatrixf(worldtransform);
  glCallList(theCube);

  [[self openGLContext] flushBuffer];
}

/*
 * Cleanup
 */
-(void)dealloc
{
  [super dealloc];
}


-(void)processMotionEvent
{
  float   motion[6];
  GLfloat rotate[16];

  motion[0] = (GLfloat)Tx;   // X = left - right movement
  motion[1] = (GLfloat)Ty;   // Y = up - down movement
  motion[2] = (GLfloat)Tz;   // Z = forward - backward movement

  motion[3] = (GLfloat)xrot; // A = rotation around X
  motion[4] = (GLfloat)yrot; // B = rotation around Y
  motion[5] = (GLfloat)zrot; // C = rotation around Z

  motion[0] += worldtransform[12];
  motion[1] += worldtransform[13];
  motion[2] += worldtransform[14];
  worldtransform[12] = 0.0;
  worldtransform[13] = 0.0;
  worldtransform[14] = 0.0;

  //  d3glComputeRotationMatrix(rotate, &motion[3]);
  double ca,cb,cc,sa,sb,sc;
    
  ca = cos(motion[5]); sa = sin(motion[5]);
  cb = cos(motion[4]); sb = sin(motion[4]);
  cc = cos(motion[3]); sc = sin(motion[3]);

  rotate[0] =  ca*cb;
  rotate[1] =  sa*cb;
  rotate[2] = -sb;

  rotate[4] = -sa*cc+ca*sc*sb;
  rotate[5] =  ca*cc+sa*sc*sb;
  rotate[6] =  cb*sc;

  rotate[8] =  sa*sc+ca*cc*sb;
  rotate[9] = -ca*sc+sa*cc*sb;
  rotate[10]= cb*cc;

  rotate[3] = rotate[7] = rotate[11] = rotate[12] = rotate[13] = rotate[14] = 0.0;

  rotate[15] = 1.0;
    
  //  d3glMultiplyRotationMatrix(rotate, context->worldtransform);
  float mr[16];

  mr[0] = rotate[0]*worldtransform[0] + rotate[4]*worldtransform[1] + rotate[8]*worldtransform[2];
  mr[4] = rotate[0]*worldtransform[4] + rotate[4]*worldtransform[5] + rotate[8]*worldtransform[6];
  mr[8] = rotate[0]*worldtransform[8] + rotate[4]*worldtransform[9] + rotate[8]*worldtransform[10];
  mr[1] = rotate[1]*worldtransform[0] + rotate[5]*worldtransform[1] + rotate[9]*worldtransform[2];
  mr[5] = rotate[1]*worldtransform[4] + rotate[5]*worldtransform[5] + rotate[9]*worldtransform[6];
  mr[9] = rotate[1]*worldtransform[8] + rotate[5]*worldtransform[9] + rotate[9]*worldtransform[10];
  mr[2] = rotate[2]*worldtransform[0] + rotate[6]*worldtransform[1] + rotate[10]*worldtransform[2];
  mr[6] = rotate[2]*worldtransform[4] + rotate[6]*worldtransform[5] + rotate[10]*worldtransform[6];
  mr[10]= rotate[2]*worldtransform[8] + rotate[6]*worldtransform[9] + rotate[10]*worldtransform[10];

  rotate[0] = mr[0];
  rotate[1] = mr[1];
  rotate[2] = mr[2];
  rotate[4] = mr[4];
  rotate[5] = mr[5];
  rotate[6] = mr[6];
  rotate[8] = mr[8];
  rotate[9] = mr[9];
  rotate[10]= mr[10];    

  memcpy(worldtransform, rotate, sizeof(worldtransform));

  worldtransform[12] = motion[0];
  worldtransform[13] = motion[1];
  worldtransform[14] = motion[2];
}


-(void)setXRot:(GLfloat)Xr YRot:(GLfloat)Yr ZRot:(GLfloat)Zr
{
  xrot = Xr;
  yrot = Yr;
  zrot = Zr;
}


-(void) setMax
{
}


-(void)setXt:(GLfloat)Xt Yt:(GLfloat)Yt Zt:(GLfloat)Zt
{
  Tx = Xt;
  Ty = Yt;
  Tz = Zt;
}


-(void)reset
{
  float ident[16] = { 1.0f,0.0f,0.0f,0.0f,
                      0.0f,1.0f,0.0f,0.0f,
                      0.0f,0.0f,1.0f,0.0f,
                      0.0f,0.0f,0.0f,1.0f};

  memcpy(worldtransform, ident, sizeof(worldtransform));
  Tx = Ty = Tz =0;
}


-(void)Font
{
  NSFont *font = [NSFont fontWithName:@"Tahoma" size:15];

  // 95 since if we do 96, we get the delete character...
  base = glGenLists(95);   // Storage for 95 textures (one per character)
  if (font == nil)
    NSLog(@"font is nil\n");
  
  if (![font makeGLDisplayListFirst:(unichar)' ' count:95 base:base])
    NSLog(@"Didn't make display list\n");
}


-(void)glPrint:(NSString *)fmt, ...
{
   NSString *text;
   va_list ap;                           // Pointer To List Of Arguments
   unichar *uniBuffer;

   if (fmt == nil || [fmt length] == 0)  // If There's No Text
      return;                            // Do Nothing

   va_start(ap, fmt);                    // Parses The String For Variables
   text = [[[NSString alloc] initWithFormat:fmt arguments:ap] autorelease];
   va_end(ap);                           // Results Are Stored In Text

   glPushAttrib(GL_LIST_BIT);            // Pushes The Display List Bits
   glListBase(base - 32);                // Sets The Base Character to 32
   uniBuffer = calloc([text length], sizeof(unichar));
   [text getCharacters:uniBuffer];
   // Draws The Display List Text
   glCallLists((GLsizei)[text length], GL_UNSIGNED_SHORT, uniBuffer);
   free(uniBuffer);
   glPopAttrib();                        // Pops The Display List Bits
}
@end
