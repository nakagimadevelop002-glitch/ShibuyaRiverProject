
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoLegends
{
    public class FurShaderFiberLitInspector : ShaderGUIBase
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

            materialEditor.ShaderProperty(_BaseMap, _BaseMap.displayName);
            materialEditor.ShaderProperty(_BaseColor, _BaseColor.displayName);
            materialEditor.ShaderProperty(_AmbientColor, _AmbientColor.displayName);
            materialEditor.ShaderProperty(_Metallic, _Metallic.displayName);
            materialEditor.ShaderProperty(_Smoothness, _Smoothness.displayName);
            materialEditor.ShaderProperty(_Occlusion, _Occlusion.displayName);

        }

        protected void ShowFurProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _FurJoint = FindProperty("_FurJoint", properties);
            MaterialProperty _FurLength = FindProperty("_FurLength", properties);
            MaterialProperty _NormalFactor = FindProperty("_NormalFactor", properties);
            
            showFurProperties = EditorPrefs.GetBool("FurShaderFiberLitInspector_ShowFurProperties", false);
            showFurProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFurProperties, "Fiber");
            if (showFurProperties)
            {
                materialEditor.ShaderProperty(_FurJoint, _FurJoint.displayName);
                materialEditor.ShaderProperty(_FurLength, _FurLength.displayName);
                materialEditor.ShaderProperty(_NormalFactor, _NormalFactor.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderFiberLitInspector_ShowFurProperties", showFurProperties);
        }


        protected void ShowWindProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMove = FindProperty("_BaseMove", properties);
            MaterialProperty _WindFreq = FindProperty("_WindFreq", properties);
            MaterialProperty _WindMove = FindProperty("_WindMove", properties);

            showWindProperties = EditorPrefs.GetBool("FurShaderFiberLitInspector_showWindProperties", false);
            showWindProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showWindProperties, "Wind");
            if (showWindProperties)
            {
                materialEditor.ShaderProperty(_BaseMove, _BaseMove.displayName);
                materialEditor.ShaderProperty(_WindFreq, _WindFreq.displayName);
                materialEditor.ShaderProperty(_WindMove, _WindMove.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderFiberLitInspector_showWindProperties", showWindProperties);
        }

        protected void ShowPerformanceProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _TessMinDist = FindProperty("_TessMinDist", properties);
            MaterialProperty _TessMaxDist = FindProperty("_TessMaxDist", properties);
            MaterialProperty _TessFactor = FindProperty("_TessFactor", properties);

            showPerformanceProperties = EditorPrefs.GetBool("FurShaderFiberLitInspector_ShowPerformanceProperties", false);
            showPerformanceProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showPerformanceProperties, "Performance");
            if (showPerformanceProperties)
            {
                materialEditor.ShaderProperty(_TessMinDist, _TessMinDist.displayName);
                materialEditor.ShaderProperty(_TessMaxDist, _TessMaxDist.displayName);
                materialEditor.ShaderProperty(_TessFactor, _TessFactor.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderFiberLitInspector_ShowPerformanceProperties", showPerformanceProperties);
        }


        protected void ShowLightingProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _RimLightPower = FindProperty("_RimLightPower", properties);
            MaterialProperty _RimLightIntensity = FindProperty("_RimLightIntensity", properties);
            MaterialProperty _ShadowExtraBias = FindProperty("_ShadowExtraBias", properties);

            showLightingProperties = EditorPrefs.GetBool("FurShaderFiberLitInspector_showLightingProperties", false);
            showLightingProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showLightingProperties, "Lighting");
            if (showLightingProperties)
            {
                materialEditor.ShaderProperty(_RimLightPower, _RimLightPower.displayName);
                materialEditor.ShaderProperty(_RimLightIntensity, _RimLightIntensity.displayName);
                materialEditor.ShaderProperty(_ShadowExtraBias, _ShadowExtraBias.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderFiberLitInspector_showLightingProperties", showLightingProperties);
        }


    }

}

#endif
