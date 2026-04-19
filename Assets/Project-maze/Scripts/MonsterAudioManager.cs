using UnityEngine;

public class MonsterSoundManager : MonoBehaviour
{
    [Header("Настройки проигрывателя")]
    public AudioSource source; // Сюда перетащи сам компонент AudioSource (или он подцепится сам)

    [Header("Звуковые файлы")]
    public AudioClip loopClip;       // Сюда тащи файл постоянного звука
    public AudioClip[] randomClips; // Сюда тащи файлы случайных рыков

    public float minDelay = 3f;
    public float maxDelay = 7f;

    void Start()
    {
        if (source == null) source = GetComponent<AudioSource>();

        // Настраиваем и запускаем фоновый звук
        if (loopClip != null)
        {
            source.clip = loopClip;
            source.loop = true;
            source.spatialBlend = 1f; // Делаем звук 3D
            source.Play();
        }

        StartCoroutine(PlayRandomRoutine());
    }

    System.Collections.IEnumerator PlayRandomRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            
            if (randomClips.Length > 0)
            {
                // Играем случайный звук поверх основного
                AudioClip clip = randomClips[Random.Range(0, randomClips.Length)];
                source.PlayOneShot(clip);
            }
        }
    }
}