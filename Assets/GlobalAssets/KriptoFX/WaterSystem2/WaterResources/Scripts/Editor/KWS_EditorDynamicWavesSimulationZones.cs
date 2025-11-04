#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static KWS.KWS_DynamicWavesSimulationZone;
using static KWS.KWS_EditorUtils;


namespace KWS
{
    [CustomEditor(typeof(KWS_DynamicWavesSimulationZone))]
    public class KWS_EditorDynamicWavesSimulationZones : Editor
    {
        private KWS_DynamicWavesSimulationZone _target;

        private static  string _usedSelectedCachePath;
        
        GUIStyle     _perfStyle;
        private bool _isZoneAllowed;
        
        public override void OnInspectorGUI()
        {
            _target      = (KWS_DynamicWavesSimulationZone)target;
            
            Undo.RecordObject(_target, "Changed Dynamic Waves Simulation Zone");
            
            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 220;
          
            bool defaultVal       = false;
            EditorGUILayout.Space(10);  
          
            
            ZoneLimitSettings();
            KWS2_Tab(ref _target.ShowSimulationSettings, false, false, ref defaultVal, "Simulation Settings", SimulationSettings, WaterSystem.WaterSettingsCategory.SimulationZone, foldoutSpace: 14);
            KWS2_TabWithEnabledToogle(ref _target.UseFoamParticles, ref _target.ShowFoamParticlesSettings, useExpertButton: false, ref defaultVal, "Foam Particles", FoamParticlesSettings, WaterSystem.WaterSettingsCategory.SimulationZone, foldoutSpace: 14);
            KWS2_TabWithEnabledToogle(ref _target.UseSplashParticles, ref _target.ShowSplashSettings, useExpertButton: false, ref defaultVal, "Splash Particles", SplashParticlesSettings, WaterSystem.WaterSettingsCategory.SimulationZone, foldoutSpace: 14);

            if(_target.ZoneType != SimulationZoneTypeMode.MovableZone) BakeSettings();

            if (EditorGUI.EndChangeCheck())
            {
                _target.ValueChanged();
                EditorUtility.SetDirty(_target);
            }

        }

        void ZoneLimitSettings()
        {
            _isZoneAllowed = _target.IsZoneAllowed();
            if (!_isZoneAllowed)
            {
                EditorGUILayout.HelpBox("Selected zone is too large and exceeds GPU memory limits and performance. Reduce zone size or Simulation Resolution", MessageType.Error);
                GUI.enabled = false;
                Line();
            }
        }

        void SimulationPerformanceInfo()
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space(-12);  
            
            var renderedPixels = _target.TextureSize.x * _target.TextureSize.y * 7; //data, addData, ColorData, Mask, DepthMask, MaskColor, Normals
            float referencePixels = _target.MaxSimulationTexturePixels * 7f; // baseline zone
            float relativeLoad    = renderedPixels                     / referencePixels;

            GetLoadInfo(relativeLoad, "Simulation load");
            
            string FormatNumber(int value)
            {
                if (value >= 1_000_000)
                    return (value / 1_000_000f).ToString("0.#") + " million";
                if (value >= 1_000)
                    return (value / 1_000f).ToString("0.#") + " thousand";
                return value.ToString();
            }
            
            string displayValue = FormatNumber(renderedPixels);
            
            EditorGUILayout.LabelField("Rendered pixels:  " + displayValue, KWS_EditorUtils.NotesLabelStyleFade);
            
            GUILayout.EndHorizontal();
        }

        void FoamPerformanceInfo()
        {
            float maxParticles  = (int)FoamParticlesMaxLimitEnum._1Million;
            float particlesLoad = 1f * (int)_target.MaxFoamParticlesBudget / maxParticles;
            float emissionLoad  = Mathf.Max(_target.RiverEmissionRateFoam, _target.ShorelineEmissionRateFoam);
            
            float minLoad = 0.2f;
            float relativeLoad = minLoad + (1f - minLoad) * emissionLoad;
            relativeLoad *= particlesLoad;
           
            GetLoadInfo(relativeLoad, "Foam particle load");
        }
        
        void SplashPerformanceInfo()
        {
            float maxParticles  = (int)SplashParticlesMaxLimitEnum._50k;
            float particleRatio = (float)_target.MaxSplashParticlesBudget / maxParticles;

            float emissionRate      = Mathf.Clamp01(Mathf.Max(_target.RiverEmissionRateSplash, _target.ShorelineEmissionRateSplash));
            float nonlinearEmission = Mathf.Pow(emissionRate, 0.5f);
            
            float minBaseLoad = 0.02f;
            float baseLoad    = Mathf.Lerp(minBaseLoad, 1f, emissionRate) * particleRatio;

            // ---------------- Receive Shadows ----------------
            float shadowReceiveMultiplier = _target.ReceiveShadowMode switch
            {
                SplashReceiveShadowModeEnum.Disabled               => 0f,
                SplashReceiveShadowModeEnum.DirectionalLowQuality  => 0.25f,
                SplashReceiveShadowModeEnum.DirectionalHighQuality => 2.5f,
                SplashReceiveShadowModeEnum.AllShadowsLowQuality   => 0.5f,
                SplashReceiveShadowModeEnum.AllShadowsHighQuality  => 4f,
                _                                                  => 0f
            };

            // ---------------- Cast Shadows ----------------
            float shadowCastMultiplier = _target.CastShadowMode switch
            {
                SplashCasticShadowModeEnum.Disabled    => 0f,
                SplashCasticShadowModeEnum.LowQuality  => 1.5f,
                SplashCasticShadowModeEnum.HighQuality => 4f,
                _                                      => 0f
            };

            // ---------------- Final load ----------------

            float shadowCastWeight    = 0.4f;
            float shadowReceiveWeight = 0.4f;

            float shadowCost  = baseLoad * shadowCastMultiplier    * shadowCastWeight;
            float receiveCost = baseLoad * shadowReceiveMultiplier * shadowReceiveWeight;

            float totalLoad = baseLoad + shadowCost + receiveCost;

            GetLoadInfo(totalLoad, "Splash particle load");
        }

        private void GetLoadInfo(float relativeLoad, string text)
        { 
            GUILayout.Space(4);
            string performanceText;
            Color  color;

            if (relativeLoad < 0.25f)
            {
                performanceText = "Low";
                color           = Color.green;
            }
            else if (relativeLoad < 0.5f)
            {
                performanceText = "Medium";
                color           = Color.yellow;
            }
            else if (relativeLoad < 0.75f)
            {
                performanceText = "High";
                color           = new Color(1f, 0.55f, 0f);
            }
            else
            {
                performanceText = "Extreme";
                color           = new Color(1f, 0.1f, 0.1f);
            }

            if (_perfStyle == null) _perfStyle = new GUIStyle(EditorStyles.label);
            _perfStyle.normal.textColor = color;
            _perfStyle.fontStyle        = FontStyle.Bold;

            float labelWidth = GUI.skin.label.CalcSize(new GUIContent(text + ":")).x + 4f;

         
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label(text + ": ",                                       EditorStyles.label, GUILayout.Width(labelWidth));
            GUILayout.Label($"{performanceText} ({(relativeLoad * 100f):0}%)", _perfStyle);
           
            GUILayout.EndHorizontal();
            

        }

        private void BakeSettings()
        {  
            EditorGUILayout.Space(20);

            var isHorizontalUsed = false;
            EditorGUILayout.BeginHorizontal();
            isHorizontalUsed = true;
            
            EditorGUI.BeginChangeCheck();
            
                GUI.enabled = _target.IsBakeMode == false;
                    var isStartBakingPressed = GUILayout.Toggle(false, "Start Cache", "Button");
                GUI.enabled = true;
                
            if (EditorGUI.EndChangeCheck())
            {
                if (isStartBakingPressed)
                {
                    if (EditorUtility.DisplayDialog("Start Precomputation?", "This will overwrite the existing simulation cache. Do you want to continue?",
                                                    "Start", "Cancel"))
                    {
                        ClearSimulationCache();
                        StartBaking(_target);
                    }
                }
            }
            
            EditorGUI.BeginChangeCheck();
            
                GUI.enabled = _target.IsBakeMode == true;
                    var isEndBakingPresset  = GUILayout.Toggle(false, "Stop & Save", "Button");
                GUI.enabled = true;
                
            if (EditorGUI.EndChangeCheck())
            {
                if (isEndBakingPresset)
                {
                    StopBaking(_target);
                }
            }
            
            
            EditorGUI.BeginChangeCheck();
            var isClearCache = GUILayout.Toggle(false, "Clear Cache", "Button");
            if (EditorGUI.EndChangeCheck())
            {
                if (isClearCache && EditorUtility.DisplayDialog("Confirm Deletion",
                                                            "Are you sure you want to delete the precomputed cache textures?",
                                                            "Yes",
                                                            "Cancel"))
                {
                    
                    ClearSimulationCache();
                    _target.ForceUpdateZone();
                }
                
            }

            if (WaterSystem.Instance)
            {
                WaterSystem.Instance.AutoUpdateIntersections = GUILayout.Toggle(WaterSystem.Instance.AutoUpdateIntersections, "Auto Update Intersections", "Button");
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Toggle(false, "Auto Update Intersections", "Button");
                GUI.enabled = true;
            }
            
            if(isHorizontalUsed) EditorGUILayout.EndHorizontal();
           
            EditorGUILayout.LabelField("Cache Path:  " + _usedSelectedCachePath, KWS_EditorUtils.NotesLabelStyleFade);
            
        }

        void SimulationSettings()
        {
            SimulationPerformanceInfo();
            Line();
            var isBakedSim = _target.SavedDepth != null;

            if (isBakedSim)
            {
                GUI.enabled = false;
                EditorGUILayout.HelpBox("You can't change some parameters of a precomputed simulation. " + Environment.NewLine +
                                        "Clear the simulation or recompute it again with new parameters", MessageType.Info);
            }
            _target.ZoneType = (SimulationZoneTypeMode)EnumPopup("Zone Type", "", _target.ZoneType, "");
            if (_target.ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                _target.FollowObject = (GameObject)EditorGUILayout.ObjectField(_target.FollowObject, typeof(GameObject), true);
            }
            
            var layerNames = new List<string>();
            for (int i = 0; i <= 31; i++)
            {
                var maskName = LayerMask.LayerToName(i);
                if(maskName != String.Empty) layerNames.Add(maskName);
            }
            _target.IntersectionLayerMask        = MaskField("Intersection Layer Mask", "", _target.IntersectionLayerMask, layerNames.ToArray(), "");

            if (!_isZoneAllowed) GUI.enabled = true;
            _target.SimulationResolutionPerMeter = Slider("Simulation Resolution Per Meter", "", _target.SimulationResolutionPerMeter, 2, 10, "", false);
            if (!_isZoneAllowed) GUI.enabled = false;
            
            if (isBakedSim)
            {
                GUI.enabled = true;
            }
            
            _target.FlowSpeedMultiplier = Slider("Flow Speed Multiplier", "", _target.FlowSpeedMultiplier, 0.5f, 1.5f, "", false);
            
            Line();

            if (_target.ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                _target.FoamType = FoamTypeEnum.FlowMap;
            }
            else
            {
                _target.FoamType = (FoamTypeEnum)EnumPopup("Foam Type", "", _target.FoamType, "");
            }
            
            _target.FoamStrengthRiver       = Slider("Shallow (River) Foam Strength", "", _target.FoamStrengthRiver,       0.001f, 1.0f, "", false);
            _target.FoamDisappearSpeedRiver = Slider("Shallow (River) Foam Fade",     "", _target.FoamDisappearSpeedRiver, 0.1f,   1.0f, "", false);
            Line();
            _target.FoamStrengthShoreline    = Slider("Ocean Foam Strength",     "", _target.FoamStrengthShoreline,    0.001f, 1.0f, "", false);
            _target.FoamDisappearSpeedShoreline = Slider("Ocean Foam Fade", "", _target.FoamDisappearSpeedShoreline, 0.1f, 1.0f, "", false);
        }
        
        void FoamParticlesSettings()
        {
           FoamPerformanceInfo();
           Line();
            
           _target.MaxFoamParticlesBudget       = (FoamParticlesMaxLimitEnum)EnumPopup("Max Particles", "", _target.MaxFoamParticlesBudget, "");
           _target.MaxFoamRenderingDistance     = Slider("Max Rendering Distance",               "", _target.MaxFoamRenderingDistance,     100, 500, "", false);
           Line();
           
           _target.FoamParticlesScale           = Slider("Particles Scale",            "", _target.FoamParticlesScale,           0.25f, 2f, "", false);
           _target.FoamParticlesAlphaMultiplier = Slider("Particles Alpha Multiplier", "", _target.FoamParticlesAlphaMultiplier, 0.1f, 1f, "", false);
           Line();
           
           _target.RiverEmissionRateFoam        = Slider("Shallow (River) Emission Rate",        "", _target.RiverEmissionRateFoam,        0f, 1f, "", false);
           _target.ShorelineEmissionRateFoam    = Slider("Ocean Emission Rate",    "", _target.ShorelineEmissionRateFoam,    0f, 1f, "", false);
           _target.UsePhytoplanktonEmission     = Toggle("Phytoplankton Glow", "", _target.UsePhytoplanktonEmission, "", false);
        }
        
        void SplashParticlesSettings()
        {
            SplashPerformanceInfo();
            Line();
            
            _target.MaxSplashParticlesBudget       = (SplashParticlesMaxLimitEnum)EnumPopup("Max Particles", "", _target.MaxSplashParticlesBudget, "");
            _target.MaxSplashRenderingDistance     = Slider("Max Rendering Distance",               "", _target.MaxSplashRenderingDistance,     100, 1000, "", false);
            Line();
            
            _target.SplashParticlesScale           = Slider("Particles Scale",            "", _target.SplashParticlesScale,           0.1f,  2f,  "", false);
            _target.SplashParticlesAlphaMultiplier = Slider("Particles Alpha Multiplier", "", _target.SplashParticlesAlphaMultiplier, 0.1f,  1f,  "", false);
            Line();
            
            _target.RiverEmissionRateSplash        = Slider("Shallow (River) Emission Rate",        "", _target.RiverEmissionRateSplash,        0f,  1f,  "", false);
            _target.ShorelineEmissionRateSplash    = Slider("Ocean Emission Rate",    "", _target.ShorelineEmissionRateSplash,    0f,  1f,  "", false);
            _target.WaterfallEmissionRateSplash    = Slider("Waterfall Emission Rate",    "", _target.WaterfallEmissionRateSplash,    0f,  1f,  "", false);
            
            Line();
            _target.ReceiveShadowMode = (SplashReceiveShadowModeEnum)EnumPopup("Receive Shadow Mode", "", _target.ReceiveShadowMode, "");
            _target.CastShadowMode    = (SplashCasticShadowModeEnum)EnumPopup("Cast Shadow Mode",     "", _target.CastShadowMode, "");
        }

     
        public static void StartBaking(KWS_DynamicWavesSimulationZone zone)
        {
            if (!zone.transform) return;
            //save textures to disk

            zone.IsBakeMode = true;
            
            zone.ForceUpdateZone();
        }

        public static void StopBaking(KWS_DynamicWavesSimulationZone zone)
        {
            zone.IsBakeMode = false;
            
            if (zone._bakeDepthRT)
            {
                SaveBakedTextures(zone);
            }
        }
        
        public static void SetSaveFolderPath(string assetRelativePath)
        {
            _usedSelectedCachePath = assetRelativePath;
        }
        
        public void ClearSimulationCache()
        {
            TryDelete(_target.SavedDepth);
            TryDelete(_target.SavedDistanceField);
            TryDelete(_target.SavedDynamicWavesSimulation);

            _target.SavedDepth                  = null;
            _target.SavedDistanceField          = null;
            _target.SavedDynamicWavesSimulation = null;

            UnityEditor.AssetDatabase.Refresh();
            
        }  
            
        void TryDelete(Texture2D texture)
        {
            if (texture == null) return;

            var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(path))
            {
                UnityEditor.AssetDatabase.DeleteAsset(path);
            }
        }

   

        static void UpdateSaveFolderPath(KWS_DynamicWavesSimulationZone zone, bool requireSaveFolderPanel)
        {
            if (zone && zone.SavedDepth)
            {
                if (String.IsNullOrEmpty(_usedSelectedCachePath))
                {
                    _usedSelectedCachePath = UnityEditor.AssetDatabase.GetAssetPath(zone.SavedDepth);
                    if (!String.IsNullOrEmpty(_usedSelectedCachePath))
                    {
                        _usedSelectedCachePath = Path.GetDirectoryName(Path.GetRelativePath("Assets", Path.Combine("Assets", _usedSelectedCachePath)));
                    }
                }
                
            }

            if (requireSaveFolderPanel && String.IsNullOrEmpty(_usedSelectedCachePath))
            {
                _usedSelectedCachePath = UnityEditor.EditorUtility.SaveFolderPanel("Save texture location", _usedSelectedCachePath, "");
            }

        }


        private static void SaveBakedTextures(KWS_DynamicWavesSimulationZone zone)
        {
            UpdateSaveFolderPath(zone, requireSaveFolderPanel: true);
            
            if (String.IsNullOrEmpty(_usedSelectedCachePath))
            {
                return;
            }

            var randFileName = Path.GetRandomFileName().Substring(0, 5).ToUpper();

            var depthPath    = Path.Combine(_usedSelectedCachePath, "DepthTexture_"           + randFileName);
            var sdfDepthPath = Path.Combine(_usedSelectedCachePath, "DistanceFieldTexture_"   + randFileName);
            var simDataPath  = Path.Combine(_usedSelectedCachePath, "DynamicWavesSimulation_" + randFileName);

            zone._bakeDepthRT.SaveRenderTextureDepth32(depthPath);
            zone._bakeDepthSdfRT.SaveRenderTexture(sdfDepthPath);
            zone._simulationData.GetTarget.rt.SaveRenderTexture(simDataPath);

            zone.SavedDepth                  = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(depthPath.GetRelativeToAssetsPath()    + ".kwsTexture");
            zone.SavedDistanceField          = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(sdfDepthPath.GetRelativeToAssetsPath() + ".kwsTexture");
            zone.SavedDynamicWavesSimulation = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(simDataPath.GetRelativeToAssetsPath()  + ".kwsTexture");
        }



        void OnEnable()
        {
            foreach (var iZone in KWS_TileZoneManager.DynamicWavesZones)
            { 
                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                if(zone.SavedDepth) UpdateSaveFolderPath(zone, false);
            }
            
        }

    }
    
    
    
    [InitializeOnLoad]
    static class MoveDetector
    { 
        static HashSet<KWS_TileZoneManager.IWaterZone> pendingZones = new();
        static double                                  lastUpdateTime;
        
        static MoveDetector()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;
            
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!WaterSystem.Instance || !WaterSystem.Instance.AutoUpdateIntersections) return modifications;
            
            foreach (var mod in modifications)
            {
                var prop = mod.currentValue;
                if (prop == null || prop.target == null) continue;

                if (prop.target is not Transform tr) continue;
                
                foreach (var dynamicWavesZone in KWS_TileZoneManager.DynamicWavesZones)
                {
                    if (dynamicWavesZone.Bounds.Contains(tr.position))
                    {
                        pendingZones.Add(dynamicWavesZone);
                    }
                }
            }

            
            return modifications;
        } 
        
        private static void EditorUpdate()
        {
            if (pendingZones.Count == 0) return;
           
            double time = EditorApplication.timeSinceStartup;
            if (time - lastUpdateTime < 0.5) return;

            foreach (var iZone in pendingZones)
            {
                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                if (zone && zone.ZoneType != SimulationZoneTypeMode.MovableZone)
                {
                    zone.ForceUpdateZone(false);
                }
            }

            pendingZones.Clear();
            lastUpdateTime = time;
        }
    }
}

#endif