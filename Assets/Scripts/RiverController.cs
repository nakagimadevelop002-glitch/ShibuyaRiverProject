using UnityEngine;
using KWS;

public class RiverController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KWS_DynamicWavesSimulationZone simulationZone;
    [SerializeField] private KWS_DynamicWavesObject waterSource;
    [SerializeField] private KWS_DynamicWavesObject waterDrain;

    [Header("Flow Speed Control")]
    [Range(0f, 5f)]
    [SerializeField] private float flowSpeed = 0.5f;
    private float previousFlowSpeed;

    [Header("Water Level Control")]
    [Range(0f, 2f)]
    [SerializeField] private float waterSourceFlowRate = 0.3f;
    private float previousSourceFlowRate;

    [Range(0f, 2f)]
    [SerializeField] private float waterDrainRate = 0.88f;
    private float previousDrainRate;

    // 現在値を取得するゲッター
    public float CurrentFlowSpeed => flowSpeed;
    public float CurrentWaterSourceFlowRate => waterSourceFlowRate;
    public float CurrentWaterDrainRate => waterDrainRate;

    private void Start()
    {
        // 初期値を保存
        previousFlowSpeed = flowSpeed;
        previousSourceFlowRate = waterSourceFlowRate;
        previousDrainRate = waterDrainRate;

        // 参照が設定されていない場合は自動検索
        if (simulationZone == null)
        {
            simulationZone = FindObjectOfType<KWS_DynamicWavesSimulationZone>();
        }

        if (waterSource == null || waterDrain == null)
        {
            var dynamicWavesObjects = FindObjectsOfType<KWS_DynamicWavesObject>();
            foreach (var obj in dynamicWavesObjects)
            {
                if (obj.InteractionType == KWS_DynamicWavesObject.InteractionTypeEnum.WaterSource && waterSource == null)
                {
                    waterSource = obj;
                }
                else if (obj.InteractionType == KWS_DynamicWavesObject.InteractionTypeEnum.WaterDrain && waterDrain == null)
                {
                    waterDrain = obj;
                }
            }
        }
    }

    private void Update()
    {
        // Flow Speedの変更を検知
        if (Mathf.Abs(flowSpeed - previousFlowSpeed) > 0.001f)
        {
            if (simulationZone != null)
            {
                simulationZone.FlowSpeedMultiplier = flowSpeed;
            }
            previousFlowSpeed = flowSpeed;
        }

        // WaterSource FlowRateの変更を検知
        if (Mathf.Abs(waterSourceFlowRate - previousSourceFlowRate) > 0.001f)
        {
            if (waterSource != null)
            {
                waterSource.ConstantFlowRate = waterSourceFlowRate;
            }
            previousSourceFlowRate = waterSourceFlowRate;
        }

        // WaterDrain DrainRateの変更を検知
        if (Mathf.Abs(waterDrainRate - previousDrainRate) > 0.001f)
        {
            if (waterDrain != null)
            {
                waterDrain.ConstantDrainRate = waterDrainRate;
            }
            previousDrainRate = waterDrainRate;
        }
    }

    // OnValidateは削除（WeatherSystemとの競合を防ぐため）
    // Inspector上での手動変更はUpdate()の変更検知で対応

    // WeatherSystemから呼ばれるパブリックメソッド
    public void SetFlowSpeed(float speed)
    {
        flowSpeed = speed;
        if (simulationZone != null)
        {
            simulationZone.FlowSpeedMultiplier = speed;
        }
        previousFlowSpeed = speed;
    }

    public void SetWaterSourceFlowRate(float rate)
    {
        waterSourceFlowRate = rate;
        if (waterSource != null)
        {
            waterSource.ConstantFlowRate = rate;
        }
        previousSourceFlowRate = rate;
    }

    public void SetWaterDrainRate(float rate)
    {
        waterDrainRate = rate;
        if (waterDrain != null)
        {
            waterDrain.ConstantDrainRate = rate;
        }
        previousDrainRate = rate;
    }
}
