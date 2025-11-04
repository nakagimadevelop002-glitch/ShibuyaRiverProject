#ifndef FUR_SHELL_UNLIT_HLSL
#define FUR_SHELL_UNLIT_HLSL

#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
#include "./Param.hlsl"
#include "../Common/Common.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 uv         : TEXCOORD0;
};

struct Varyings
{
    float4 vertex   : SV_POSITION;
    float2 uv       : TEXCOORD0;
    float  fogCoord : TEXCOORD1;
    float  layer    : TEXCOORD2;
};

Attributes vert(Attributes input)
{
    return input;
}

[maxvertexcount(128)]
void geom(triangle Attributes input[3], inout TriangleStream<Varyings> stream)
{
    // Precompute per-triangle data
    VertexPositionInputs vertexInputs[3];
    VertexNormalInputs normalInputs[3];
    float2 uvArray[3];
    float3 posOSArray[3];
    float3 normalOSArray[3];

    [unroll]
    for (int j = 0; j < 3; j++)
    {
         vertexInputs[j]   = GetVertexPositionInputs(input[j].positionOS.xyz);
         normalInputs[j]   = GetVertexNormalInputs(input[j].normalOS, input[j].tangentOS);
         uvArray[j]        = TRANSFORM_TEX(input[j].uv, _BaseMap);
         posOSArray[j]     = input[j].positionOS.xyz;
         normalOSArray[j]  = input[j].normalOS;
    }
    
    // Compute constant wind angle once
    float3 windAngle = _Time.w * _WindFreq.xyz;
    
    // Loop over shell layers using integer indices
    for (int i = 0; i < _ShellAmount; i++)
    {
         float moveFactor = pow(abs((float)i / (float)_ShellAmount), _BaseMove.w);

         [unroll]
         for (int j = 0; j < 3; j++)
         {
              // Compute per-vertex offsets
              float3 windMove = moveFactor * _WindMove.xyz * sin(windAngle + posOSArray[j] * _WindMove.w);
              float3 move     = moveFactor * _BaseMove.xyz;
              float3 shellDir = normalize(normalInputs[j].normalWS + move + windMove);
              float3 posWS    = vertexInputs[j].positionWS + shellDir * (_ShellStep * i);
              float4 posCS    = TransformWorldToHClip(posWS);

              // For shells above the base layer, perform a face-orientation check
              if (i > 0)
              {
                  float3 viewDirOS = GetViewDirectionOS(posOSArray[j]);
                  float eyeDotN = dot(viewDirOS, normalOSArray[j]);
                  if (abs(eyeDotN) < _FaceViewProdThresh)
                      continue; // Skip this vertex if not visible enough
              }

              Varyings output = (Varyings)0;
              output.vertex   = posCS;
              output.uv       = uvArray[j];
              output.fogCoord = ComputeFogFactor(posCS.z);
              output.layer    = (float)i / (float)_ShellAmount;

              stream.Append(output);
         }
         stream.RestartStrip();
    }
}

float4 frag(Varyings input) : SV_Target
{
    // Sample fur texture
    float4 furColor = SAMPLE_TEXTURE2D(_FurMap, sampler_FurMap, input.uv * _FurScale);
    float alpha = furColor.r * (1.0 - input.layer);
    if (input.layer > 0.0 && alpha < _AlphaCutout)
         discard;

    // Sample base texture (e.g., skin)
    float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float occlusion = lerp(1.0 - _Occlusion, 1.0, input.layer);
    float3 color = baseColor.xyz * occlusion;
    color = MixFog(color, input.fogCoord);

    // Sample the alpha mask texture
    float alphaMaskValue = SAMPLE_TEXTURE2D(_AlphaMask, sampler_AlphaMask, input.uv).a;
  
    // Calculate fur alpha and blend textures
    float furAlpha = furColor.a * alphaMaskValue;
    if (input.layer > 0.0 && furAlpha < _AlphaCutout)
         discard;

    float4 finalColor = lerp(baseColor, furColor, alphaMaskValue);
    finalColor.a = furAlpha;

    return float4(color, alpha);
}

#endif
