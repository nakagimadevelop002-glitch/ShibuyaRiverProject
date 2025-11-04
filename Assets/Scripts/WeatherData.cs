using UnityEngine;

[CreateAssetMenu(fileName = "WeatherData", menuName = "River/Weather Data")]
public class WeatherData : ScriptableObject
{
    [Header("Weather Info")]
    public string weatherName = "Sunny";

    [Header("River Parameters")]
    [Range(0f, 5f)]
    [Tooltip("川の流速倍率")]
    public float flowSpeedMultiplier = 0.5f;

    [Range(0f, 2f)]
    [Tooltip("水源からの水供給量")]
    public float waterSourceFlowRate = 0.3f;

    [Range(0f, 2f)]
    [Tooltip("排水の排出量")]
    public float waterDrainRate = 0.88f;

    [Header("Wind Parameters (Optional)")]
    [Range(0f, 20f)]
    [Tooltip("風速 (WaterSystemのWindSpeedに反映)")]
    public float windSpeed = 5f;

    [Range(-180f, 180f)]
    [Tooltip("風向き")]
    public float windRotation = 0f;

    [Header("Wave Parameters (Optional)")]
    [Range(0f, 3f)]
    [Tooltip("波の時間スケール")]
    public float wavesTimeScale = 1f;

    [Header("Audio Parameters")]
    [Tooltip("川の環境音")]
    public AudioClip riverAmbientSound;

    [Tooltip("雨の環境音")]
    public AudioClip rainAmbientSound;
}
