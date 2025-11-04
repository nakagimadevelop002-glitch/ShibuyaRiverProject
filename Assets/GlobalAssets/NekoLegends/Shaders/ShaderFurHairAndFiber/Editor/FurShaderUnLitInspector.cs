
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace NekoLegends
{
    public class FurShaderUnLitInspector : ShaderGUIBase
    {


        protected bool showFurProperties, showWindProperties, showLightingProperties;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            ShowLogo();
            ShowMainSection(materialEditor, properties);
            ShowFurProperties(materialEditor, properties);
            ShowWindProperties(materialEditor, properties);

        }

        protected void ShowMainSection(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMap = FindProperty("_BaseMap", properties);
            MaterialProperty _AlphaMask = FindProperty("_AlphaMask", properties);
            MaterialProperty _Occlusion = FindProperty("_Occlusion", properties);

            materialEditor.ShaderProperty(_BaseMap, _BaseMap.displayName);
            materialEditor.ShaderProperty(_AlphaMask, _AlphaMask.displayName);
            materialEditor.ShaderProperty(_Occlusion, _Occlusion.displayName);

        }
        protected void ShowFurProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {

            MaterialProperty _FurMap = FindProperty("_FurMap", properties);
            MaterialProperty _ShellAmount = FindProperty("_ShellAmount", properties);
            MaterialProperty _ShellStep = FindProperty("_ShellStep", properties);
            MaterialProperty _AlphaCutout = FindProperty("_AlphaCutout", properties);

            showFurProperties = EditorPrefs.GetBool("FurShaderUnLitInspector_ShowFurProperties", false);
            showFurProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showFurProperties, "Fur");
            if (showFurProperties)
            {

                materialEditor.ShaderProperty(_FurMap, _FurMap.displayName);
                materialEditor.ShaderProperty(_ShellAmount, _ShellAmount.displayName);
                materialEditor.ShaderProperty(_ShellStep, _ShellStep.displayName);
                materialEditor.ShaderProperty(_AlphaCutout, _AlphaCutout.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderUnLitInspector_ShowFurProperties", showFurProperties);
        }


        protected void ShowWindProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            MaterialProperty _BaseMove = FindProperty("_BaseMove", properties);
            MaterialProperty _WindFreq = FindProperty("_WindFreq", properties);
            MaterialProperty _WindMove = FindProperty("_WindMove", properties);

            showWindProperties = EditorPrefs.GetBool("FurShaderUnLitInspector_showWindProperties", false);
            showWindProperties = EditorGUILayout.BeginFoldoutHeaderGroup(showWindProperties, "Wind");
            if (showWindProperties)
            {
                materialEditor.ShaderProperty(_BaseMove, _BaseMove.displayName);
                materialEditor.ShaderProperty(_WindFreq, _WindFreq.displayName);
                materialEditor.ShaderProperty(_WindMove, _WindMove.displayName);

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorPrefs.SetBool("FurShaderUnLitInspector_showWindProperties", showWindProperties);
        }


    }

}

#endif
