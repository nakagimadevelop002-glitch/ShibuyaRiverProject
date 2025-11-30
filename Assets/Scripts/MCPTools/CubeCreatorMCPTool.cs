using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Create GameObject objects in the scene")]
public class GameObjectCreatorMCPTool
{
    [McpServerTool, Description("Create a GameObject in the scene (Empty, Cube, Sphere, Capsule, Cylinder, Plane, Quad)")]
    public async ValueTask<string> CreateGameObject(
        [Description("Object type: Empty, Cube, Sphere, Capsule, Cylinder, Plane, Quad")] string objectType = "Empty",
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Object name")] string name = "GameObject")
    {
        await UniTask.SwitchToMainThread();

        GameObject obj;

        switch (objectType.ToLower())
        {
            case "empty":
                obj = new GameObject();
                break;
            case "cube":
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                break;
            case "sphere":
                obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                break;
            case "capsule":
                obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                break;
            case "cylinder":
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                break;
            case "plane":
                obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                break;
            case "quad":
                obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                break;
            default:
                obj = new GameObject();
                Debug.LogWarning($"Unknown objectType '{objectType}', creating empty GameObject");
                break;
        }

        obj.transform.position = new Vector3(x, y, z);
        obj.name = name;

        Debug.Log($"Created {objectType} GameObject '{name}' at ({x}, {y}, {z})");
        return $"SUCCESS: Created {objectType} GameObject '{name}' at ({x}, {y}, {z})";
    }

    [McpServerTool, Description("Create a 3D Cube in the scene (backward compatibility)")]
    public async ValueTask<string> CreateCube(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Cube name")] string name = "Cube")
    {
        return await CreateGameObject("Cube", x, y, z, name);
    }
}