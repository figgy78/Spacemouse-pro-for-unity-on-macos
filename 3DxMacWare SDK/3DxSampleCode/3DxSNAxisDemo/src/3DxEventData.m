/*
 * 3DxEventData.m
 *
 * Container for 3D Mouse event data
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


@implementation TDxEventData

@synthesize tx;
@synthesize ty;
@synthesize tz;
@synthesize rx;
@synthesize ry;
@synthesize rz;
@synthesize button;
@synthesize timeStamp;

-(id)init
{
  self = [super init];
  if (self)
  {
    tx = 1;
    ty = 2;
    tz = 3;
    rx = 4;
    ry = 5;
    rz = 6;
    button = 0;

    //NSDate *aDate = [NSDate date];
    timeStamp  = 1.0f;//[aDate timeIntervalSince1970];
    //NSLog(@"init... %f", timeStamp);
    //[aDate release];
  }
  else
  {
    self = nil;
  }    
  return self;
}

- (id)initWithTDxEventData:(TDxEventData *)inData
{
  self = [super init];
  if (self)
  {
    tx = ty = tz = 0;
    rx = ry = rz = 0;
    button = 0;
    if (inData)
    {
      tx = [inData tx];
      ty = [inData ty];
      tz = [inData tz];
      rx = [inData rx];
      ry = [inData ry];
      rz = [inData rz];
      button     = [inData button];
      timeStamp  = [inData timeStamp];
    }
  }
  else
  {
    self = nil;
  }    
  return self;  
}

-(void)setValuesFromEventData:(TDxEventData *)inData
{
  //tx = ty = tz = 9;
  //NSLog(@"TDxEventData -setValuesFromEventData ... %@ - %@", self, inData);
  if (inData)
  {
    tx = [inData tx];
    ty = [inData ty];
    tz = [inData tz];
    rx = [inData rx];
    ry = [inData ry];
    rz = [inData rz];
    button     = [inData button];
    timeStamp  = [inData timeStamp];//[NSDate timeIntervalSince1970];
  }
}

-(void)setButton:(int)inButton
{
  tx=ty=tz=rx=ry=rz=0;
  button = inButton;
}


-(void)dealloc
{
  [super dealloc];
}

- (NSString *)description
{
  return [NSString stringWithFormat:@"[(%e) %i %i %i / %i %i %i]", timeStamp, tx, ty, tz, rx, ry, rz];
}
@end
