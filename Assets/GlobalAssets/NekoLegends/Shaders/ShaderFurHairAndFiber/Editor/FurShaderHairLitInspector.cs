
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoLegends
{
    public class FurShaderHairLitInspector : ShaderGUIBase
    {


        protected bool showFurProperties, showWindProperties, showLightingProperties, showPerformanceProperties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            ShowLogo();
            ShowMainSection(materialEditor, properties);
            ShowFurProperties(materialEditor, properties);
            ShowWindProperties(materialEditor, properties);
            ShowPerformanceProperties(materialEditor, properties);
            ShowLightingProperties(materialEditor, properties);

        }

        protected void ShowMainSection(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMap = FindProperty("_BaseMap", properties);
            MaterialProperty _BaseColor = FindProperty("_BaseColor", properties);
            MaterialProperty _AmbientColor = FindProperty("_AmbientColor", properties);
            
            MaterialProperty _Metallic = FindProperty("_Metallic", properties);
            MaterialProperty _Smoothness = FindProperty("_Smoothness", properties);
            MaterialProperty _Occlusion = FindProperty("_Occlusion", properties);
            MaterialProperty _DrawOrigPolygon = FindProperty("_DrawOrigPolygon", properties);

            materialEditor.ShaderProperty(_BaseMap, _BaseMap.displayName);
            materialEditor.ShaderProperty(_BaseColor, _BaseColor.displayName);
            materialEditor.ShaderProperty(_AmbientColor, _AmbientColor.displayName);
            materialEditor.ShaderProperty(_Metallic, _Metallic.displayName);
            materialEditor.ShaderProperty(_Smoothness, _Smoothness.displayName);
            materialEditor.ShaderProperty(_Occlusion, _Occlusion.displayName);
            materialEditor.ShaderProperty(_DrawOrigPolygon, _DrawOrigPolygon.displayName);

        }
        protected void ShowFurProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {

            MaterialProperty _FurMap = FindProperty("_FurMap", properties);
            MaterialProperty _NormalMap = FindProperty("_NormalMap", properties);
            MaterialProperty _NormalScale = FindProperty("_NormalScale", properties);
            MaterialProperty _FaceNormalFactor = FindProperty("_FaceNormalFactor", properties);
            MaterialProperty _FinJointNum = FindProperty("_FinJointNum", properties);
            MaterialProperty _FinLength = FindProperty("_FinLength", properties);
            MaterialProperty _Density = FindProperty("_Density", properties);
            MaterialProperty _AlphaCutout = FindProperty("_AlphaCutout", properties);
            
            showFurProperties = EditorPrefs.GetBool("FurShaderHairLitInspector_ShowFurProperties", false);
            showFurProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFurProperties, "Hair");
            if (showFurProperties)
            {

                materialEditor.ShaderProperty(_FurMap, _FurMap.displayName);
                materialEditor.ShaderProperty(_NormalMap, _NormalMap.displayName);
                materialEditor.ShaderProperty(_NormalScale, _NormalScale.displayName);
                materialEditor.ShaderProperty(_FaceNormalFactor, _FaceNormalFactor.displayName);
                materialEditor.ShaderProperty(_FinJointNum, _FinJointNum.displayName);
                materialEditor.ShaderProperty(_FinLength, _FinLength.displayName);
                materialEditor.ShaderProperty(_Density, _Density.displayName);
                materialEditor.ShaderProperty(_AlphaCutout, _AlphaCutout.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderHairLitInspector_ShowFurProperties", showFurProperties);
        }


        protected void ShowWindProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMove = FindProperty("_BaseMove", properties);
            MaterialProperty _WindFreq = FindProperty("_WindFreq", properties);
            MaterialProperty _WindMove = FindProperty("_WindMove", properties);

            showWindProperties = EditorPrefs.GetBool("FurShaderHairLitInspector_showWindProperties", false);
            showWindProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showWindProperties, "Wind");
            if (showWindProperties)
            {
                materialEditor.ShaderProperty(_BaseMove, _BaseMove.displayName);
                materialEditor.ShaderProperty(_WindFreq, _WindFreq.displayName);
                materialEditor.ShaderProperty(_WindMove, _WindMove.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderHairLitInspector_showWindProperties", showWindProperties);
        }

        protected void ShowPerformanceProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _TessMinDist = FindProperty("_TessMinDist", properties);
            MaterialProperty _TessMaxDist = FindProperty("_TessMaxDist", properties);
            MaterialProperty _TessFactor = FindProperty("_TessFactor", properties);

            showPerformanceProperties = EditorPrefs.GetBool("FurShaderHairLitInspector_ShowPerformanceProperties", false);
            showPerformanceProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showPerformanceProperties, "Performance");
            if (showPerformanceProperties)
            {
                materialEditor.ShaderProperty(_TessMinDist, _TessMinDist.displayName);
                materialEditor.ShaderProperty(_TessMaxDist, _TessMaxDist.displayName);
                materialEditor.ShaderProperty(_TessFactor, _TessFactor.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderHairLitInspector_ShowPerformanceProperties", showPerformanceProperties);
        }


        protected void ShowLightingProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _RimLightPower = FindProperty("_RimLightPower", properties);
            MaterialProperty _RimLightIntensity = FindProperty("_RimLightIntensity", properties);
            MaterialProperty _ShadowExtraBias = FindProperty("_ShadowExtraBias", properties);

            showLightingProperties = EditorPrefs.GetBool("FurShaderHairLitInspector_showLightingProperties", false);
            showLightingProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showLightingProperties, "Lighting");
            if (showLightingProperties)
            {
                materialEditor.ShaderProperty(_RimLightPower, _RimLightPower.displayName);
                materialEditor.ShaderProperty(_RimLightIntensity, _RimLightIntensity.displayName);
                materialEditor.ShaderProperty(_ShadowExtraBias, _ShadowExtraBias.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderHairLitInspector_showLightingProperties", showLightingProperties);
        }


    }

}

#endif
