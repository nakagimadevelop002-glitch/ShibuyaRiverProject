using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Create various custom shape meshes (triangle, circle, polygon)")]
public class CustomShapeMCPTool
{
    [McpServerTool, Description("Create a triangular plane mesh")]
    public async ValueTask<string> CreateTrianglePlane(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Triangle side length")] float size = 2f,
        [Description("Triangle object name")] string name = "Triangle")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create GameObject
            GameObject triangleObj = new GameObject(name);
            triangleObj.transform.position = new Vector3(x, y, z);

            // Add required components
            MeshFilter meshFilter = triangleObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = triangleObj.AddComponent<MeshRenderer>();

            // Generate triangle mesh
            Mesh triangleMesh = GenerateTriangleMesh(size);
            meshFilter.mesh = triangleMesh;

            // Set default material
            Material defaultMaterial = CreateDefaultMaterial(Color.cyan);
            meshRenderer.material = defaultMaterial;

            Debug.Log($"Created triangle '{name}' at ({x}, {y}, {z}) - Size: {size}");
            return $"Successfully created triangle '{name}' with side length {size}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create triangle: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("Create a circular plane mesh")]
    public async ValueTask<string> CreateCirclePlane(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Circle radius")] float radius = 1f,
        [Description("Number of segments for smoothness")] int segments = 32,
        [Description("Circle object name")] string name = "Circle")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create GameObject
            GameObject circleObj = new GameObject(name);
            circleObj.transform.position = new Vector3(x, y, z);

            // Add required components
            MeshFilter meshFilter = circleObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = circleObj.AddComponent<MeshRenderer>();

            // Generate circle mesh
            Mesh circleMesh = GenerateCircleMesh(radius, segments);
            meshFilter.mesh = circleMesh;

            // Set default material
            Material defaultMaterial = CreateDefaultMaterial(Color.green);
            meshRenderer.material = defaultMaterial;

            Debug.Log($"Created circle '{name}' at ({x}, {y}, {z}) - Radius: {radius}, Segments: {segments}");
            return $"Successfully created circle '{name}' with radius {radius} and {segments} segments";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create circle: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("Create a pentagon plane mesh")]
    public async ValueTask<string> CreatePentagonPlane(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Pentagon radius (distance from center to vertex)")] float radius = 1f,
        [Description("Pentagon object name")] string name = "Pentagon")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create GameObject
            GameObject pentagonObj = new GameObject(name);
            pentagonObj.transform.position = new Vector3(x, y, z);

            // Add required components
            MeshFilter meshFilter = pentagonObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = pentagonObj.AddComponent<MeshRenderer>();

            // Generate pentagon mesh
            Mesh pentagonMesh = GeneratePolygonMesh(radius, 5);
            meshFilter.mesh = pentagonMesh;

            // Set default material
            Material defaultMaterial = CreateDefaultMaterial(Color.magenta);
            meshRenderer.material = defaultMaterial;

            Debug.Log($"Created pentagon '{name}' at ({x}, {y}, {z}) - Radius: {radius}");
            return $"Successfully created pentagon '{name}' with radius {radius}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create pentagon: {e.Message}");
            throw;
        }
    }

    private Mesh GenerateTriangleMesh(float size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "TriangleMesh";

        // Equilateral triangle vertices
        float height = size * Mathf.Sqrt(3f) / 2f; // Height of equilateral triangle
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, height / 2f),           // Top vertex
            new Vector3(-size / 2f, 0, -height / 2f), // Bottom left
            new Vector3(size / 2f, 0, -height / 2f)   // Bottom right
        };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0.5f, 1f),  // Top
            new Vector2(0f, 0f),    // Bottom left
            new Vector2(1f, 0f)     // Bottom right
        };

        int[] triangles = new int[] { 0, 2, 1 }; // Clockwise for correct normal

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Mesh GenerateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "CircleMesh";

        // Vertices: center + perimeter points
        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];

        // Center vertex
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        // Perimeter vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            uvs[i + 1] = new Vector2(
                (Mathf.Cos(angle) + 1f) * 0.5f,
                (Mathf.Sin(angle) + 1f) * 0.5f
            );
        }

        // Triangles from center to each edge (clockwise)
        for (int i = 0; i < segments; i++)
        {
            int triIndex = i * 3;
            triangles[triIndex] = 0;                    // Center
            triangles[triIndex + 1] = (i + 1) % segments + 1; // Next vertex
            triangles[triIndex + 2] = i + 1;            // Current vertex
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Mesh GeneratePolygonMesh(float radius, int sides)
    {
        Mesh mesh = new Mesh();
        mesh.name = $"{sides}SidedPolygonMesh";

        // Vertices: center + vertices
        Vector3[] vertices = new Vector3[sides + 1];
        Vector2[] uvs = new Vector2[sides + 1];
        int[] triangles = new int[sides * 3];

        // Center vertex
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        // Polygon vertices
        for (int i = 0; i < sides; i++)
        {
            float angle = i * 2f * Mathf.PI / sides;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            uvs[i + 1] = new Vector2(
                (Mathf.Cos(angle) + 1f) * 0.5f,
                (Mathf.Sin(angle) + 1f) * 0.5f
            );
        }

        // Triangles from center to each edge (clockwise)
        for (int i = 0; i < sides; i++)
        {
            int triIndex = i * 3;
            triangles[triIndex] = 0;                    // Center
            triangles[triIndex + 1] = (i + 1) % sides + 1; // Next vertex
            triangles[triIndex + 2] = i + 1;            // Current vertex
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Material CreateDefaultMaterial(Color color)
    {
        // URP Compatible shader
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (material.shader == null)
        {
            // Fallback to Unlit if Lit not found
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }
        if (material.shader == null)
        {
            // Final fallback
            material = new Material(Shader.Find("Sprites/Default"));
        }

        material.color = color;
        return material;
    }
}