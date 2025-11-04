Shader "Neko Legends/Fur/Fur Short UnLit"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        [NoScaleOffset]_AlphaMask("Alpha Mask", 2D) = "white" {}
        [NoScaleOffset]_FurMap("Fur Map", 2D) = "white" {}

        [IntRange] _ShellAmount("Fur Growth", Range(1, 50)) = 16
        _ShellStep("Fur Spread", Range(0.0, 0.01)) = 0.001
        _AlphaCutout("Fluff Threshold", Range(0.0, 1.0)) = 0.2
        _FurScale("Fur Scale", Range(0.0, 10.0)) = 1.0
        _Occlusion("Occlusion", Range(0.0, 1.0)) = 0.5
        _BaseMove("Wind Direction", Vector) = (0.0, -0.0, 0.0, 3.0)
        _WindFreq("Wind Freq", Vector) = (0.5, 0.7, 0.9, 1.0)
        _WindMove("Wind Move", Vector) = (0.2, 0.3, 0.2, 1.0)
        _FaceViewProdThresh("Direction Threshold", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }
        LOD 100

        ZWrite On
        Cull Back

        // Unlit pass using our optimized Unlit.hlsl
        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma multi_compile_fog
                #pragma vertex vert
                #pragma require geometry
                #pragma geometry geom 
                #pragma fragment frag
                #include "./Unlit.hlsl"
            ENDHLSL
        }

        // DepthOnly pass for shadow mapping and depth prepass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma vertex vert
                #pragma require geometry
                #pragma geometry geom 
                #pragma fragment frag
                #include "./Depth.hlsl"
            ENDHLSL
        }

        // ShadowCaster pass for rendering shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma vertex vert
                #pragma require geometry
                #pragma geometry geom 
                #pragma fragment frag
                #include "./Shadow.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "NekoLegends.FurShaderUnLitInspector" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
}
