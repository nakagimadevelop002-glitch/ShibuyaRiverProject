#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

using static KWS.KWS_ShaderConstants.WaterKeywords;

namespace KWS
{


    class KWS_ShaderStripping : IPreprocessShaders
    {
        public int   callbackOrder => 0;
        List<string> _keywordsToStrip          = new List<string>();
        static int  _strippingListHash;
        
        void UpdateStrippingList()
        { 
            _keywordsToStrip.Clear(); 
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            if (!_settingsContainer) return;
            
            var allQualityLevels = _settingsContainer.qualityLevelSettings;
          
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UsePlanarReflection),            GlobalKeyword_KWS_USE_PLANAR_REFLECTION.name);
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UseScreenSpaceReflection),       GlobalKeyword_KWS_SSR_REFLECTION.name);
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UseVolumetricLight),             GlobalKeyword_KWS_USE_VOLUMETRIC_LIGHT.name);
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UseCausticEffect),               GlobalKeyword_KWS_USE_CAUSTIC.name);
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UseRefractionDispersion),        GlobalKeyword_KWS_USE_REFRACTION_DISPERSION.name);
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UseCausticHighQualityFiltering), GlobalKeyword_KWS_USE_CAUSTIC_FILTERING.name);
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UseCausticDispersion),           GlobalKeyword_KWS_USE_CAUSTIC_DISPERSION.name);
            
            CheckBoolAndAddToStippingList(allQualityLevels, GetBoolFieldName(x => x.UseDynamicWaves),
                                          GlobalKeyword_KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE.name,
                                          GlobalKeyword_KWS_DYNAMIC_WAVES_VISIBLE_ZONES_1.name,
                                          GlobalKeyword_KWS_DYNAMIC_WAVES_VISIBLE_ZONES_2.name,
                                          GlobalKeyword_KWS_DYNAMIC_WAVES_VISIBLE_ZONES_4.name,
                                          GlobalKeyword_KWS_DYNAMIC_WAVES_VISIBLE_ZONES_8.name,
                                          GlobalKeyword_KWS_DYNAMIC_WAVES_USE_COLOR.name);

            foreach (var keyword in _keywordsToStrip)
            {
                Debug.Log("Stipped water keyword: " + keyword);
            }

            
        }

        void CheckBoolAndAddToStippingList(List<WaterQualityLevelSettings> levels, string boolFieldName, params string[] keywordNames)
        {
            var canStripKeyword = IsBoolDisabledInAllQualityLevels(levels, boolFieldName);
            if (!canStripKeyword) return;
            
            foreach (var keywordName in keywordNames)
            {
                _keywordsToStrip.Add(keywordName);
            }
        }
        
        void CheckEnumAndAddToStrippingList<TEnum>(
            List<WaterQualityLevelSettings>                    levels,
            Expression<Func<WaterQualityLevelSettings, TEnum>> expr,
            TEnum                                              requiredValue,
            string                                             keywordName
        ) where TEnum : Enum
        {
            var canStrip = IsEnumValueDisabledInAllQualityLevels(levels, expr, requiredValue);
            if (canStrip) _keywordsToStrip.Add(keywordName);
        }
        
        bool IsBoolDisabledInAllQualityLevels(List<WaterQualityLevelSettings> levels, string boolFieldName)
        {
            foreach (var level in levels)
            {
                var field = typeof(WaterQualityLevelSettings).GetField(boolFieldName);
                if (field == null || field.FieldType != typeof(bool))
                {
                    Debug.LogError($"Field '{boolFieldName}' not found or not a bool.");
                    return false;   
                }

                bool isEnabled = (bool)field.GetValue(level);
                if (isEnabled) return false;
            }

            return true;
        }
        
        bool IsEnumValueDisabledInAllQualityLevels<TEnum>(List<WaterQualityLevelSettings>                    levels,
                                                          Expression<Func<WaterQualityLevelSettings, TEnum>> expr,
                                                          TEnum                                              targetValue
        ) where TEnum : Enum
        {
            var member = expr.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException("Expression must be a member access");

            string fieldName = member.Member.Name;
            var    field     = typeof(WaterQualityLevelSettings).GetField(fieldName);
            if (field == null || !field.FieldType.IsEnum)
            {
                Debug.LogError($"Field '{fieldName}' not found or not an enum.");
                return false;
            }

            foreach (var level in levels)
            {
                var value = (TEnum)field.GetValue(level);
                if (EqualityComparer<TEnum>.Default.Equals(value, targetValue))
                    return false;
            }

            return true;
        }
        
        public static string GetBoolFieldName(Expression<Func<WaterQualityLevelSettings, bool>> expr)
        {
            if (expr.Body is MemberExpression member)
                return member.Member.Name;

            throw new ArgumentException("Expression is not a member access");
        }
        

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (_strippingListHash != KWS_WaterSettingsRuntimeLoader._settingsHash)
            {
                UpdateStrippingList();
                _strippingListHash = KWS_WaterSettingsRuntimeLoader._settingsHash;
            }
            
            
            if (!shader.name.StartsWith("Hidden/KriptoFX/")) return;

            for (int i = 0; i < data.Count; ++i)
            {
                var keywordSet     = data[i].shaderKeywordSet;
                var activeKeywords = keywordSet.GetShaderKeywords();

                bool shouldStrip = activeKeywords.Any(k => _keywordsToStrip.Contains(k.name));

                if (shouldStrip)
                {
// #if KWS_DEBUG
//                     var debugKeywords = string.Join(" ", activeKeywords.Select(k => k.name));
//                     Debug.Log($"[STRIP] {shader.name} variant removed with keywords: {debugKeywords}");
// #endif
                    data.RemoveAt(i);
                    --i;
                }
            }
        }
    }
}
#endif