
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoLegends
{
    public class FurShaderHairUnLitInspector : ShaderGUIBase
    {


        protected bool showFurProperties, showWindProperties, showLightingProperties, showPerformanceProperties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            ShowLogo();
            ShowMainSection(materialEditor, properties);
            ShowFurProperties(materialEditor, properties);
            ShowWindProperties(materialEditor, properties);
            ShowPerformanceProperties(materialEditor, properties);

        }

        protected void ShowMainSection(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMap = FindProperty("_BaseMap", properties);
            MaterialProperty _BaseColor = FindProperty("_BaseColor", properties);
            MaterialProperty _Occlusion = FindProperty("_Occlusion", properties);
            MaterialProperty _DrawOrigPolygon = FindProperty("_DrawOrigPolygon", properties);

            materialEditor.ShaderProperty(_BaseMap, _BaseMap.displayName);
            materialEditor.ShaderProperty(_BaseColor, _BaseColor.displayName);
            materialEditor.ShaderProperty(_Occlusion, _Occlusion.displayName);
            materialEditor.ShaderProperty(_DrawOrigPolygon, _DrawOrigPolygon.displayName);

        }
        protected void ShowFurProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {

            MaterialProperty _FurMap = FindProperty("_FurMap", properties);
            MaterialProperty _FinJointNum = FindProperty("_FinJointNum", properties);
            MaterialProperty _FinLength = FindProperty("_FinLength", properties);
            MaterialProperty _Density = FindProperty("_Density", properties);
            MaterialProperty _AlphaCutout = FindProperty("_AlphaCutout", properties);
            MaterialProperty _AppendMoreFins = FindProperty("_AppendMoreFins", properties);

            showFurProperties = EditorPrefs.GetBool("FurShaderHairUnLitInspector_ShowFurProperties", false);
            showFurProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFurProperties, "Hair");
            if (showFurProperties)
            {

                materialEditor.ShaderProperty(_FurMap, _FurMap.displayName);
                materialEditor.ShaderProperty(_FinJointNum, _FinJointNum.displayName);
                materialEditor.ShaderProperty(_FinLength, _FinLength.displayName);
                materialEditor.ShaderProperty(_Density, _Density.displayName);
                materialEditor.ShaderProperty(_AlphaCutout, _AlphaCutout.displayName);
                materialEditor.ShaderProperty(_AppendMoreFins, _AppendMoreFins.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderHairUnLitInspector_ShowFurProperties", showFurProperties);
        }


        protected void ShowWindProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMove = FindProperty("_BaseMove", properties);
            MaterialProperty _WindFreq = FindProperty("_WindFreq", properties);
            MaterialProperty _WindMove = FindProperty("_WindMove", properties);

            showWindProperties = EditorPrefs.GetBool("FurShaderHairUnLitInspector_showWindProperties", false);
            showWindProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showWindProperties, "Wind");
            if (showWindProperties)
            {
                materialEditor.ShaderProperty(_BaseMove, _BaseMove.displayName);
                materialEditor.ShaderProperty(_WindFreq, _WindFreq.displayName);
                materialEditor.ShaderProperty(_WindMove, _WindMove.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderHairUnLitInspector_showWindProperties", showWindProperties);
        }

        protected void ShowPerformanceProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _TessMinDist = FindProperty("_TessMinDist", properties);
            MaterialProperty _TessMaxDist = FindProperty("_TessMaxDist", properties);
            MaterialProperty _TessFactor = FindProperty("_TessFactor", properties);

            showPerformanceProperties = EditorPrefs.GetBool("FurShaderHairUnLitInspector_ShowPerformanceProperties", false);
            showPerformanceProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showPerformanceProperties, "Performance");
            if (showPerformanceProperties)
            {
                materialEditor.ShaderProperty(_TessMinDist, _TessMinDist.displayName);
                materialEditor.ShaderProperty(_TessMaxDist, _TessMaxDist.displayName);
                materialEditor.ShaderProperty(_TessFactor, _TessFactor.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderHairUnLitInspector_ShowPerformanceProperties", showPerformanceProperties);
        }




    }

}

#endif
