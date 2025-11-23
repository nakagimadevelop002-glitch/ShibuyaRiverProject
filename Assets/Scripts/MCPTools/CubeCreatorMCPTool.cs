using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Create Cube objects in the scene")]
public class CubeCreatorMCPTool
{
    [McpServerTool, Description("Create a 3D Cube in the scene")]
    public async ValueTask<string> CreateCube(
        [Description("X position")] float x = 0f,
        [Description("Y position")] float y = 0f,
        [Description("Z position")] float z = 0f,
        [Description("Cube name")] string name = "Cube")
    {
        await UniTask.SwitchToMainThread();
        
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(x, y, z);
        cube.name = name;
        
        Debug.Log($"Created Cube '{name}' at ({x}, {y}, {z})");
        return $"Created Cube '{name}' at ({x}, {y}, {z})";
    }
}