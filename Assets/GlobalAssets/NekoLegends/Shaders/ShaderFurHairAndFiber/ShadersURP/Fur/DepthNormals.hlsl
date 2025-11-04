#ifndef FUR_SHELL_DEPTH_NORMALS_HLSL
#define FUR_SHELL_DEPTH_NORMALS_HLSL

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "./Param.hlsl"

struct Attributes
{
    half4 positionOS : POSITION;
    half3 normalOS   : NORMAL;
    half4 tangentOS  : TANGENT;
    float2 uv        : TEXCOORD0;
};

struct Varyings
{
    float4 vertex    : SV_POSITION;
    float2 uv        : TEXCOORD0;
    float  layer     : TEXCOORD1;
    float3 normalWS  : TEXCOORD2;
    float3 tangentWS : TEXCOORD3;
    float3 posWS     : TEXCOORD4;
};

Attributes vert(Attributes input)
{
    return input;
}

[maxvertexcount(63)]
void geom(triangle Attributes input[3], inout TriangleStream<Varyings> stream)
{
    // Precompute per-vertex data for the triangle.
    VertexPositionInputs vertexInputs[3];
    VertexNormalInputs normalInputs[3];
    float2 uvArray[3];
    float3 posOSArray[3];

    [unroll]
    for (int j = 0; j < 3; j++)
    {
        vertexInputs[j] = GetVertexPositionInputs(input[j].positionOS.xyz);
        normalInputs[j] = GetVertexNormalInputs(input[j].normalOS, input[j].tangentOS);
        uvArray[j]      = TRANSFORM_TEX(input[j].uv, _BaseMap);
        posOSArray[j]   = input[j].positionOS.xyz;
    }

    // Precompute the constant wind angle (same for all vertices and shells)
    float3 windAngle = _Time.w * _WindFreq.xyz;

    // Loop over shells using integer indices.
    for (int i = 0; i < _ShellAmount; i++)
    {
        float moveFactor = pow(abs((float)i / (float)_ShellAmount), _BaseMove.w);

        [unroll]
        for (int j = 0; j < 3; j++)
        {
            Varyings output = (Varyings)0;

            // Calculate wind movement and base move
            float3 windMove = moveFactor * _WindMove.xyz * sin(windAngle + posOSArray[j] * _WindMove.w);
            float3 move     = moveFactor * _BaseMove.xyz;
            // Combine the normal, base move, and wind move
            float3 shellDir = normalize(normalInputs[j].normalWS + move + windMove);
            // Calculate new world space position offset by the shell step
            float3 posWS    = vertexInputs[j].positionWS + shellDir * (_ShellStep * i);
            float4 posCS    = TransformWorldToHClip(posWS);

            output.vertex    = posCS;
            output.posWS     = posWS;
            output.uv        = uvArray[j];
            output.layer     = (float)i / (float)_ShellAmount;
            output.normalWS  = normalInputs[j].normalWS;
            output.tangentWS = normalInputs[j].tangentWS;

            stream.Append(output);
        }
        stream.RestartStrip();
    }
}

float4 frag(Varyings input) : SV_Target
{
    // Compute the fur UVs
    float2 furUV = input.uv / _BaseMap_ST.xy * _FurScale;

    float4 furColor = SAMPLE_TEXTURE2D(_FurMap, sampler_FurMap, furUV);
    float alpha = furColor.r * (1.0 - input.layer);
    if (input.layer > 0.0 && alpha < _AlphaCutout) 
        discard;

    // Compute view direction in world space
    float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.posWS);
    // Sample and unpack the normal from the normal map
    half3 normalTS = UnpackNormalScale(
        SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, furUV),
        _NormalScale);
    // Calculate the bitangent
    float3 bitangent = SafeNormalize(viewDirWS.y * cross(input.normalWS, input.tangentWS));
    // Transform the tangent space normal to world space
    float3 normalWS = SafeNormalize(TransformTangentToWorld(
        normalTS,
        float3x3(input.tangentWS, bitangent, input.normalWS)));

    // Output the normalized world-space normal (with zero alpha for depth normals pass)
    return float4(NormalizeNormalPerPixel(normalWS), 0.0);
}

#endif
