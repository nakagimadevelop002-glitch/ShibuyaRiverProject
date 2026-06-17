using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using ModelContextProtocol.Server;
using UnityEngine;

[McpServerToolType, Description("Enumerate available WebCam (camera) devices as Unity sees them")]
public class WebCamDeviceMCPTool
{
    [McpServerTool, Description("List all WebCamTexture devices with their index and name (the index used by CameraInput.cameraIndex)")]
    public async ValueTask<string> ListWebCamDevices()
    {
        try
        {
            await UniTask.SwitchToMainThread();

            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                return "No WebCam devices found.";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Found {devices.Length} WebCam device(s):");
            for (int i = 0; i < devices.Length; i++)
            {
                sb.AppendLine($"  [{i}] name=\"{devices[i].name}\" isFrontFacing={devices[i].isFrontFacing} kind={devices[i].kind}");
            }
            return sb.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to list webcam devices: {e.Message}");
            throw;
        }
    }
}
