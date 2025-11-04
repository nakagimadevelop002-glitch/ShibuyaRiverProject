
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoLegends
{
    public class FurShaderLitInspector : ShaderGUIBase
    {


        protected bool showFurProperties, showWindProperties, showLightingProperties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            ShowLogo();
            ShowMainSection(materialEditor, properties);
            ShowFurProperties(materialEditor, properties);
            ShowWindProperties(materialEditor, properties);
            ShowLightingProperties(materialEditor, properties);

        }

        protected void ShowMainSection(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMap = FindProperty("_BaseMap", properties);
            MaterialProperty _BaseColor = FindProperty("_BaseColor", properties);
            MaterialProperty _AmbientColor = FindProperty("_AmbientColor", properties);
            MaterialProperty _AlphaMask = FindProperty("_AlphaMask", properties);
            
            MaterialProperty _Metallic = FindProperty("_Metallic", properties);
            MaterialProperty _Smoothness = FindProperty("_Smoothness", properties);
            MaterialProperty _Occlusion = FindProperty("_Occlusion", properties);

            materialEditor.ShaderProperty(_BaseMap, _BaseMap.displayName);
            materialEditor.ShaderProperty(_BaseColor, _BaseColor.displayName);
            materialEditor.ShaderProperty(_AmbientColor, _AmbientColor.displayName);
            materialEditor.ShaderProperty(_AlphaMask, _AlphaMask.displayName);
            materialEditor.ShaderProperty(_Metallic, _Metallic.displayName);
            materialEditor.ShaderProperty(_Smoothness, _Smoothness.displayName);
            materialEditor.ShaderProperty(_Occlusion, _Occlusion.displayName);

        }
        protected void ShowFurProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {

            MaterialProperty _FurMap = FindProperty("_FurMap", properties);
            MaterialProperty _NormalMap = FindProperty("_NormalMap", properties);
            MaterialProperty _NormalScale = FindProperty("_NormalScale", properties);
            MaterialProperty _ShellAmount = FindProperty("_ShellAmount", properties);
            MaterialProperty _ShellStep = FindProperty("_ShellStep", properties);
            MaterialProperty _AlphaCutout = FindProperty("_AlphaCutout", properties);

            showFurProperties = EditorPrefs.GetBool("FurHairAndFiberInspector_ShowFurProperties", false);
            showFurProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFurProperties, "Fur");
            if (showFurProperties)
            {

                materialEditor.ShaderProperty(_FurMap, _FurMap.displayName);
                materialEditor.ShaderProperty(_NormalMap, _NormalMap.displayName);
                materialEditor.ShaderProperty(_NormalScale, _NormalScale.displayName);
                materialEditor.ShaderProperty(_ShellAmount, _ShellAmount.displayName);
                materialEditor.ShaderProperty(_ShellStep, _ShellStep.displayName);
                materialEditor.ShaderProperty(_AlphaCutout, _AlphaCutout.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurHairAndFiberInspector_ShowFurProperties", showFurProperties);
        }


        protected void ShowWindProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMove = FindProperty("_BaseMove", properties);
            MaterialProperty _WindFreq = FindProperty("_WindFreq", properties);
            MaterialProperty _WindMove = FindProperty("_WindMove", properties);

            showWindProperties = EditorPrefs.GetBool("FurHairAndFiberInspector_showWindProperties", false);
            showWindProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showWindProperties, "Wind");
            if (showWindProperties)
            {
                materialEditor.ShaderProperty(_BaseMove, _BaseMove.displayName);
                materialEditor.ShaderProperty(_WindFreq, _WindFreq.displayName);
                materialEditor.ShaderProperty(_WindMove, _WindMove.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurHairAndFiberInspector_showWindProperties", showWindProperties);
        }


        protected void ShowLightingProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _RimLightPower = FindProperty("_RimLightPower", properties);
            MaterialProperty _RimLightIntensity = FindProperty("_RimLightIntensity", properties);
            MaterialProperty _ShadowExtraBias = FindProperty("_ShadowExtraBias", properties);

            showLightingProperties = EditorPrefs.GetBool("FurHairAndFiberInspector_showLightProperties", false);
            showLightingProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showLightingProperties, "Lighting");
            if (showLightingProperties)
            {
                materialEditor.ShaderProperty(_RimLightPower, _RimLightPower.displayName);
                materialEditor.ShaderProperty(_RimLightIntensity, _RimLightIntensity.displayName);
                materialEditor.ShaderProperty(_ShadowExtraBias, _ShadowExtraBias.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurHairAndFiberInspector_showLightProperties", showLightingProperties);
        }


    }

}

#endif
