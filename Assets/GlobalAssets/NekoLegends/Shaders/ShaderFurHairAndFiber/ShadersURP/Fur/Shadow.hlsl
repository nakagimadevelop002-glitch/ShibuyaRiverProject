#ifndef FUR_SHELL_SHADOW_HLSL
#define FUR_SHELL_SHADOW_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
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
    // Precompute per-vertex data for the triangle
    VertexPositionInputs vertexInputs[3];
    VertexNormalInputs normalInputs[3];
    float2 uvArray[3];
    float3 posOSArray[3];

    [unroll]
    for (int j = 0; j < 3; j++)
    {
        vertexInputs[j] = GetVertexPositionInputs(input[j].positionOS.xyz);
        normalInputs[j] = GetVertexNormalInputs(input[j].normalOS, input[j].tangentOS);
        uvArray[j]      = TRANSFORM_TEX(input[j].uv, _FurMap);
        posOSArray[j]   = input[j].positionOS.xyz;
    }
    
    // Compute the constant wind angle once
    float3 windAngle = _Time.w * _WindFreq.xyz;

    // Loop over shells using integer loop counters
    for (int i = 0; i < _ShellAmount; i++)
    {
        float moveFactor = pow(abs((float)i / (float)_ShellAmount), _BaseMove.w);

        [unroll]
        for (int j = 0; j < 3; j++)
        {
            Varyings output = (Varyings)0;
            
            // Calculate wind movement and base movement offsets
            float3 windMove = moveFactor * _WindMove.xyz * sin(windAngle + posOSArray[j] * _WindMove.w);
            float3 move     = moveFactor * _BaseMove.xyz;
            float3 shellDir = normalize(normalInputs[j].normalWS + move + windMove);
            
            // Compute the world-space position offset by the shell step
            float3 posWS = vertexInputs[j].positionWS + shellDir * (_ShellStep * i);
            // Use the shadow-specific function to compute the clip space position for shadows
            float4 posCS = GetShadowPositionHClip(posWS, normalInputs[j].normalWS);
            
            output.vertex   = posCS;
            output.uv       = uvArray[j];
            output.fogCoord = ComputeFogFactor(posCS.z);
            output.layer    = (float)i / (float)_ShellAmount;
            
            stream.Append(output);
        }
        stream.RestartStrip();
    }
}

void frag(
    Varyings input, 
    out float4 outColor : SV_Target, 
    out float outDepth : SV_Depth)
{
    float4 furColor = SAMPLE_TEXTURE2D(_FurMap, sampler_FurMap, input.uv * _FurScale);
    float alpha = furColor.r * (1.0 - input.layer);
    if (input.layer > 0.0 && alpha < _AlphaCutout)
        discard;

    outColor = outDepth = input.vertex.z / input.vertex.w;
}

#endif
