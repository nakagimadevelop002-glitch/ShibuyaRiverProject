Shader "Neko Legends/Fur/Hair UnLit"
{

Properties
{
    [MainColor] _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
    _BaseMap("Main Texture", 2D) = "white" {}

    [Toggle(DRAW_ORIG_POLYGON)]_DrawOrigPolygon("Show Mesh", Float) = 1
    [Toggle(APPEND_MORE_FINS)]_AppendMoreFins("Extra Hair", Float) = 1
    [NoScaleOffset] _FurMap("Hair Map", 2D) = "white" {}
    [IntRange] _FinJointNum("Joints", Range(1, 10)) = 5
    _AlphaCutout("Fluff Threshold", Range(0.0, 1.0)) = 0.2
    _FinLength("Length", Range(0.0, 1.0)) = 0.1
    _Density("Density", Range(0.1, 10.0)) = 1.0
    _FaceViewProdThresh("Direction Threshold", Range(0.0, 1.0)) = 0.1
    _Occlusion("Occlusion", Range(0.0, 1.0)) = 0.3
    _RandomDirection("Randomize Direction", Range(0.0, 1.0)) = 0.3

    _BaseMove("Wind Direction", Vector) = (0.0, -0.0, 0.0, 3.0)
    _WindFreq("Wind Freq", Vector) = (0.5, 0.7, 0.9, 1.0)
    _WindMove("Wind Move", Vector) = (0.2, 0.3, 0.2, 1.0)

    _TessMinDist("Detail Min Distance", Range(0.1, 50)) = 1.0
    _TessMaxDist("Detail Max Distance", Range(0.1, 50)) = 10.0
    _TessFactor("Detail Factor", Range(1, 50)) = 10
}

SubShader
{
    Tags 
    { 
        "RenderType" = "Opaque" 
        "RenderPipeline" = "UniversalPipeline" 
        "IgnoreProjector" = "True"
    }

    ZWrite On
    Cull Off

    Pass
    {
        Name "Unlit"

        HLSLPROGRAM
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_fog
        #pragma multi_compile _ DRAW_ORIG_POLYGON
        #pragma multi_compile _ APPEND_MORE_FINS
        #pragma vertex vert
        #pragma require tessellation tessHW
        #pragma hull hull
        #pragma domain domain
        #pragma require geometry
        #pragma geometry geom 
        #pragma fragment frag
        #include "./Unlit.hlsl"
        #include "./UnlitTessellation.hlsl"
        ENDHLSL
    }

    Pass
    {
        Name "DepthOnly"
        Tags { "LightMode" = "DepthOnly" }

        ZWrite On
        ColorMask 0

        HLSLPROGRAM
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_fog
        #pragma multi_compile _ DRAW_ORIG_POLYGON
        #pragma multi_compile _ APPEND_MORE_FINS
        #pragma vertex vert
        #pragma require tessellation tessHW
        #pragma hull hull
        #pragma domain domain
        #pragma require geometry
        #pragma geometry geom 
        #pragma fragment frag
        #include "./Shadow.hlsl"
        #include "./UnlitTessellation.hlsl"
        ENDHLSL
    }

    Pass
    {
        Name "ShadowCaster"
        Tags {"LightMode" = "ShadowCaster" }

        ZWrite On
        ZTest LEqual
        ColorMask 0

        HLSLPROGRAM
        #pragma exclude_renderers gles gles3 glcore
        #pragma multi_compile_fog
        #pragma multi_compile _ DRAW_ORIG_POLYGON
        #pragma multi_compile _ APPEND_MORE_FINS
        #pragma vertex vert
        #pragma require tessellation tessHW
        #pragma hull hull
        #pragma domain domain
        #pragma require geometry
        #pragma geometry geom 
        #pragma fragment frag
        #define SHADOW_CASTER_PASS
        #include "./Shadow.hlsl"
        #include "./UnlitTessellation.hlsl"
        ENDHLSL
    }
}
CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
CustomEditorForRenderPipeline "NekoLegends.FurShaderHairUnLitInspector" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"


}
