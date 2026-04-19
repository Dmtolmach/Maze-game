using UnityEngine;
using UnityEngine.UI;

public class KeyItem : MonoBehaviour
{
    [Header("Настройки парения")]
    public float speed = 2f;
    public float height = 0.5f;

    [Header("Индикатор близости")]
    public Transform player;
    public Image proximityIcon;
    public float detectionRadius = 5f;

    private Vector3 startPos;
    private AudioSource audioSource;
    private bool isPickedUp = false; 

    void Start()
    {
        startPos = transform.position;
        // Ищем компонент AudioSource на этом же объекте
        audioSource = GetComponent<AudioSource>();
        
        if (proximityIcon != null)
        {
            Color c = proximityIcon.color;
            c.a = 0;
            proximityIcon.color = c;
        }
    }

    // Метод для настройки из генератора
    public void Setup(Transform playerTransform, Image icon)
    {
        player = playerTransform;
        proximityIcon = icon;
    }

    void Update()
    {
        if (isPickedUp) return; // Если подобрали, больше ничего не делаем

        // Парение вверх-вниз
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * height;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Логика иконки близости
        if (player != null && proximityIcon != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            float alpha = 1 - Mathf.Clamp01(distance / detectionRadius);
            Color c = proximityIcon.color;
            c.a = alpha;
            proximityIcon.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Теперь переменные isPickedUp и audioSource существуют везде!
        if (other.CompareTag("Player") && !isPickedUp)
        {
            Inventory inv = other.GetComponent<Inventory>();
            if (inv != null)
            {
                isPickedUp = true;
                inv.hasKey = true;

                if (inv.keyInInventoryIcon != null)
                    inv.keyInInventoryIcon.SetActive(true);

                // Играем звук
                if (audioSource != null && audioSource.clip != null)
                {
                    audioSource.Play();
                }

                // Скрываем ключ, чтобы звук успел доиграть
                GetComponent<SpriteRenderer>().enabled = false;
                GetComponent<Collider2D>().enabled = false;
                if (proximityIcon != null) proximityIcon.gameObject.SetActive(false);

                // Удаляем объект после завершения звука
                Destroy(gameObject, 1.0f);
            }
        }
    }
}