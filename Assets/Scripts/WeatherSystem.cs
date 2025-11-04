using UnityEngine;
using System.Collections.Generic;
using KWS;

public class WeatherSystem : MonoBehaviour
{
    [Header("Weather Presets")]
    [Tooltip("天候のプリセットリスト")]
    public List<WeatherData> weatherPresets = new List<WeatherData>();

    [Header("Current Weather")]
    [Tooltip("現在の天候 (インデックス)")]
    public int currentWeatherIndex = 0;

    [Header("Auto Change Settings")]
    [Tooltip("自動で天候を変更する")]
    public bool autoChangeWeather = true;

    [Tooltip("天候変更の間隔（秒）")]
    [Range(10f, 300f)]
    public float weatherChangeInterval = 60f;

    [Tooltip("ランダムに天候を選ぶ")]
    public bool randomWeather = true;

    [Header("Transition Settings")]
    [Tooltip("天候切り替え時に補間する")]
    public bool smoothTransition = true;

    [Tooltip("補間にかける時間（秒）")]
    [Range(1f, 30f)]
    public float transitionDuration = 5f;

    [Tooltip("FlowSpeed専用の補間時間（秒）- より長くして滑らかに")]
    [Range(5f, 120f)]
    public float flowSpeedTransitionDuration = 30f;

    [Tooltip("FlowSpeedにイージング関数を使用")]
    public bool useFlowSpeedEasing = true;

    [Header("References")]
    [SerializeField] private RiverController riverController;
    [SerializeField] private WaterSystem waterSystem;
    [SerializeField] private WeatherAmbience weatherAmbience;

    // 内部状態
    private float timer = 0f;
    private WeatherData currentWeather;
    private WeatherData targetWeather;
    private float transitionTimer = 0f;
    private bool isTransitioning = false;

    // FlowSpeed専用の補間状態
    private float flowSpeedTransitionTimer = 0f;
    private bool isFlowSpeedTransitioning = false;
    private float flowSpeedFrom = 0f;
    private float flowSpeedTo = 0f;

    // WaterSourceFlowRate専用の補間状態
    private float waterSourceFlowRateFrom = 0f;
    private float waterSourceFlowRateTo = 0f;

    // WaterDrainRate専用の補間状態
    private float waterDrainRateFrom = 0f;
    private float waterDrainRateTo = 0f;


    public WeatherData CurrentWeather => currentWeather;

    private void Start()
    {
        // 参照の自動検索
        if (riverController == null)
        {
            riverController = FindObjectOfType<RiverController>();
        }

        if (waterSystem == null)
        {
            waterSystem = FindObjectOfType<WaterSystem>();
        }

        if (weatherAmbience == null)
        {
            weatherAmbience = FindObjectOfType<WeatherAmbience>();
        }

        // 初期天候を設定
        if (weatherPresets.Count > 0)
        {
            currentWeatherIndex = Mathf.Clamp(currentWeatherIndex, 0, weatherPresets.Count - 1);
            currentWeather = weatherPresets[currentWeatherIndex];
            targetWeather = currentWeather;

            // 初回は即座に適用（補間なし）
            ApplyWeather(currentWeather);

            // 環境音も初回適用
            if (weatherAmbience != null)
            {
                weatherAmbience.ApplyWeatherAudioImmediate(currentWeather);
            }
        }
        else
        {
            Debug.LogWarning("WeatherSystem: No weather presets assigned!");
        }
    }

    private void Update()
    {
        // 通常パラメータの補間処理
        if (isTransitioning && smoothTransition)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);

            // 補間してパラメータを適用（FlowSpeed以外）
            ApplyWeatherLerp(currentWeather, targetWeather, t);

            if (t >= 1f)
            {
                currentWeather = targetWeather;
                isTransitioning = false;
            }
        }

        // FlowSpeed専用の補間処理
        if (isFlowSpeedTransitioning && smoothTransition)
        {
            flowSpeedTransitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(flowSpeedTransitionTimer / flowSpeedTransitionDuration);

            // イージング適用
            float easedT = useFlowSpeedEasing ? EaseInOutQuad(t) : t;

            // FlowSpeedを補間
            float currentFlowSpeed = Mathf.Lerp(flowSpeedFrom, flowSpeedTo, easedT);
            if (riverController != null)
            {
                riverController.SetFlowSpeed(currentFlowSpeed);
            }

            if (t >= 1f)
            {
                isFlowSpeedTransitioning = false;
            }
        }

        // 自動天候変更
        if (autoChangeWeather && weatherPresets.Count > 1)
        {
            timer += Time.deltaTime;

            if (timer >= weatherChangeInterval)
            {
                timer = 0f;
                ChangeWeather();
            }
        }
    }

    /// <summary>
    /// 天候を変更
    /// </summary>
    public void ChangeWeather()
    {
        if (weatherPresets.Count == 0) return;

        int nextIndex;

        if (randomWeather)
        {
            // ランダム（現在と違う天候を選ぶ）
            do
            {
                nextIndex = Random.Range(0, weatherPresets.Count);
            } while (nextIndex == currentWeatherIndex && weatherPresets.Count > 1);
        }
        else
        {
            // 順番に次へ
            nextIndex = (currentWeatherIndex + 1) % weatherPresets.Count;
        }

        SetWeather(nextIndex);
    }

    /// <summary>
    /// 指定インデックスの天候に変更
    /// </summary>
    public void SetWeather(int index)
    {
        if (index < 0 || index >= weatherPresets.Count) return;

        currentWeatherIndex = index;
        targetWeather = weatherPresets[index];

        if (smoothTransition)
        {
            // 通常パラメータの補間開始
            bool wasTransitioning = isTransitioning;
            isTransitioning = true;
            transitionTimer = 0f;

            // FlowSpeed専用の補間開始
            // RiverControllerから現在実行中の実際の値を取得
            float currentFlowSpeedValue = riverController != null ? riverController.CurrentFlowSpeed : 0f;

            if (isFlowSpeedTransitioning && flowSpeedTransitionTimer > 0f)
            {
                // 補間途中の現在値を計算
                float t = Mathf.Clamp01(flowSpeedTransitionTimer / flowSpeedTransitionDuration);
                float easedT = useFlowSpeedEasing ? EaseInOutQuad(t) : t;
                currentFlowSpeedValue = Mathf.Lerp(flowSpeedFrom, flowSpeedTo, easedT);
            }

            isFlowSpeedTransitioning = true;
            flowSpeedTransitionTimer = 0f;
            flowSpeedFrom = currentFlowSpeedValue;
            flowSpeedTo = targetWeather.flowSpeedMultiplier;

            // WaterSourceFlowRate補間値の設定
            // RiverControllerから現在実行中の実際の値を取得
            float currentWaterSourceValue = riverController != null ? riverController.CurrentWaterSourceFlowRate : 0f;

            if (wasTransitioning && transitionTimer > 0f)
            {
                float t = Mathf.Clamp01(transitionTimer / transitionDuration);
                currentWaterSourceValue = Mathf.Lerp(waterSourceFlowRateFrom, waterSourceFlowRateTo, t);
            }

            waterSourceFlowRateFrom = currentWaterSourceValue;
            waterSourceFlowRateTo = targetWeather.waterSourceFlowRate;

            // WaterDrainRate補間値の設定
            // RiverControllerから現在実行中の実際の値を取得
            float currentWaterDrainValue = riverController != null ? riverController.CurrentWaterDrainRate : 0f;

            if (wasTransitioning && transitionTimer > 0f)
            {
                float t = Mathf.Clamp01(transitionTimer / transitionDuration);
                currentWaterDrainValue = Mathf.Lerp(waterDrainRateFrom, waterDrainRateTo, t);
            }

            waterDrainRateFrom = currentWaterDrainValue;
            waterDrainRateTo = targetWeather.waterDrainRate;

            // WavesTimeScaleは常に1.0固定
            if (waterSystem != null)
            {
                waterSystem.WavesTimeScale = 1f;
            }
        }
        else
        {
            currentWeather = targetWeather;
            ApplyWeather(currentWeather);
        }

        // 環境音を適用
        if (weatherAmbience != null)
        {
            if (smoothTransition)
            {
                weatherAmbience.ApplyWeatherAudio(targetWeather);
            }
            else
            {
                weatherAmbience.ApplyWeatherAudioImmediate(targetWeather);
            }
        }
    }

    /// <summary>
    /// 天候パラメータを即座に適用
    /// </summary>
    private void ApplyWeather(WeatherData weather)
    {
        if (riverController != null)
        {
            riverController.SetFlowSpeed(weather.flowSpeedMultiplier);
            riverController.SetWaterSourceFlowRate(weather.waterSourceFlowRate);
            riverController.SetWaterDrainRate(weather.waterDrainRate);
        }

        if (waterSystem != null)
        {
            waterSystem.WindSpeed = weather.windSpeed;
            waterSystem.WindRotation = weather.windRotation;
            waterSystem.WavesTimeScale = 1f; // 常に1.0固定
        }
    }

    /// <summary>
    /// 2つの天候パラメータを補間して適用（FlowSpeed以外）
    /// </summary>
    private void ApplyWeatherLerp(WeatherData from, WeatherData to, float t)
    {
        if (riverController != null)
        {
            // FlowSpeedは専用の補間処理で行うのでここではスキップ
            // WaterSourceFlowRateとWaterDrainRateは保存された補間値を使用
            riverController.SetWaterSourceFlowRate(Mathf.Lerp(waterSourceFlowRateFrom, waterSourceFlowRateTo, t));
            riverController.SetWaterDrainRate(Mathf.Lerp(waterDrainRateFrom, waterDrainRateTo, t));
        }

        if (waterSystem != null)
        {
            waterSystem.WindSpeed = Mathf.Lerp(from.windSpeed, to.windSpeed, t);
            waterSystem.WindRotation = Mathf.Lerp(from.windRotation, to.windRotation, t);
            waterSystem.WavesTimeScale = 1f; // 常に1.0固定
        }
    }

    /// <summary>
    /// EaseInOutQuad イージング関数
    /// </summary>
    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    /// <summary>
    /// Inspector用：天候を即座に変更
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && weatherPresets.Count > 0)
        {
            currentWeatherIndex = Mathf.Clamp(currentWeatherIndex, 0, weatherPresets.Count - 1);
            SetWeather(currentWeatherIndex);
        }
    }
}
