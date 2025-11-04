#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;


namespace KWS
{

    public class KWS_PipelineDefines : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets,
                                           string[] deletedAssets,
                                           string[] movedAssets,
                                           string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.Contains("KriptoFX/WaterSystem2"))
                {
                    #if KWS_DEBUG
                        Debug.Log("KWS2 assets imported, applying keyword setup...");
                    #endif
                    CheckAndUpdateShaderPipelineDefines();
                    break;
                }
            }
        }
       
#if KWS_DEBUG
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RunOnStart()
        { 
           
            CheckPipelineChange();
        }
        
        static void CheckPipelineChange()
        {
            CheckAndUpdateShaderPipelineDefines();
             
        }
#endif
       


        static void UpdatePipelineDefine(KWS_EditorUtils.UnityPipeline actualPipeline)
        {
            var group = BuildTargetGroup.Standalone;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            defines = Remove(defines, "KWS_BUILTIN");
            defines = Remove(defines, "KWS_URP");
            defines = Remove(defines, "KWS_HDRP");

            KWS_EditorUtils.DisableAllShaderTextDefines(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile:false, "KWS_BUILTIN", "KWS_URP", "KWS_HDRP");

            switch (actualPipeline)
            {
                case KWS_EditorUtils.UnityPipeline.Builtin:
                    defines += ";KWS_BUILTIN";
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: false, "KWS_BUILTIN", enabled: true);
                    break;
                case KWS_EditorUtils.UnityPipeline.URP:
                    defines += ";KWS_URP";
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: false, "KWS_URP", enabled: true);
                    break;
                case KWS_EditorUtils.UnityPipeline.HDRP:
                    defines += ";KWS_HDRP";
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: false, "KWS_HDRP", enabled: true);
                    break;
                case KWS_EditorUtils.UnityPipeline.Unknown:
                    Debug.LogError("KWS2 Water Unknown RenderPipeline: ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actualPipeline), actualPipeline, null);
            }
            #if KWS_DEBUG
                        Debug.Log("Water Pipeline: " + actualPipeline + "     defines "+  defines );
            #endif

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
            AssetDatabase.Refresh();
        }

        static  void CheckAndUpdateShaderPipelineDefines()
        {
            var actualPipeline        = KWS_EditorUtils.GetCurrentPipeline();
            var shaderDefine          = KWS_EditorUtils.GetActiveShaderDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines);
            
            var scriptDefine = KWS_EditorUtils.UnityPipeline.Unknown;

            
            #if KWS_BUILTIN
                scriptDefine = KWS_EditorUtils.UnityPipeline.Builtin;
            #endif
            
            #if KWS_URP
                scriptDefine = KWS_EditorUtils.UnityPipeline.URP;
            #endif
            
            #if KWS_HDRP
                scriptDefine = KWS_EditorUtils.UnityPipeline.HDRP;
            #endif

#if KWS_DEBUG
            Debug.Log("actualPipeline " + actualPipeline + "    " + shaderDefine  + "    " + shaderDefine);
#endif

            
            if (actualPipeline != scriptDefine || actualPipeline != shaderDefine || scriptDefine == KWS_EditorUtils.UnityPipeline.Unknown)
            {
                UpdatePipelineDefine(actualPipeline);
            }
        }
        
        static string Remove(string input, string keyword)
        {
            return input.Replace(keyword + ";", "").Replace(";" + keyword, "").Replace(keyword, "");
        }
    }
}
#endif