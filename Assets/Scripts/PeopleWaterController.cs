using UnityEngine;
using KWS;

/// <summary>
/// 人物検知結果を水面パラメータに反映するコンポーネント
/// PersonDetectorから人数を取得し、WeatherSystemまたはWaterSystemを制御
/// </summary>
public class PeopleWaterController : MonoBehaviour
{
    [Header("イベント発火設定")]
    [Tooltip("人数がこの値以上になったら一度だけイベント発火")]
    public int fishEscapeThreshold = 3;

    [Tooltip("人数が閾値以上になった時に一度だけ発火するGameEvent")]
    public GameEvent onPeopleExceedThreshold;

    [Tooltip("人数が0人に戻った時に一度だけ発火するGameEvent")]
    public GameEvent onPeopleReturnToZero;

    bool hasExceededThreshold = false;
    bool hasReturnedToZero = true;
    [Header("人物検知参照")]
    [Tooltip("PersonDetectorコンポーネント")]
    public PersonDetector personDetector;

    [Header("天候システム参照（任意）")]
    [Tooltip("WeatherSystemコンポーネント（任意）")]
    public WeatherSystem weatherSystem;

    [Header("水システム参照（任意）")]
    [Tooltip("WaterSystemコンポーネント（任意）")]
    public WaterSystem waterSystem;

    [Header("川制御参照（任意）")]
    [Tooltip("RiverControllerコンポーネント（任意）")]
    public RiverController riverController;

    [Header("人数の上限値")]
    [Tooltip("人数の上限値（これ以上は変化しない）")]
    public int maxPeopleCount = 10;

    [Header("最小水面パラメータ倍率")]
    [Tooltip("人数0人の時の水面パラメータ倍率")]
    [Range(0f, 5f)]
    public float minWaterIntensity = 0.5f;

    [Header("最大水面パラメータ倍率")]
    [Tooltip("人数10人以上の時の水面パラメータ倍率")]
    [Range(0f, 5f)]
    public float maxWaterIntensity = 3.0f;

    [Header("風速を制御")]
    [Tooltip("WindSpeedを制御する")]
    public bool controlWindSpeed = false;

    [Header("流速を制御")]
    [Tooltip("FlowSpeedを制御する")]
    public bool controlFlowSpeed = false;

    [Header("水源流量を制御")]
    [Tooltip("WaterSourceFlowRateを制御する")]
    public bool controlWaterSource = false;

    [Header("透明度を制御")]
    [Tooltip("水の透明度を制御する")]
    public bool controlTransparency = true;

    [Header("濁り色を制御")]
    [Tooltip("水の濁り色を制御する")]
    public bool controlTurbidity = true;

    [Header("屈折モード有効化")]
    [Tooltip("屈折モードを有効化する（透明感に必須）")]
    public bool enableRefraction = true;

    [Header("最小透明度")]
    [Tooltip("人数0人の時の透明度（大きいほど透明）")]
    [Range(1f, 100f)]
    public float minTransparency = 20f;

    [Header("最大透明度")]
    [Tooltip("人数10人以上の時の透明度（小さいほど濁る）")]
    [Range(1f, 100f)]
    public float maxTransparency = 5f;

    [Header("最小濁り色")]
    [Tooltip("人数0人の時の濁り色（透明な水の色）")]
    public Color minTurbidityColor = new Color(7 / 255.0f, 65 / 255.0f, 80 / 255.0f, 1f); // 薄い青

    [Header("最大濁り色")]
    [Tooltip("人数10人以上の時の濁り色（現在の茶色）")]
    public Color maxTurbidityColor = new Color(7 / 255.0f, 65 / 255.0f, 80 / 255.0f, 1f); // 茶色

    [Header("スムージング有効化")]
    [Tooltip("パラメータ変化を滑らかにする")]
    public bool enableSmoothing = true;

    [Header("変化にかける時間（秒）")]
    [Tooltip("目標値まで変化するのにかける時間（秒）")]
    [Range(0.1f, 10f)]
    public float transitionDuration = 3f;

    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    private float transitionStartTime = 0f;
    private float transitionStartIntensity = 0f;
    private int lastPeopleCount = -1;

    void Start()
    {
        // 自動検索
        if (personDetector == null)
        {
            personDetector = FindObjectOfType<PersonDetector>();
        }

        if (weatherSystem == null)
        {
            weatherSystem = FindObjectOfType<WeatherSystem>();
        }

        if (waterSystem == null)
        {
            waterSystem = FindObjectOfType<WaterSystem>();
        }

        if (riverController == null)
        {
            riverController = FindObjectOfType<RiverController>();
        }

        // 屈折モードを有効化（透明感に必須）
        if (enableRefraction && waterSystem != null)
        {
            waterSystem.RefractionMode = KWS.WaterQualityLevelSettings.RefractionModeEnum.PhysicalAproximationIOR;
            Debug.Log("PeopleWaterController: 屈折モードを有効化しました（透明感向上）");
        }

        // 初期値設定
        currentIntensity = minWaterIntensity;
        targetIntensity = minWaterIntensity;
    }

    void Update()
    {
        if (personDetector == null)
            return;

        int peopleCount = personDetector.detectedPeopleCount;

        // 人数が変化した場合のみ更新
        if (peopleCount != lastPeopleCount)
        {
            lastPeopleCount = peopleCount;

            // イベント発火チェック
            CheckAndFireEvents(peopleCount);

            // 人数を0-1の範囲に正規化
            float normalizedCount = Mathf.Clamp01((float)peopleCount / maxPeopleCount);

            // 線形補間で目標強度を計算
            targetIntensity = Mathf.Lerp(minWaterIntensity, maxWaterIntensity, normalizedCount);

            // トランジション開始
            transitionStartTime = Time.time;
            transitionStartIntensity = currentIntensity;

            Debug.Log($"PeopleWaterController: 人数 {peopleCount} → 目標強度 {targetIntensity:F2} (現在 {currentIntensity:F2})");
        }

        // スムージング処理（時間ベース）
        if (enableSmoothing)
        {
            float elapsedTime = Time.time - transitionStartTime;
            float t = Mathf.Clamp01(elapsedTime / transitionDuration);
            currentIntensity = Mathf.Lerp(transitionStartIntensity, targetIntensity, t);
        }
        else
        {
            currentIntensity = targetIntensity;
        }

        // 水面パラメータに反映
        ApplyWaterParameters();
    }

    void ApplyWaterParameters()
    {
        if (waterSystem == null) return;

        // currentIntensityを0-1の範囲に正規化（平滑化済みの値を使用）
        float normalizedIntensity = Mathf.InverseLerp(minWaterIntensity, maxWaterIntensity, currentIntensity);

        // WindSpeedの制御
        if (controlWindSpeed)
        {
            waterSystem.WindSpeed = currentIntensity * 5f; // 0-25の範囲
        }

        // FlowSpeedの制御
        if (controlFlowSpeed && riverController != null)
        {
            riverController.SetFlowSpeed(currentIntensity);
        }

        // WaterSourceFlowRateの制御
        if (controlWaterSource && riverController != null)
        {
            riverController.SetWaterSourceFlowRate(currentIntensity * 0.5f);
        }

        // 透明度の制御（人が少ない→透明、人が多い→濁る）
        if (controlTransparency)
        {
            waterSystem.Transparent = Mathf.Lerp(minTransparency, maxTransparency, normalizedIntensity);
        }

        // 濁り色の制御（人が少ない→薄い青、人が多い→茶色）
        if (controlTurbidity)
        {
            Color turbidityColor = Color.Lerp(minTurbidityColor, maxTurbidityColor, normalizedIntensity);
            waterSystem.TurbidityColor = turbidityColor;
            waterSystem.CustomSkyColor = turbidityColor; // 水面の色も同期
        }
    }

    /// <summary>
    /// 現在の強度を取得
    /// </summary>
    public float GetCurrentIntensity()
    {
        return currentIntensity;
    }

    /// <summary>
    /// 検出人数を取得
    /// </summary>
    public int GetPeopleCount()
    {
        return lastPeopleCount;
    }

    /// <summary>
    /// イベント発火チェック（閾値以上で一度だけ、0人に戻ったら一度だけ）
    /// </summary>
    void CheckAndFireEvents(int peopleCount)
    {
        // 閾値以上になった時の処理（一度だけ）
        if (peopleCount >= fishEscapeThreshold && !hasExceededThreshold)
        {
            hasExceededThreshold = true;
            hasReturnedToZero = false;
            if (onPeopleExceedThreshold != null)
            {
                onPeopleExceedThreshold.Raise();
            }
            Debug.Log($"PeopleWaterController: 人数が{fishEscapeThreshold}人以上になりました - GameEvent発火");
        }

        // 0人に戻った時の処理（一度だけ）
        if (peopleCount == 0 && !hasReturnedToZero && hasExceededThreshold)
        {
            hasReturnedToZero = true;
            hasExceededThreshold = false;
            if (onPeopleReturnToZero != null)
            {
                onPeopleReturnToZero.Raise();
            }
            Debug.Log("PeopleWaterController: 人数が0人に戻りました - GameEvent発火");
        }
    }
}
