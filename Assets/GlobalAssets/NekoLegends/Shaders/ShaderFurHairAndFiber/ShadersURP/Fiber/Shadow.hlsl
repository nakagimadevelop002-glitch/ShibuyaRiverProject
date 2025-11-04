#ifndef FUR_GEOMETRY_UNLIT_HLSL
#define FUR_GEOMETRY_UNLIT_HLSL

#include "Packages/com.unity.render-pipelines.universal/Shaders/UnlitInput.hlsl"
#include "./Param.hlsl"
#include "../Common/Common.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
};

struct Varyings
{
    float4 vertex   : SV_POSITION;
    float2 uv       : TEXCOORD0;
    float  fogCoord : TEXCOORD1;
    float  factor   : TEXCOORD2;
};

Attributes vert(Attributes input)
{
    return input;
}

// Helper function to append a vertex to the stream.
void AppendVertex(inout TriangleStream<Varyings> stream, float3 posOS, float3 normalWS, float2 uv, float factor)
{
    Varyings output;
#ifdef SHADOW_CASTER_PASS
    float3 posWS = TransformObjectToWorld(posOS);
    output.vertex = GetShadowPositionHClip(posWS, normalWS);
#else
    output.vertex = TransformObjectToHClip(posOS);
#endif
    output.uv = TRANSFORM_TEX(uv, _BaseMap);
    output.fogCoord = ComputeFogFactor(output.vertex.z);
    output.factor = factor;
    stream.Append(output);
}

[maxvertexcount(53)]
void geom(triangle Attributes input[3], inout TriangleStream<Varyings> stream)
{
    // Precompute starting positions.
    float3 startPos0OS = input[0].positionOS.xyz;
    float3 startPos1OS = input[1].positionOS.xyz;
    float3 startPos2OS = input[2].positionOS.xyz;
    
    // Precompute UVs.
    float2 prevUv0 = input[0].uv;
    float2 prevUv1 = input[1].uv;
    float2 prevUv2 = input[2].uv;
    float2 topUv = (prevUv0 + prevUv1 + prevUv2) / 3.0;
    float2 uvInterp0 = topUv - prevUv0;
    float2 uvInterp1 = topUv - prevUv1;
    float2 uvInterp2 = topUv - prevUv2;
    
    // Compute face geometry.
    float3 prevPos0OS = startPos0OS;
    float3 prevPos1OS = startPos1OS;
    float3 prevPos2OS = startPos2OS;
    float3 line01OS = prevPos1OS - prevPos0OS;
    float3 line02OS = prevPos2OS - prevPos0OS;
    float3 faceNormalOS = SafeNormalize(cross(line01OS, line02OS));
    faceNormalOS += rand3(topUv) * _RandomDirection;
    faceNormalOS = SafeNormalize(faceNormalOS);
    
    // Compute center and "top" positions.
    float3 startCenterPosOS = (prevPos0OS + prevPos1OS + prevPos2OS) / 3.0;
    float3 topPosOS = startCenterPosOS + faceNormalOS * _FurLength;
    
    // Compute moved top position using world space wind effects.
    float3 startCenterPosWS = TransformObjectToWorld(startCenterPosOS);
    float3 faceNormalWS = TransformObjectToWorldNormal(faceNormalOS, true);
    float3 windAngle = _Time.w * _WindFreq.xyz;
    float3 windMoveWS = _WindMove.xyz * sin(windAngle + startCenterPosWS * _WindMove.w);
    float3 baseMoveWS = _BaseMove.xyz;
    float3 movedFaceNormalWS = faceNormalWS + (baseMoveWS + windMoveWS);
    float3 movedFaceNormalOS = TransformWorldToObjectNormal(movedFaceNormalWS, true);
    float3 topMovedPosOS = startCenterPosOS + movedFaceNormalOS * _FurLength;
    
    // Initialize factors and set up delta.
    float prevFactor = 0.0;
    float delta = 1.0 / (float)_FurJoint;
    
    // Iterate over fur joints.
    for (int j = 0; j < _FurJoint; ++j)
    {
        float nextFactor = prevFactor + delta;
        // Compute a factor for the current joint.
        float moveFactor = pow(abs(nextFactor), _BaseMove.w);
        
        // Compute the interpolated top position for this joint.
        float3 lerpTopPosOS = lerp(topPosOS, topMovedPosOS, moveFactor);
        
        // Compute new positions by offsetting the starting positions toward the moved top.
        float3 posInterp0OS = lerpTopPosOS - startPos0OS;
        float3 posInterp1OS = lerpTopPosOS - startPos1OS;
        float3 posInterp2OS = lerpTopPosOS - startPos2OS;
        float3 nextPos0OS = startPos0OS + posInterp0OS * nextFactor;
        float3 nextPos1OS = startPos1OS + posInterp1OS * nextFactor;
        float3 nextPos2OS = startPos2OS + posInterp2OS * nextFactor;
        
        // Interpolate UVs.
        float2 nextUv0 = prevUv0 + uvInterp0 * delta;
        float2 nextUv1 = prevUv1 + uvInterp1 * delta;
        float2 nextUv2 = prevUv2 + uvInterp2 * delta;
        
        // Emit vertices in an order that maintains connectivity.
        AppendVertex(stream, nextPos0OS, faceNormalWS, nextUv0, nextFactor);
        AppendVertex(stream, prevPos0OS, faceNormalWS, prevUv0, prevFactor);
        AppendVertex(stream, nextPos1OS, faceNormalWS, nextUv1, nextFactor);
        AppendVertex(stream, prevPos1OS, faceNormalWS, prevUv1, prevFactor);
        AppendVertex(stream, nextPos2OS, faceNormalWS, nextUv2, nextFactor);
        AppendVertex(stream, prevPos2OS, faceNormalWS, prevUv2, prevFactor);
        AppendVertex(stream, nextPos0OS, faceNormalWS, nextUv0, nextFactor);
        AppendVertex(stream, prevPos0OS, faceNormalWS, prevUv0, prevFactor);
        
        // Prepare for the next iteration.
        prevFactor = nextFactor;
        prevPos0OS = nextPos0OS;
        prevPos1OS = nextPos1OS;
        prevPos2OS = nextPos2OS;
        prevUv0 = nextUv0;
        prevUv1 = nextUv1;
        prevUv2 = nextUv2;
        
        stream.RestartStrip();
    }
    
    // Emit the final strip that reaches the top.
    AppendVertex(stream, prevPos0OS, faceNormalWS, prevUv0, prevFactor);
    AppendVertex(stream, prevPos1OS, faceNormalWS, prevUv1, prevFactor);
    AppendVertex(stream, topMovedPosOS, faceNormalWS, topUv, 1.0);
    AppendVertex(stream, prevPos2OS, faceNormalWS, prevUv2, prevFactor);
    AppendVertex(stream, prevPos0OS, faceNormalWS, prevUv0, prevFactor);
    stream.RestartStrip();
}

void frag(
    Varyings input, 
    out float4 outColor : SV_Target, 
    out float outDepth : SV_Depth)
{
    // In this unlit pass, simply output depth.
    outColor = outDepth = input.vertex.z / input.vertex.w;
}

#endif
