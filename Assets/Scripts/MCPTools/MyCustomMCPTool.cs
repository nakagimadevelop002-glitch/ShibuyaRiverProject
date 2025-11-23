using System.ComponentModel;
using System;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("カスタムMCPツールの説明")]
public class MyCustomMCPTool
{
    [McpServerTool, Description("メソッドの説明")]
    public string MyMethod()
    {
        Debug.Log("Hello from Unity!");
        return "Hello from Unity!";
    }
}