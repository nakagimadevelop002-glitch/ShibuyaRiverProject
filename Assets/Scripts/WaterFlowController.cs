using UnityEngine;
using KWS;

/// <summary>
/// 水の流量を制御するコントローラー
/// EventListenerから呼び出して、KWS_DynamicWavesObjectのConstantFlowRateを変更する
///
/// 使用方法:
/// 1. Dynamic Waves Objectにこのコンポーネントをアタッチ
/// 2. InspectorでtargetWaterObjectに自身（KWS_DynamicWavesObject）を参照設定
/// 3. EventListenerのUnityEventから以下のメソッドを呼び出し:
///    - SetFlowRateHigh(): フェード中などの高速流量（1.0）
///    - SetFlowRateLow(): 通常時の低速流量（0.008）
/// </summary>
public class WaterFlowController : MonoBehaviour
{
    [Header("Target Water Object")]
    [Tooltip("制御対象のKWS_DynamicWavesObject（通常は自身のGameObjectにアタッチされているもの）")]
    [SerializeField] private KWS_DynamicWavesObject targetWaterObject;

    [Header("Flow Rate Settings")]
    [Tooltip("高速流量の値（フェード中など）")]
    [SerializeField] private float highFlowRate = 1.0f;

    [Tooltip("低速流量の値（通常時）")]
    [SerializeField] private float lowFlowRate = 0.008f;

    /// <summary>
    /// 流量を高速（1.0）に設定
    /// EventListenerから呼び出す用のpublicメソッド
    /// </summary>
    public void SetFlowRateHigh()
    {
        if (targetWaterObject != null)
        {
            targetWaterObject.ConstantFlowRate = highFlowRate;
            Debug.Log($"[WaterFlowController] ConstantFlowRate set to {highFlowRate} (高速)");
        }
        else
        {
            Debug.LogError("[WaterFlowController] targetWaterObjectが設定されていません。Inspectorで設定してください。");
        }
    }

    /// <summary>
    /// 流量を低速（0.008）に設定
    /// EventListenerから呼び出す用のpublicメソッド
    /// </summary>
    public void SetFlowRateLow()
    {
        if (targetWaterObject != null)
        {
            targetWaterObject.ConstantFlowRate = lowFlowRate;
            Debug.Log($"[WaterFlowController] ConstantFlowRate set to {lowFlowRate} (低速)");
        }
        else
        {
            Debug.LogError("[WaterFlowController] targetWaterObjectが設定されていません。Inspectorで設定してください。");
        }
    }

    /// <summary>
    /// 任意の流量を設定（カスタム値が必要な場合）
    /// </summary>
    /// <param name="flowRate">設定する流量の値</param>
    public void SetFlowRateCustom(float flowRate)
    {
        if (targetWaterObject != null)
        {
            targetWaterObject.ConstantFlowRate = flowRate;
            Debug.Log($"[WaterFlowController] ConstantFlowRate set to {flowRate} (カスタム)");
        }
        else
        {
            Debug.LogError("[WaterFlowController] targetWaterObjectが設定されていません。Inspectorで設定してください。");
        }
    }
}
