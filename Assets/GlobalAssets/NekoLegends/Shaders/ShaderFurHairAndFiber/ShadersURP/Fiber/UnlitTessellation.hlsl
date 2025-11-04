#ifndef FUR_FIN_UNLIT_TESSELLATION_HLSL
#define FUR_FIN_UNLIT_TESSELLATION_HLSL

#include "./Param.hlsl"

// Structure to hold tessellation factors and extra data for domain interpolation.
struct HsConstantOutput
{
    float fTessFactor[3]    : SV_TessFactor;
    float fInsideTessFactor : SV_InsideTessFactor;
    float3 f3B210 : POS3;
    float3 f3B120 : POS4;
    float3 f3B021 : POS5;
    float3 f3B012 : POS6;
    float3 f3B102 : POS7;
    float3 f3B201 : POS8;
    float3 f3B111 : CENTER;
    float3 f3N110 : NORMAL3;
    float3 f3N011 : NORMAL4;
    float3 f3N101 : NORMAL5;
};

//
// Hull Shader Stage
//
[domain("tri")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[patchconstantfunc("hullConst")]
[outputcontrolpoints(3)]
Attributes hull(InputPatch<Attributes, 3> input, uint id : SV_OutputControlPointID)
{
    return input[id];
}

HsConstantOutput hullConst(InputPatch<Attributes, 3> i)
{
    HsConstantOutput o = (HsConstantOutput)0;

    // Compute a tessellation factor based on the camera distance.
    float3 camPosMV = float3(UNITY_MATRIX_MV[0][3], UNITY_MATRIX_MV[1][3], UNITY_MATRIX_MV[2][3]);
    float distance = length(camPosMV);
    float factor = (_TessMaxDist - _TessMinDist) / max(distance - _TessMinDist, 0.01);
    factor = min(factor, 1.0) * _TessFactor;

    o.fTessFactor[0] = o.fTessFactor[1] = o.fTessFactor[2] = factor;
    o.fInsideTessFactor = factor;

    // Cache the positions and normals from the input patch.
    float3 p0 = i[0].positionOS.xyz;
    float3 p1 = i[1].positionOS.xyz;
    float3 p2 = i[2].positionOS.xyz;

    float3 n0 = i[0].normalOS;
    float3 n1 = i[1].normalOS;
    float3 n2 = i[2].normalOS;
        
    // Compute intermediate control points along the edges.
    o.f3B210 = ((2.0 * p0) + p1 - (dot(p1 - p0, n0) * n0)) / 3.0;
    o.f3B120 = ((2.0 * p1) + p0 - (dot(p0 - p1, n1) * n1)) / 3.0;
    o.f3B021 = ((2.0 * p1) + p2 - (dot(p2 - p1, n1) * n1)) / 3.0;
    o.f3B012 = ((2.0 * p2) + p1 - (dot(p1 - p2, n2) * n2)) / 3.0;
    o.f3B102 = ((2.0 * p2) + p0 - (dot(p0 - p2, n2) * n2)) / 3.0;
    o.f3B201 = ((2.0 * p0) + p2 - (dot(p2 - p0, n0) * n0)) / 3.0;

    // Compute the average of the control points and the vertex positions.
    float3 baryAvg = (o.f3B210 + o.f3B120 + o.f3B021 + o.f3B012 + o.f3B102 + o.f3B201) / 6.0;
    float3 vertexAvg = (p0 + p1 + p2) / 3.0;
    o.f3B111 = baryAvg + ((baryAvg - vertexAvg) / 2.0);
    
    // Compute edge vectors and their squared lengths.
    float3 edge0 = p1 - p0;
    float3 edge1 = p2 - p1;
    float3 edge2 = p0 - p2;
    
    float denom0 = dot(edge0, edge0);
    float denom1 = dot(edge1, edge1);
    float denom2 = dot(edge2, edge2);
    
    // Compute correction factors for normal interpolation.
    float fV12 = 2.0 * dot(edge0, n0 + n1) / denom0;
    float fV23 = 2.0 * dot(edge1, n1 + n2) / denom1;
    float fV31 = 2.0 * dot(edge2, n2 + n0) / denom2;
    
    o.f3N110 = normalize(n0 + n1 - fV12 * edge0);
    o.f3N011 = normalize(n1 + n2 - fV23 * edge1);
    o.f3N101 = normalize(n2 + n0 - fV31 * edge2);
           
    return o;
}

//
// Domain Shader Stage
//
[domain("tri")]
Attributes domain(
    HsConstantOutput hsConst, 
    const OutputPatch<Attributes, 3> i,
    float3 bary : SV_DomainLocation)
{
    Attributes o = (Attributes)0;

    // Decompose the barycentrics.
    float fU = bary.x;
    float fV = bary.y;
    float fW = bary.z;
    
    // Precompute squared terms.
    float fUU = fU * fU;
    float fVV = fV * fV;
    float fWW = fW * fW;
    // Precompute triple factors for use in blending the control points.
    float fUU3 = fUU * 3.0;
    float fVV3 = fVV * 3.0;
    float fWW3 = fWW * 3.0;
    
    // Compute the interpolated position.
    o.positionOS = float4(
        i[0].positionOS.xyz * fWW * fW +
        i[1].positionOS.xyz * fUU * fU +
        i[2].positionOS.xyz * fVV * fV +
        hsConst.f3B210 * fWW3 * fU +
        hsConst.f3B120 * fW * fUU3 +
        hsConst.f3B201 * fWW3 * fV +
        hsConst.f3B021 * fUU3 * fV +
        hsConst.f3B102 * fW * fVV3 +
        hsConst.f3B012 * fU * fVV3 +
        hsConst.f3B111 * 6.0 * fW * fU * fV, 
        1.0);
        
    // Compute the interpolated normal.
    o.normalOS = normalize(
        i[0].normalOS * fWW +
        i[1].normalOS * fUU +
        i[2].normalOS * fVV +
        hsConst.f3N110 * fW * fU +
        hsConst.f3N011 * fU * fV +
        hsConst.f3N101 * fW * fV);
    
    // Interpolate texture coordinates.
    o.uv = i[0].uv * fW + i[1].uv * fU + i[2].uv * fV;
    
    return o;
}

#endif
