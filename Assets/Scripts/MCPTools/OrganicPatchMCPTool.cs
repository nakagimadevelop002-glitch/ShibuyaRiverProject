using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Create organic, irregular patch meshes for natural fur/algae shaders")]
public class OrganicPatchMCPTool
{
    [McpServerTool, Description("Create an organic patch plane with natural irregular variations")]
    public async ValueTask<string> CreateOrganicPatch(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Base width of patch")] float baseWidth = 2f,
        [Description("Base height of patch")] float baseHeight = 2f,
        [Description("Subdivision density (higher = more detail)")] int subdivisions = 32,
        [Description("Edge irregularity strength (0-1)")] float edgeIrregularity = 0.3f,
        [Description("Surface variation strength (0-1)")] float surfaceVariation = 0.1f,
        [Description("Noise scale for organic patterns")] float noiseScale = 1f,
        [Description("Random seed for reproducible patterns")] int seed = 0,
        [Description("Patch object name")] string name = "OrganicPatch")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create GameObject
            GameObject patchObj = new GameObject(name);
            patchObj.transform.position = new Vector3(x, y, z);

            // Add required components
            MeshFilter meshFilter = patchObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = patchObj.AddComponent<MeshRenderer>();

            // Generate organic patch mesh
            Mesh organicMesh = GenerateOrganicPatchMesh(
                baseWidth, baseHeight, subdivisions,
                edgeIrregularity, surfaceVariation, noiseScale, seed);
            meshFilter.mesh = organicMesh;

            // Set default material
            Material defaultMaterial = CreateAlgaeMaterial();
            meshRenderer.material = defaultMaterial;

            Debug.Log($"Created organic patch '{name}' at ({x}, {y}, {z}) - Size: {baseWidth}x{baseHeight}, Subdivisions: {subdivisions}");
            return $"Successfully created organic patch '{name}' with natural variations";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create organic patch: {e.Message}");
            throw;
        }
    }

    [McpServerTool, Description("Create multiple scattered organic patches for seabed coverage")]
    public async ValueTask<string> CreateOrganicPatchField(
        [Description("X position of field center")] float centerX = 0f,
        [Description("Y position of field center")] float centerY = 0f,
        [Description("Z position of field center")] float centerZ = 0f,
        [Description("Field width")] float fieldWidth = 10f,
        [Description("Field depth")] float fieldDepth = 10f,
        [Description("Number of patches")] int patchCount = 15,
        [Description("Min patch size")] float minSize = 0.5f,
        [Description("Max patch size")] float maxSize = 2f,
        [Description("Random seed")] int seed = 0,
        [Description("Field object name")] string name = "OrganicPatchField")
    {
        try
        {
            await UniTask.SwitchToMainThread();

            // Create parent GameObject
            GameObject fieldObj = new GameObject(name);
            fieldObj.transform.position = new Vector3(centerX, centerY, centerZ);

            UnityEngine.Random.InitState(seed);

            for (int i = 0; i < patchCount; i++)
            {
                // Random position within field
                float patchX = UnityEngine.Random.Range(-fieldWidth / 2f, fieldWidth / 2f);
                float patchZ = UnityEngine.Random.Range(-fieldDepth / 2f, fieldDepth / 2f);

                // Random size
                float patchSize = UnityEngine.Random.Range(minSize, maxSize);
                float aspectRatio = UnityEngine.Random.Range(0.7f, 1.3f);

                // Create individual patch
                GameObject patchObj = new GameObject($"Patch_{i:D2}");
                patchObj.transform.parent = fieldObj.transform;
                patchObj.transform.localPosition = new Vector3(patchX, 0, patchZ);
                patchObj.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

                // Add mesh components
                MeshFilter meshFilter = patchObj.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = patchObj.AddComponent<MeshRenderer>();

                // Generate unique organic mesh for each patch
                Mesh organicMesh = GenerateOrganicPatchMesh(
                    patchSize, patchSize * aspectRatio,
                    16, // Lower subdivision for field patches
                    UnityEngine.Random.Range(0.2f, 0.5f), // Random edge irregularity
                    UnityEngine.Random.Range(0.05f, 0.15f), // Random surface variation
                    UnityEngine.Random.Range(0.5f, 2f), // Random noise scale
                    seed + i); // Unique seed per patch

                meshFilter.mesh = organicMesh;
                meshRenderer.material = CreateAlgaeMaterial();
            }

            Debug.Log($"Created organic patch field '{name}' with {patchCount} patches");
            return $"Successfully created organic patch field '{name}' with {patchCount} scattered patches";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create organic patch field: {e.Message}");
            throw;
        }
    }

    private Mesh GenerateOrganicPatchMesh(float baseWidth, float baseHeight, int subdivisions,
        float edgeIrregularity, float surfaceVariation, float noiseScale, int seed)
    {
        Mesh mesh = new Mesh();
        mesh.name = "OrganicPatchMesh";

        // Initialize random with seed for reproducible results
        UnityEngine.Random.InitState(seed);

        int vertexCount = (subdivisions + 1) * (subdivisions + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        // Generate vertices with organic variations
        for (int y = 0; y <= subdivisions; y++)
        {
            for (int x = 0; x <= subdivisions; x++)
            {
                int index = y * (subdivisions + 1) + x;

                // Base normalized coordinates (0-1)
                float normalizedX = (float)x / subdivisions;
                float normalizedY = (float)y / subdivisions;

                // Center coordinates (-0.5 to 0.5)
                float centeredX = normalizedX - 0.5f;
                float centeredY = normalizedY - 0.5f;

                // Distance from center for edge effects
                float distanceFromCenter = Mathf.Sqrt(centeredX * centeredX + centeredY * centeredY);
                float maxDistance = 0.707f; // sqrt(0.5^2 + 0.5^2)
                float edgeFactor = 1f - Mathf.Clamp01(distanceFromCenter / maxDistance);

                // Apply organic edge deformation
                float edgeDeformation = 1f;
                if (distanceFromCenter > 0.3f) // Only affect outer areas
                {
                    float noiseValue = Mathf.PerlinNoise(
                        centeredX * noiseScale * 3f + seed,
                        centeredY * noiseScale * 3f + seed);
                    edgeDeformation = Mathf.Lerp(0.4f, 1f, noiseValue);
                    edgeDeformation = Mathf.Lerp(1f, edgeDeformation, edgeIrregularity * (1f - edgeFactor));
                }

                // Calculate world position with organic scaling
                float worldX = centeredX * baseWidth * edgeDeformation;
                float worldZ = centeredY * baseHeight * edgeDeformation;

                // Add surface height variation using multiple noise octaves
                float heightVariation = 0f;

                // Large scale variation
                heightVariation += Mathf.PerlinNoise(
                    normalizedX * noiseScale + seed,
                    normalizedY * noiseScale + seed) * 0.6f;

                // Medium scale detail
                heightVariation += Mathf.PerlinNoise(
                    normalizedX * noiseScale * 2f + seed + 100,
                    normalizedY * noiseScale * 2f + seed + 100) * 0.3f;

                // Fine scale detail
                heightVariation += Mathf.PerlinNoise(
                    normalizedX * noiseScale * 4f + seed + 200,
                    normalizedY * noiseScale * 4f + seed + 200) * 0.1f;

                // Normalize height variation
                heightVariation = (heightVariation - 0.5f) * surfaceVariation;

                // Apply edge fade to height variation
                heightVariation *= edgeFactor;

                vertices[index] = new Vector3(worldX, heightVariation, worldZ);
                uvs[index] = new Vector2(normalizedX, normalizedY);
            }
        }

        // Generate triangles
        int triangleCount = subdivisions * subdivisions * 6;
        int[] triangles = new int[triangleCount];
        int triIndex = 0;

        for (int y = 0; y < subdivisions; y++)
        {
            for (int x = 0; x < subdivisions; x++)
            {
                int bottomLeft = y * (subdivisions + 1) + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + (subdivisions + 1);
                int topRight = topLeft + 1;

                // First triangle (bottom-left, top-left, bottom-right)
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;

                // Second triangle (bottom-right, top-left, top-right)
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private Material CreateAlgaeMaterial()
    {
        // URP Compatible shader with algae-friendly settings
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (material.shader == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }
        if (material.shader == null)
        {
            material = new Material(Shader.Find("Sprites/Default"));
        }

        // Set algae-like color (dark green-brown)
        material.color = new Color(0.2f, 0.4f, 0.3f, 1f);

        // Make it slightly transparent if possible
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1); // Transparent
            material.SetFloat("_Blend", 0); // Alpha blend
        }

        // Reduce smoothness for organic look
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.1f);
        }

        return material;
    }
}