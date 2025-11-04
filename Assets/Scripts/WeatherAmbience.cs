using UnityEngine;

/// <summary>
/// 天候に応じた環境音を管理・再生するコンポーネント
/// WeatherSystemから天候データを受け取り、川の音と雨の音をクロスフェードで切り替える
/// </summary>
public class WeatherAmbience : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("川の環境音を再生するAudioSource")]
    [SerializeField] private AudioSource riverAudioSource;

    [Tooltip("雨の環境音を再生するAudioSource")]
    [SerializeField] private AudioSource rainAudioSource;

    [Header("Volume Settings")]
    [Tooltip("川の音の最大音量")]
    [Range(0f, 1f)]
    [SerializeField] private float riverMaxVolume = 0.7f;

    [Tooltip("雨の音の最大音量")]
    [Range(0f, 1f)]
    [SerializeField] private float rainMaxVolume = 0.7f;

    [Header("Transition Settings")]
    [Tooltip("音のクロスフェード時間（秒）")]
    [Range(0.5f, 10f)]
    [SerializeField] private float crossfadeDuration = 3f;

    // 内部状態
    private AudioClip targetRiverClip;
    private AudioClip targetRainClip;
    private float targetRiverVolume;
    private float targetRainVolume;
    private float transitionTimer;
    private bool isTransitioning;

    private void Awake()
    {
        // AudioSourceの初期化
        if (riverAudioSource == null)
        {
            riverAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (rainAudioSource == null)
        {
            rainAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // ループ設定を強制適用（手動アタッチ済みのAudioSourceにも適用）
        riverAudioSource.loop = true;
        riverAudioSource.playOnAwake = false;

        rainAudioSource.loop = true;
        rainAudioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / crossfadeDuration);

            // 音量をクロスフェード
            riverAudioSource.volume = Mathf.Lerp(riverAudioSource.volume, targetRiverVolume, t);
            rainAudioSource.volume = Mathf.Lerp(rainAudioSource.volume, targetRainVolume, t);

            if (t >= 1f)
            {
                isTransitioning = false;

                // 音量が0になったAudioSourceは停止
                if (riverAudioSource.volume <= 0.01f && riverAudioSource.isPlaying)
                {
                    riverAudioSource.Stop();
                }

                if (rainAudioSource.volume <= 0.01f && rainAudioSource.isPlaying)
                {
                    rainAudioSource.Stop();
                }
            }
        }
    }

    /// <summary>
    /// 天候データに基づいて環境音を設定・再生
    /// </summary>
    /// <param name="weatherData">適用する天候データ</param>
    public void ApplyWeatherAudio(WeatherData weatherData)
    {
        if (weatherData == null) return;

        // ターゲットクリップを設定
        targetRiverClip = weatherData.riverAmbientSound;
        targetRainClip = weatherData.rainAmbientSound;

        // ターゲット音量を決定
        targetRiverVolume = targetRiverClip != null ? riverMaxVolume : 0f;
        targetRainVolume = targetRainClip != null ? rainMaxVolume : 0f;

        // クリップが変わった場合は切り替え
        if (riverAudioSource.clip != targetRiverClip)
        {
            riverAudioSource.clip = targetRiverClip;
            if (targetRiverClip != null)
            {
                riverAudioSource.volume = 0f;
                riverAudioSource.Play();
            }
        }

        if (rainAudioSource.clip != targetRainClip)
        {
            rainAudioSource.clip = targetRainClip;
            if (targetRainClip != null)
            {
                rainAudioSource.volume = 0f;
                rainAudioSource.Play();
            }
        }

        // クロスフェード開始
        isTransitioning = true;
        transitionTimer = 0f;
    }

    /// <summary>
    /// 即座に環境音を適用（クロスフェードなし）
    /// </summary>
    /// <param name="weatherData">適用する天候データ</param>
    public void ApplyWeatherAudioImmediate(WeatherData weatherData)
    {
        if (weatherData == null) return;

        // 川の音
        if (weatherData.riverAmbientSound != null)
        {
            riverAudioSource.clip = weatherData.riverAmbientSound;
            riverAudioSource.volume = riverMaxVolume;
            riverAudioSource.Play();
        }
        else
        {
            riverAudioSource.Stop();
            riverAudioSource.volume = 0f;
        }

        // 雨の音
        if (weatherData.rainAmbientSound != null)
        {
            rainAudioSource.clip = weatherData.rainAmbientSound;
            rainAudioSource.volume = rainMaxVolume;
            rainAudioSource.Play();
        }
        else
        {
            rainAudioSource.Stop();
            rainAudioSource.volume = 0f;
        }

        isTransitioning = false;
    }
}
