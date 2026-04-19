using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    private Light2D light2D;

    [Header("Настройки яркости")]
    public float baseIntensity = 1.0f;     // Базовая яркость
    public float flickerAmount = 0.15f;    // Насколько сильно «дрожит» свет
    public float speed = 5f;               // Скорость дрожания

    [Header("Настройки резких миганий")]
    [Range(0, 1)]
    public float glitchChance = 0.05f;     // Шанс резкого затухания (от 0 до 1)
    public float glitchIntensity = 0.2f;   // Яркость при «глюке»

    private float targetIntensity;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        targetIntensity = baseIntensity;
    }

    void Update()
    {
        if (light2D == null) return;

        // 1. Создаем эффект мягкого «дрожания» через шум Перлина
        // Это дает более естественный свет, чем обычный Random.Range
        float noise = Mathf.PerlinNoise(Time.time * speed, 0);
        float currentIntensity = baseIntensity + (noise - 0.5f) * flickerAmount;

        // 2. Рандомные резкие просадки яркости (глюки фонарика)
        if (Random.value < glitchChance)
        {
            currentIntensity = glitchIntensity;
        }

        // 3. Плавно применяем яркость, чтобы не было слишком «больно» глазам
        light2D.intensity = Mathf.Lerp(light2D.intensity, currentIntensity, Time.deltaTime * 20f);
    }
}