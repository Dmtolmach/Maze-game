using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Настройки звука")]
    public AudioSource audioSource;
    public AudioClip[] footstepSounds; // Список ваших звуков шагов
    public float stepInterval = 0.4f;  // Как часто звучит шаг

    private float stepTimer;

    void Update()
    {
        // Считываем нажатия клавиш (W, A, S, D или стрелки)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Если игрок нажимает кнопки движения
        if (horizontal != 0 || vertical != 0)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0)
            {
                PlayFootstep(); // Вот эта команда, которая вызывала ошибку
                stepTimer = stepInterval;
            }
        }
        else
        {
            // Сбрасываем таймер, чтобы первый шаг звучал сразу при начале ходьбы
            stepTimer = 0;
        }
    }

    // ВАЖНО: Этот блок должен быть внутри класса, но вне метода Update
    void PlayFootstep()
    {
        // Проверяем, есть ли вообще звуки в списке и назначен ли AudioSource
        if (footstepSounds.Length == 0 || audioSource == null) return;

        // Выбираем случайный звук из списка
        int index = Random.Range(0, footstepSounds.Length);
        
        // Слегка меняем высоту звука (pitch), чтобы шаги не были скучными
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        
        // Воспроизводим звук
        audioSource.PlayOneShot(footstepSounds[index]);
    }
}