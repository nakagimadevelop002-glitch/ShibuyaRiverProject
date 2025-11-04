#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static KWS.KWS_EditorUtils;
using Description = KWS.KWS_EditorTextDescription;
using link = KWS.KWS_EditorUrlLinks;

namespace KWS
{
    [CustomEditor(typeof(KWS_LocalWaterZone))]
    internal class KWS_EditorLocalWaterZone : Editor
    {
        private KWS_LocalWaterZone _target;
        public override void OnInspectorGUI()
        {
            //var isChanged = DrawDefaultInspector();
            _target = (KWS_LocalWaterZone)target;
            
            Undo.RecordObject(_target, "Changed Local Water Zone");

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 220;
          
            bool defaultVal       = false;
            EditorGUILayout.Space(20);  
            
            if (_target.OverrideColorSettings &&  _target.UseSphericalBlending)
            { 
                EditorGUILayout.HelpBox("Spherical blending is enabled, so the scale is always in the shape of a cube", MessageType.Info);
            }
           
            KWS2_TabWithEnabledToogle(ref _target.OverrideColorSettings, ref _target.ShowColorSettings, useExpertButton: false, ref defaultVal, "Override Color", ColorSettings, WaterSystem.WaterSettingsCategory.LocalZone, foldoutSpace: 14);
            KWS2_TabWithEnabledToogle(ref _target.OverrideWindSettings, ref _target.ShowWindSettings, useExpertButton: false, ref defaultVal, "Override Wind", WindSettings, WaterSystem.WaterSettingsCategory.LocalZone, foldoutSpace: 14);

            if (_target.OverrideMesh)
            {
                GUI.enabled            = false;
                _target.OverrideHeight = false;
                KWS2_TabWithEnabledToogle(ref _target.OverrideHeight, ref _target.ShowHeightSettings, useExpertButton: false, ref defaultVal, "Override Height (Can't be used due to active Override Mesh)", HeightSettings, WaterSystem.WaterSettingsCategory.LocalZone, foldoutSpace: 14);
                GUI.enabled = true;
            }
            else
            {
                KWS2_TabWithEnabledToogle(ref _target.OverrideHeight, ref _target.ShowHeightSettings, useExpertButton: false, ref defaultVal, "Override Height", HeightSettings, WaterSystem.WaterSettingsCategory.LocalZone, foldoutSpace: 14);
            }
            
            
            EditorGUI.BeginChangeCheck();
            KWS2_TabWithEnabledToogle(ref _target.OverrideMesh, ref _target.ShowMeshSettings, useExpertButton: false, ref defaultVal, "Override Mesh (Experimental)", MeshSettings, WaterSystem.WaterSettingsCategory.LocalZone, foldoutSpace: 14);
            if (EditorGUI.EndChangeCheck()) { _target.UpdateTransform(); }
            
          
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_target);
                // AssetDatabase.SaveAssets();
            }

        }

        void ColorSettings()
        {
           
            _target.Transparent          = Slider("Transparent (Meters)", Description.Color.Transparent, _target.Transparent, 2.0f, 100f, link.Transparent);
            _target.WaterColor           = ColorField("Water Color", Description.Color.WaterColor, _target.WaterColor, false, false, false, link.WaterColor);
            _target.TurbidityColor       = ColorField("Turbidity Color", Description.Color.TurbidityColor, _target.TurbidityColor, false, false, false, link.TurbidityColor);
            _target.UseSphericalBlending = Toggle("Use Sphere Blending", "", _target.UseSphericalBlending, "", false);
           
        }
        
        void WindSettings()
        {
            _target.WindStrengthMultiplier = Slider("Strength Multiplier", "", _target.WindStrengthMultiplier, 0, 1, "", false);
            _target.WindEdgeBlending       = Slider("Edge Blending", "", _target.WindEdgeBlending, 0, 1, "", false);
        }
        
        void HeightSettings()
        {
            _target.ClipWaterBelowZone = Toggle("Clip Water Below Zone", "", _target.ClipWaterBelowZone , "", false);
            _target.HeightEdgeBlending = Slider("Edge Blending", "", _target.HeightEdgeBlending, 0, 1, "", false);
        }
        
        void MeshSettings()
        {
            
            _target.CustomMesh               = (Mesh) EditorGUILayout.ObjectField("Custom Mesh", _target.CustomMesh, typeof(Mesh), true);
            _target.CustomMeshRotationOffset = KWS_EditorUtils.Vector3Field("Rotation Offset", "", _target.CustomMeshRotationOffset, "");
        }
    }
}

#endif