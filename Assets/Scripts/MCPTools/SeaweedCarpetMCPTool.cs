using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Create custom procedural meshes for various purposes")]
public class SeaweedCarpetMCPTool
{
    [McpServerTool, Description("Create a large seaweed carpet plane with organic variations")]
    public async ValueTask<string> CreateSeaweedCarpet(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Width of carpet")] float width = 10f,
        [Description("Length of carpet")] float length = 10f,
        [Description("Width segments for detail")] int widthSegments = 50,
        [Description("Length segments for detail")] int lengthSegments = 50,
        [Description("Height variation amplitude")] float heightVariation = 0.2f,
        [Description("Carpet object name")] string name = "SeaweedCarpet")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create GameObject
            GameObject carpetObj = new GameObject(name);
            carpetObj.transform.position = new Vector3(x, y, z);

            // Add required components
            MeshFilter meshFilter = carpetObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = carpetObj.AddComponent<MeshRenderer>();

            // Generate organic seaweed carpet mesh
            Mesh carpetMesh = GenerateSeaweedCarpetMesh(width, length, widthSegments, lengthSegments, heightVariation);
            meshFilter.mesh = carpetMesh;

            // Default material - will be replaced with Fiber shader
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = new Color(0.3f, 0.7f, 0.4f, 1f);
            meshRenderer.material = defaultMaterial;

            Debug.Log($"Created seaweed carpet '{name}' at ({x}, {y}, {z}) - Size: {width}x{length}, Segments: {widthSegments}x{lengthSegments}");
            return $"Successfully created seaweed carpet '{name}' with {widthSegments * lengthSegments} segments and organic height variations";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create seaweed carpet: {e.Message}");
            throw;
        }
    }

    private Mesh GenerateSeaweedCarpetMesh(float width, float length, int widthSegments, int lengthSegments, float heightVariation)
    {
        Mesh mesh = new Mesh();
        mesh.name = "SeaweedCarpetMesh";

        // Calculate vertices count
        int vertexCount = (widthSegments + 1) * (lengthSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];

        // Generate vertices with organic variations
        for (int z = 0; z <= lengthSegments; z++)
        {
            for (int x = 0; x <= widthSegments; x++)
            {
                int index = z * (widthSegments + 1) + x;

                // Basic position
                float xPos = ((float)x / widthSegments - 0.5f) * width;
                float zPos = ((float)z / lengthSegments - 0.5f) * length;

                // Organic height variation using Perlin noise
                float height = 0f;
                if (heightVariation > 0f)
                {
                    // Multiple octaves for natural variation
                    height += Mathf.PerlinNoise(xPos * 0.1f, zPos * 0.1f) * heightVariation;
                    height += Mathf.PerlinNoise(xPos * 0.3f, zPos * 0.3f) * heightVariation * 0.3f;
                    height += Mathf.PerlinNoise(xPos * 0.8f, zPos * 0.8f) * heightVariation * 0.1f;
                }

                vertices[index] = new Vector3(xPos, height, zPos);
                uvs[index] = new Vector2((float)x / widthSegments, (float)z / lengthSegments);
                
                // Calculate basic normal (will be recalculated)
                normals[index] = Vector3.up;
            }
        }

        // Generate triangles
        int triangleCount = widthSegments * lengthSegments * 6;
        int[] triangles = new int[triangleCount];
        int triIndex = 0;

        for (int z = 0; z < lengthSegments; z++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                int topLeft = z * (widthSegments + 1) + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * (widthSegments + 1) + x;
                int bottomRight = bottomLeft + 1;

                // First triangle (top-left, bottom-left, top-right)
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;

                // Second triangle (top-right, bottom-left, bottom-right)
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
            }
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        // Calculate proper normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    [McpServerTool, Description("Create an elliptical plane mesh with customizable dimensions")]
    public async ValueTask<string> CreateEllipticalPlane(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Radius along X-axis")] float radiusX = 5f,
        [Description("Radius along Z-axis")] float radiusZ = 3f,
        [Description("Number of radial segments")] int radialSegments = 32,
        [Description("Number of ring segments")] int ringSegments = 8,
        [Description("Height variation amplitude")] float heightVariation = 0f,
        [Description("Object name")] string name = "EllipticalPlane")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create GameObject
            GameObject planeObj = new GameObject(name);
            planeObj.transform.position = new Vector3(x, y, z);

            // Add required components
            MeshFilter meshFilter = planeObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = planeObj.AddComponent<MeshRenderer>();

            // Generate elliptical plane mesh
            Mesh planeMesh = GenerateEllipticalPlaneMesh(radiusX, radiusZ, radialSegments, ringSegments, heightVariation);
            meshFilter.mesh = planeMesh;

            // Default material
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            meshRenderer.material = defaultMaterial;

            Debug.Log($"Created elliptical plane '{name}' at ({x}, {y}, {z}) - Radii: {radiusX}x{radiusZ}, Segments: {radialSegments}x{ringSegments}");
            return $"Successfully created elliptical plane '{name}' with {radialSegments * ringSegments + 1} vertices";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create elliptical plane: {e.Message}");
            throw;
        }
    }

    private Mesh GenerateEllipticalPlaneMesh(float radiusX, float radiusZ, int radialSegments, int ringSegments, float heightVariation)
    {
        Mesh mesh = new Mesh();
        mesh.name = "EllipticalPlaneMesh";

        // Calculate vertices count (center + rings)
        int vertexCount = 1 + radialSegments * ringSegments;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        // Center vertex
        float centerHeight = heightVariation > 0 ? Mathf.PerlinNoise(0.5f, 0.5f) * heightVariation : 0f;
        vertices[0] = new Vector3(0, centerHeight, 0);
        uvs[0] = new Vector2(0.5f, 0.5f);

        // Generate ring vertices
        int vertexIndex = 1;
        for (int ring = 1; ring <= ringSegments; ring++)
        {
            float ringRadius = (float)ring / ringSegments;
            
            for (int segment = 0; segment < radialSegments; segment++)
            {
                float angle = (float)segment / radialSegments * Mathf.PI * 2f;
                
                // Elliptical coordinates
                float x = radiusX * ringRadius * Mathf.Cos(angle);
                float z = radiusZ * ringRadius * Mathf.Sin(angle);
                
                // Height variation
                float height = 0f;
                if (heightVariation > 0f)
                {
                    height = Mathf.PerlinNoise(x * 0.1f + 50f, z * 0.1f + 50f) * heightVariation;
                }
                
                vertices[vertexIndex] = new Vector3(x, height, z);
                
                // UV mapping (normalized to 0-1)
                float u = (x / radiusX + 1f) * 0.5f;
                float v = (z / radiusZ + 1f) * 0.5f;
                uvs[vertexIndex] = new Vector2(u, v);
                
                vertexIndex++;
            }
        }

        // Generate triangles
        int triangleCount = radialSegments + (ringSegments - 1) * radialSegments * 2;
        int[] triangles = new int[triangleCount * 3];
        int triIndex = 0;

        // Center to first ring triangles
        for (int segment = 0; segment < radialSegments; segment++)
        {
            int next = (segment + 1) % radialSegments;
            
            triangles[triIndex++] = 0; // center
            triangles[triIndex++] = 1 + segment; // current
            triangles[triIndex++] = 1 + next; // next
        }

        // Ring to ring triangles
        for (int ring = 0; ring < ringSegments - 1; ring++)
        {
            int currentRingStart = 1 + ring * radialSegments;
            int nextRingStart = 1 + (ring + 1) * radialSegments;
            
            for (int segment = 0; segment < radialSegments; segment++)
            {
                int next = (segment + 1) % radialSegments;
                
                // First triangle
                triangles[triIndex++] = currentRingStart + segment;
                triangles[triIndex++] = nextRingStart + segment;
                triangles[triIndex++] = currentRingStart + next;
                
                // Second triangle
                triangles[triIndex++] = currentRingStart + next;
                triangles[triIndex++] = nextRingStart + segment;
                triangles[triIndex++] = nextRingStart + next;
            }
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        // Calculate proper normals and bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}