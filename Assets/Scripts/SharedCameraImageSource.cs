using System.Collections;
using UnityEngine;
using Mediapipe.Unity;

/// <summary>
/// CameraInputからMediaPipeへTexture2Dを供給するアダプター
/// 責任: CameraInputとMediaPipeの間のブリッジ
/// </summary>
public class SharedCameraImageSource : ImageSource
{
    private CameraInput cameraInput;
    private bool _isPrepared = false;
    private bool _isPlaying = false;

    public SharedCameraImageSource(CameraInput cameraInput)
    {
        this.cameraInput = cameraInput;
    }

    public override string sourceName => cameraInput != null ? "SharedCamera" : "None";

    public override string[] sourceCandidateNames => new string[] { "SharedCamera" };

    public override ResolutionStruct[] availableResolutions
    {
        get
        {
            if (cameraInput != null && cameraInput.IsInitialized)
            {
                return new ResolutionStruct[]
                {
                    new ResolutionStruct(cameraInput.Width, cameraInput.Height, 30.0)
                };
            }
            return new ResolutionStruct[] { new ResolutionStruct(640, 480, 30.0) };
        }
    }

    public override bool isPrepared => _isPrepared && cameraInput != null && cameraInput.IsInitialized;

    public override bool isPlaying => _isPlaying;

    public override void SelectSource(int sourceId)
    {
        // SharedCameraは1つのみなので何もしない
    }

    public override IEnumerator Play()
    {
        if (cameraInput == null)
        {
            Debug.LogError("[SharedCameraImageSource] CameraInput is null!");
            yield break;
        }

        // CameraInputが初期化されるまで待機
        while (!cameraInput.IsInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 解像度設定
        resolution = new ResolutionStruct(cameraInput.Width, cameraInput.Height, 30.0);

        _isPrepared = true;
        _isPlaying = true;

        Debug.Log($"[SharedCameraImageSource] Prepared: {cameraInput.Width}x{cameraInput.Height}");
    }

    public override IEnumerator Resume()
    {
        if (!_isPrepared)
        {
            throw new System.InvalidOperationException("SharedCameraImageSource is not prepared");
        }

        _isPlaying = true;
        yield break;
    }

    public override void Pause()
    {
        _isPlaying = false;
    }

    public override void Stop()
    {
        _isPrepared = false;
        _isPlaying = false;
    }

    public override Texture GetCurrentTexture()
    {
        if (!isPrepared)
        {
            throw new System.InvalidOperationException("SharedCameraImageSource is not prepared");
        }

        return cameraInput.currentFrame;
    }
}
