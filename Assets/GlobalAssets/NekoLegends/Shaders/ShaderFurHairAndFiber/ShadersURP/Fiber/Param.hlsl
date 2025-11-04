#ifndef FUR_GEOMETRY_PARAM_HLSL
#define FUR_GEOMETRY_PARAM_HLSL

float _AlphaCutout;
float4 _AmbientColor;
float _FurLength;
int _FurJoint;
float _Occlusion;
float _RandomDirection;
float _NormalFactor;

float4 _BaseMove;
float4 _WindFreq;
float4 _WindMove;

float _TessMinDist;
float _TessMaxDist;
float _TessFactor;

float _MoveScale;
float _Spring;
float _Damper;
float _Gravity;

// Alpha mask texture and sampler
TEXTURE2D(_AlphaMask);
SAMPLER(sampler_AlphaMask);

TEXTURE2D(_FurMap);
SAMPLER(sampler_FurMap);
float4 _FurMap_ST;
#endif