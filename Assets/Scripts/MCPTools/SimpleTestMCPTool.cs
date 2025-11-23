using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Simple test tool for debugging")]
public class SimpleTestMCPTool
{
    [McpServerTool, Description("Create a simple test cube")]
    public async ValueTask<string> CreateTestCube(
        [Description("Test cube name")] string name = "TestCube")
    {
        await UniTask.SwitchToMainThread();

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = Vector3.zero;

        Debug.Log($"Created test cube: {name}");
        return $"Created test cube: {name}";
    }
}