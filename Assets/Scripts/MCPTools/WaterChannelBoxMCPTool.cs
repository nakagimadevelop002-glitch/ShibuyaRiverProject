using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;
using UnityEditor;

[McpServerToolType, Description("Create semi-transparent box geometry (water channel guides etc.)")]
public class WaterChannelBoxMCPTool
{
    [McpServerTool, Description("Create a semi-transparent URP box (no collider) at position/scale with given RGBA color. For water channel guides that need to stay visible for adjustment.")]
    public async ValueTask<string> CreateSemiTransparentBox(
        [Description("GameObject name")] string name,
        [Description("Position X")] float px,
        [Description("Position Y")] float py,
        [Description("Position Z")] float pz,
        [Description("Scale X")] float sx,
        [Description("Scale Y")] float sy,
        [Description("Scale Z")] float sz,
        [Description("Color R (0-1)")] float r = 0.2f,
        [Description("Color G (0-1)")] float g = 0.5f,
        [Description("Color B (0-1)")] float b = 1f,
        [Description("Alpha (0-1)")] float a = 0.35f)
    {
        try
        {
            await UniTask.SwitchToMainThread();

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = new Vector3(px, py, pz);
            box.transform.localScale = new Vector3(sx, sy, sz);

            // 当たり判定なし（コライダー削除）
            var col = box.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);

            // URP Lit 半透明マテリアル作成
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);              // 0=Opaque, 1=Transparent
            mat.SetFloat("_Blend", 0f);                // Alpha blend
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(r, g, b, a));
            mat.color = new Color(r, g, b, a);

            string matDir = "Assets/Material";
            if (!Directory.Exists(matDir)) Directory.CreateDirectory(matDir);
            string matPath = $"{matDir}/{name}_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);

            box.GetComponent<MeshRenderer>().sharedMaterial = mat;

            EditorUtility.SetDirty(box);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            return $"SUCCESS: Created semi-transparent box '{name}' at ({px:F2},{py:F2},{pz:F2}) scale ({sx:F2},{sy:F2},{sz:F2}) alpha {a:F2}. Material: {matPath}";
        }
        catch (Exception e)
        {
            Debug.LogError($"CreateSemiTransparentBox failed: {e.Message}");
            return $"ERROR: {e.Message}";
        }
    }
}
