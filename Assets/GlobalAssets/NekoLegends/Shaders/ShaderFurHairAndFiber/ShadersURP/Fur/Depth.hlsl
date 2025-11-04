#ifndef FUR_SHELL_DEPTH_HLSL
#define FUR_SHELL_DEPTH_HLSL

#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
#include "./Param.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float  fogCoord : TEXCOORD1;
    float  layer : TEXCOORD2;
};

Attributes vert(Attributes input)
{
    return input;
}

void AppendShellVertex(inout TriangleStream<Varyings> stream, Attributes input, int index)
{
    Varyings output = (Varyings)0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    float moveFactor = pow(abs((float)index / _ShellAmount), _BaseMove.w);
    float3 posOS = input.positionOS.xyz;
    float3 windAngle = _Time.w * _WindFreq.xyz;
    float3 windMove = moveFactor * _WindMove.xyz * sin(windAngle + posOS * _WindMove.w);
    float3 move = moveFactor * _BaseMove.xyz;

    float3 shellDir = normalize(normalInput.normalWS + move + windMove);
    float3 posWS = vertexInput.positionWS + shellDir * (_ShellStep * index);
    float4 posCS = TransformWorldToHClip(posWS);
    
    output.vertex = posCS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.fogCoord = ComputeFogFactor(posCS.z);
    output.layer = (float)index / _ShellAmount;

    stream.Append(output);
}

[maxvertexcount(128)]
void geom(triangle Attributes input[3], inout TriangleStream<Varyings> stream)
{
    // Precompute per-vertex data for the triangle
    VertexPositionInputs vertexInputs[3];
    VertexNormalInputs normalInputs[3];
    float2 uvCoords[3];
    float3 posOSArray[3];

    [unroll]
    for (int j = 0; j < 3; j++)
    {
        // Compute once per vertex (instead of per shell)
        vertexInputs[j] = GetVertexPositionInputs(input[j].positionOS.xyz);
        normalInputs[j] = GetVertexNormalInputs(input[j].normalOS, input[j].tangentOS);
        uvCoords[j] = TRANSFORM_TEX(input[j].uv, _BaseMap);
        posOSArray[j] = input[j].positionOS.xyz;
    }
    
    // Compute the constant wind angle once (same for all shells and vertices)
    float3 windAngle = _Time.w * _WindFreq.xyz;
    
    // Use an integer loop counter
    for (int i = 0; i < _ShellAmount; i++)
    {
        // Precompute the move factor for this shell level
        float moveFactor = pow(abs((float)i / (float)_ShellAmount), _BaseMove.w);

        [unroll]
        for (int j = 0; j < 3; j++)
        {
            Varyings output = (Varyings)0;
            
            // Calculate per-vertex offset with the precomputed data
            float3 windMove = moveFactor * _WindMove.xyz * sin(windAngle + posOSArray[j] * _WindMove.w);
            float3 move = moveFactor * _BaseMove.xyz;
            float3 shellDir = normalize(normalInputs[j].normalWS + move + windMove);
            float3 posWS = vertexInputs[j].positionWS + shellDir * (_ShellStep * i);
            float4 posCS = TransformWorldToHClip(posWS);
            
            output.vertex = posCS;
            output.uv = uvCoords[j];
            output.fogCoord = ComputeFogFactor(posCS.z);
            output.layer = (float)i / (float)_ShellAmount;
            
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
    float4 furColor = SAMPLE_TEXTURE2D(_FurMap, sampler_FurMap, input.uv / _BaseMap_ST.xy * _FurScale);
    float alpha = furColor.r * (1.0 - input.layer);
    if (input.layer > 0.0 && alpha < _AlphaCutout) discard;

    outColor = outDepth = input.vertex.z / input.vertex.w;
}

#endif