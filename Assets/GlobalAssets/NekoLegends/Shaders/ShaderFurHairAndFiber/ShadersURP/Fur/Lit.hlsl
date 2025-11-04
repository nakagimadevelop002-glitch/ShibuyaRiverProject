#ifndef FUR_SHELL_LIT_HLSL
#define FUR_SHELL_LIT_HLSL

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "./Param.hlsl"
#include "../Common/Common.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 texcoord   : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS              : SV_POSITION;
    float3 positionWS              : TEXCOORD0;
    float3 normalWS                : TEXCOORD1;
    float3 tangentWS               : TEXCOORD2;
    float2 uv                      : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
    float4 fogFactorAndVertexLight : TEXCOORD6; // x: fogFactor, yzw: vertex light
    float  layer                  : TEXCOORD7;
};

Attributes vert(Attributes input)
{
    return input;
}

[maxvertexcount(42)]
void geom(triangle Attributes input[3], inout TriangleStream<Varyings> stream)
{
    // Precompute per-vertex data for the entire triangle.
    VertexPositionInputs vertexInputs[3];
    VertexNormalInputs normalInputs[3];
    float2 uvArray[3];
    float3 posOSArray[3];
    float2 lightmapUVArray[3];

    [unroll]
    for (int j = 0; j < 3; j++)
    {
        vertexInputs[j]    = GetVertexPositionInputs(input[j].positionOS.xyz);
        normalInputs[j]    = GetVertexNormalInputs(input[j].normalOS, input[j].tangentOS);
        uvArray[j]         = TRANSFORM_TEX(input[j].texcoord, _BaseMap);
        posOSArray[j]      = input[j].positionOS.xyz;
        lightmapUVArray[j] = input[j].lightmapUV;
    }

    // Precompute constant values
    float3 windAngle = _Time.w * _WindFreq.xyz;
    float3 camPos    = GetCameraPositionWS();

    // Loop over shells using integer indices.
    for (int i = 0; i < _ShellAmount; i++)
    {
        float moveFactor = pow(abs((float)i / (float)_ShellAmount), _BaseMove.w);

        [unroll]
        for (int j = 0; j < 3; j++)
        {
            Varyings output = (Varyings)0;

            // Calculate per-vertex shell offset.
            float3 windMove = moveFactor * _WindMove.xyz * sin(windAngle + posOSArray[j] * _WindMove.w);
            float3 move     = moveFactor * _BaseMove.xyz;
            float3 shellDir = SafeNormalize(normalInputs[j].normalWS + move + windMove);
            // Use the precomputed camera position.
            float3 viewDirWS = camPos - vertexInputs[j].positionWS;
            
            output.positionWS = vertexInputs[j].positionWS + shellDir * (_ShellStep * i);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.uv         = uvArray[j];
            output.normalWS   = normalInputs[j].normalWS;
            output.tangentWS  = normalInputs[j].tangentWS;
            output.layer      = (float)i / (float)_ShellAmount;

            // Compute vertex lighting and fog based on the new position.
            float fogFactor    = ComputeFogFactor(output.positionCS.z);
            float3 vertexLight = VertexLighting(vertexInputs[j].positionWS, normalInputs[j].normalWS);
            output.fogFactorAndVertexLight = float4(fogFactor, vertexLight);

            OUTPUT_LIGHTMAP_UV(lightmapUVArray[j], unity_LightmapST, output.lightmapUV);
            OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

            stream.Append(output);
        }
        stream.RestartStrip();
    }
}

inline float3 TransformHClipToWorld(float4 positionCS)
{
    return mul(UNITY_MATRIX_I_VP, positionCS).xyz;
}

float4 frag(Varyings input) : SV_Target
{
    // Compute fur UV coordinates.
    float2 furUv = input.uv / _BaseMap_ST.xy * _FurScale;
    float4 furColor = SAMPLE_TEXTURE2D(_FurMap, sampler_FurMap, furUv);
    float alpha = furColor.r * (1.0 - input.layer);
    if (input.layer > 0.0 && alpha < _AlphaCutout) discard;

    float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
    float3 normalTS = UnpackNormalScale(
        SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, furUv), 
        _NormalScale);
    float3 bitangent = SafeNormalize(viewDirWS.y * cross(input.normalWS, input.tangentWS));
    float3 normalWS = SafeNormalize(TransformTangentToWorld(
        normalTS, 
        float3x3(input.tangentWS, bitangent, input.normalWS)));

    // Sample the alpha mask and base textures.
    float alphaMaskValue = SAMPLE_TEXTURE2D(_AlphaMask, sampler_AlphaMask, input.uv).a;
    float4 baseColor     = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

    // Calculate fur alpha and blend the textures.
    float furAlpha = furColor.a * alphaMaskValue;
    if (input.layer > 0.0 && furAlpha < _AlphaCutout) discard;
    float4 finalColor = lerp(baseColor, furColor, alphaMaskValue);
    finalColor.a = furAlpha;

    SurfaceData surfaceData = (SurfaceData)0;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    surfaceData.occlusion = lerp(1.0 - _Occlusion, 1.0, input.layer);
    surfaceData.albedo   *= surfaceData.occlusion;
   
    InputData inputData = (InputData)0;
    inputData.positionWS            = input.positionWS;
    inputData.normalWS              = normalWS;
    inputData.viewDirectionWS       = viewDirWS;
#if (defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)) && !defined(_RECEIVE_SHADOWS_OFF)
    inputData.shadowCoord           = TransformWorldToShadowCoord(input.positionWS);
#else
    inputData.shadowCoord           = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord              = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting        = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI               = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    float4 color = UniversalFragmentPBR(inputData, surfaceData);

    ApplyRimLight(color.rgb, input.positionWS, viewDirWS, normalWS);
    color.rgb += _AmbientColor;
    color.rgb  = MixFog(color.rgb, inputData.fogCoord);
    
    return float4(color.rgb, finalColor.a);
}

#endif
