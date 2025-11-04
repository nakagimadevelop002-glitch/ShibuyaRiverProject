Shader "Neko Legends/Fur/Fiber UnLit"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
        _BaseMap("Base Map", 2D) = "white" {}

        _FurLength("Fiber Length", Range(0.0, 2.0)) = 0.3
        [IntRange] _FurJoint("Fiber Joint", Range(0, 6)) = 3
        _Occlusion("Occlusion", Range(0.0, 1.0)) = 0.3
        _RandomDirection("Random Direction", Range(0.0, 1.0)) = 0.3

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
        Cull Back

        //---------------------------------------------------------------------
        // Unlit Pass: Uses tessellation, geometry, and fragment stages with our
        // optimized unlit HLSL includes.
        //---------------------------------------------------------------------
        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
                // Exclude certain renderers (adjust as needed)
                #pragma exclude_renderers gles gles3 glcore
                #pragma multi_compile_fog

                // Vertex, Tessellation, and Geometry stages
                #pragma vertex vert
                #pragma require tessellation tessHW
                #pragma hull hull
                #pragma domain domain
                #pragma require geometry
                #pragma geometry geom 
                #pragma fragment frag

                // Include the optimized unlit code and tessellation helper files.
                #include "./Unlit.hlsl"
                #include "./UnlitTessellation.hlsl"
            ENDHLSL
        }

        //---------------------------------------------------------------------
        // DepthOnly Pass: For shadow maps and depth prepass.
        //---------------------------------------------------------------------
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

        //---------------------------------------------------------------------
        // ShadowCaster Pass: For generating shadow maps.
        //---------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
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
    CustomEditorForRenderPipeline "NekoLegends.FurShaderFiberUnLitInspector" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
}
