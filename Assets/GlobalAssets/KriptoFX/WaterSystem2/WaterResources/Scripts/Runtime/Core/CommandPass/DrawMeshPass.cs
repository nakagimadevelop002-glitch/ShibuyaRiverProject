using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static KWS.KWS_ShaderConstants;

namespace KWS
{
    internal class DrawMeshPass : WaterPass
    {
        internal override string       PassName => "Water.DrawMeshPass";
        private           RenderParams _renderParams = new RenderParams();
        private           RenderParams _customMeshRenderParams;
        Material                       _drawMeshMaterial;
        Material                       _drawCustomMeshMaterial;

        public DrawMeshPass()
        {
           _drawMeshMaterial = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterShaderName, useWaterStencilMask: true);
           _drawMeshMaterial.SetKeyword("KWS_USE_WATER_INSTANCING", true);
           
           _drawCustomMeshMaterial = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterShaderName, useWaterStencilMask: true);
           _drawCustomMeshMaterial.SetKeyword("KWS_USE_WATER_INSTANCING", false);
        }

        public override void Release()
        {
            KW_Extensions.SafeDestroy(_drawMeshMaterial, _drawCustomMeshMaterial);
            _drawMeshMaterial       = null;
            _drawCustomMeshMaterial = null;
            //KW_Extensions.WaterLog(this, "Release", KW_Extensions.WaterLogMessageType.Release);
        }


        public override void ExecuteBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
            if (cam == null || _drawMeshMaterial == null) return;
           
            _renderParams.camera               = cam;
            _renderParams.material             = _drawMeshMaterial;
            _renderParams.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
            _renderParams.worldBounds          = WaterSystem.Instance.WorldSpaceBounds;
            _renderParams.renderingLayerMask   = GraphicsSettings.defaultRenderingLayerMask;
            _renderParams.layer                = KWS_Settings.Water.WaterLayer;
           
            DrawInstancedQuadTree(cam, WaterSystem.Instance, _drawMeshMaterial, false);

            _customMeshRenderParams.camera               = cam;
            _customMeshRenderParams.material             = _drawCustomMeshMaterial;
            _customMeshRenderParams.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
            _customMeshRenderParams.worldBounds          = WaterSystem.Instance.WorldSpaceBounds;
            _customMeshRenderParams.renderingLayerMask   = GraphicsSettings.defaultRenderingLayerMask;
            _customMeshRenderParams.layer                = KWS_Settings.Water.WaterLayer;
            
            DrawCustomMeshes();
        }

        private void DrawCustomMeshes()
        {
            var localZones = KWS_TileZoneManager.VisibleLocalWaterZones;
            foreach (var iZone in localZones)
            {
                var zone = (KWS_LocalWaterZone)iZone;
                if (zone.OverrideMesh && zone.CustomMesh)
                {
                    Graphics.RenderMesh(_customMeshRenderParams, zone.CustomMesh, 0, zone.CachedFittedMatrix);
                }
            }
        }


        public void DrawInstancedQuadTree(Camera cam, WaterSystem waterInstance, Material mat, bool isPrePass)
        {
            waterInstance._meshQuadTree.UpdateQuadTree(cam, waterInstance, forceUpdate:false);
            var isFastMode = isPrePass && !WaterSystem.IsCameraPartialUnderwater;
            if (!waterInstance._meshQuadTree.TryGetRenderingContext(cam, isFastMode, out var context)) return;

            mat.SetBuffer(StructuredBuffers.InstancedMeshData, context.visibleChunksComputeBuffer);

            if (_renderParams.camera == null || _renderParams.material == null || context.chunkInstance == null || context.visibleChunksArgs == null || context.visibleChunksArgs.count == 0)
            {
                Debug.LogError($"Water draw mesh rendering error: {_renderParams.camera}, { _renderParams.material}, {context.chunkInstance}, {context.visibleChunksArgs}");
                return;
            }

            Graphics.RenderMeshIndirect(_renderParams, context.chunkInstance, context.visibleChunksArgs);
        }
        
    }
}