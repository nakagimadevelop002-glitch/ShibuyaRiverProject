Shader "Neko Legends/Fur/Fur Short Lit"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
        _AmbientColor("Ambient Color", Color) = (0.0, 0.0, 0.0, 1)
        [NoScaleOffset]_BaseMap("Main Texture", 2D) = "white" {}
        _AlphaMask("Alpha Mask", 2D) = "white" {}
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        [NoScaleOffset]_FurMap("Fur Texture", 2D) = "white" {}
        [Normal][NoScaleOffset] _NormalMap("Fur Normal", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0.0, 2.0)) = 1.0
        [IntRange] _ShellAmount("Fur Growth", Range(1, 50)) = 14
        _ShellStep("Fur Spread", Range(0.0, 0.01)) = 0.001
        _FurScale("Fur Scale", Range(0.0, 10.0)) = 1.0
        _AlphaCutout("Fluff Threshold", Range(0.0, 1.0)) = 0.2
        _Occlusion("Occlusion", Range(0.0, 1.0)) = 0.5
        _BaseMove("Wind Direction", Vector) = (0.0, -0.0, 0.0, 3.0)
        _WindFreq("Wind Freq", Vector) = (0.5, 0.7, 0.9, 1.0)
        _WindMove("Wind Move", Vector) = (0.2, 0.3, 0.2, 1.0)

        _RimLightPower("Rim Light Power", Range(1.0, 20.0)) = 6.0
        _RimLightIntensity("Rim Light Intensity", Range(0.0, 1.0)) = 0.5
        _ShadowExtraBias("Shadow Bias", Range(-1.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        
        LOD 100

        ZWrite On
        Cull Back

        // Forward Lit Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            // Adjust multi_compile keywords as needed:
#if (UNITY_VERSION >= 202111)
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
                #pragma multi_compile_fragment _ _LIGHT_LAYERS
#else
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#endif
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
                #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile_fog

                #pragma prefer_hlslcc gles
                #pragma exclude_renderers d3d11_9x
                #pragma target 2.0
                #pragma vertex vert
                #pragma require geometry
                #pragma geometry geom
                #pragma fragment frag
                #include "./Lit.hlsl"
            ENDHLSL
        }

        // Depth Only Pass
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

        // Depth Normals Pass
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
                #pragma exclude_renderers gles gles3 glcore
                #pragma vertex vert
                #pragma require geometry
                #pragma geometry geom
                #pragma fragment frag
                #include "./DepthNormals.hlsl"
            ENDHLSL
        }

        // Shadow Caster Pass
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
    CustomEditorForRenderPipeline "NekoLegends.FurShaderLitInspector" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
}
