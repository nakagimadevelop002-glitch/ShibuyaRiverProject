using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Create seaweed mesh optimized for WindZone shaders")]
public class SeaweedCreatorMCPTool
{
    [McpServerTool, Description("Create a single realistic seaweed blade with sword-like shape")]
    public async ValueTask<string> CreateSeaweed(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Height of seaweed blade")] float height = 2f,
        [Description("Width of seaweed blade")] float width = 0.4f,
        [Description("Thickness of blade")] float thickness = 0.1f,
        [Description("Number of vertical segments")] int segments = 12,
        [Description("Seaweed object name")] string name = "Seaweed")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create GameObject
            GameObject seaweedObj = new GameObject(name);
            seaweedObj.transform.position = new Vector3(x, y, z);

            // Add required components
            MeshFilter meshFilter = seaweedObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = seaweedObj.AddComponent<MeshRenderer>();

            // Generate realistic sword-shaped mesh
            Mesh seaweedMesh = GenerateRealisticSeaweedMesh(height, width, thickness, segments);
            meshFilter.mesh = seaweedMesh;

            // Set default material (can be replaced with WindZone shader later)
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = new Color(0.2f, 0.6f, 0.3f, 0.8f); // Green with transparency
            defaultMaterial.SetFloat("_Mode", 3); // Transparent mode
            defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            defaultMaterial.SetInt("_ZWrite", 0);
            defaultMaterial.DisableKeyword("_ALPHATEST_ON");
            defaultMaterial.EnableKeyword("_ALPHABLEND_ON");
            defaultMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            defaultMaterial.renderQueue = 3000;
            
            meshRenderer.material = defaultMaterial;

            Debug.Log($"Created realistic seaweed '{name}' at ({x}, {y}, {z}) - Height: {height}, Width: {width}, Thickness: {thickness}");
            return $"Successfully created realistic seaweed '{name}' with {segments} segments";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create seaweed: {e.Message}");
            throw;
        }
    }

    private Mesh GenerateRealisticSeaweedMesh(float height, float width, float thickness, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "RealisticSeaweedMesh";

        // 3 vertices per segment: left, center (ridge), right
        Vector3[] vertices = new Vector3[(segments + 1) * 3];
        Vector2[] uvs = new Vector2[(segments + 1) * 3];
        
        // Calculate triangles: 4 triangles per segment (2 for each side)
        int[] triangles = new int[segments * 12];

        // Generate vertices and UVs
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float yPos = height * t;
            
            // Natural taper: wider at base, pointed at tip
            float currentWidth = width * (1.0f - t * 0.8f) * Mathf.Max(0.1f, 1.0f - t);
            float currentThickness = thickness * (1.0f - t * 0.6f) * Mathf.Max(0.2f, 1.0f - t);
            
            // Add subtle curve for natural look
            float curve = Mathf.Sin(t * Mathf.PI * 0.3f) * 0.1f;
            
            int baseIndex = i * 3;
            
            // Left vertex
            vertices[baseIndex] = new Vector3(-currentWidth * 0.5f + curve, yPos, -currentThickness * 0.3f);
            uvs[baseIndex] = new Vector2(0, t);
            
            // Center vertex (ridge/midline) - slightly forward for 3D effect
            vertices[baseIndex + 1] = new Vector3(curve, yPos, currentThickness * 0.5f);
            uvs[baseIndex + 1] = new Vector2(0.5f, t);
            
            // Right vertex  
            vertices[baseIndex + 2] = new Vector3(currentWidth * 0.5f + curve, yPos, -currentThickness * 0.3f);
            uvs[baseIndex + 2] = new Vector2(1, t);
        }

        // Generate triangles for sword-like shape
        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i * 3;
            int next = (i + 1) * 3;

            // Left side triangles (left-center-next_left, center-next_center-next_left)
            triangles[triIndex++] = current;        // left
            triangles[triIndex++] = current + 1;    // center  
            triangles[triIndex++] = next;           // next_left
            
            triangles[triIndex++] = current + 1;    // center
            triangles[triIndex++] = next + 1;       // next_center
            triangles[triIndex++] = next;           // next_left

            // Right side triangles (center-right-next_right, center-next_right-next_center)
            triangles[triIndex++] = current + 1;    // center
            triangles[triIndex++] = current + 2;    // right
            triangles[triIndex++] = next + 2;       // next_right
            
            triangles[triIndex++] = current + 1;    // center
            triangles[triIndex++] = next + 2;       // next_right
            triangles[triIndex++] = next + 1;       // next_center
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        // Calculate normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}