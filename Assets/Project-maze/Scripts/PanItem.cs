using UnityEngine;
using UnityEngine.UI;

public class PanItem : MonoBehaviour
{
    [Header("Настройки парения")]
    public float speed = 2f;
    public float height = 0.2f; // Сделал чуть меньше, чтобы не улетала высоко

    [Header("Настройки")]
    public float detectionRadius = 7f;
    
    private Transform player; 
    private Image proximityIcon; 
    private AudioSource audioSource;
    private bool isPickedUp = false; 
    private Vector3 startPos; // ДОБАВЛЕНО: для хранения начальной точки

    void Start()
    {
        startPos = transform.position; // ЗАПИСЫВАЕМ начальную позицию
        audioSource = GetComponent<AudioSource>();

        // 1. Ищем игрока на сцене по ТЕГУ
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // 2. Ищем иконку в Canvas по ИМЕНИ
        GameObject iconObj = GameObject.Find("PanProximityIcon");
        if (iconObj != null)
        {
            proximityIcon = iconObj.GetComponent<Image>();
            Color c = proximityIcon.color;
            c.a = 0;
            proximityIcon.color = c;
        }
    }

    void Update()
    {
        if (isPickedUp) return;

        // Логика иконки близости
        if (player != null && proximityIcon != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            float alpha = 1 - Mathf.Clamp01(distance / detectionRadius);
            Color c = proximityIcon.color;
            c.a = alpha;
            proximityIcon.color = c;
        }

        // ПАРЕНИЕ вверх-вниз (теперь startPos существует)
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * height;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isPickedUp)
        {
            isPickedUp = true;

            // Сообщаем скрипту атаки
            PlayerAttack attackScript = other.GetComponent<PlayerAttack>();
            if (attackScript != null) attackScript.EquipWeapon("Pan");

            // ИГРАЕМ ЗВУК
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
            }
    // 2. Включаем сковородку в ИНВЕНТАРЕ (Inventory)
            Inventory inv = other.GetComponent<Inventory>();
            if (inv != null) inv.AddPan(); // <--- ДОБАВЬ ЭТУ СТРОКУ
            
            // СКРЫВАЕМ ВИЗУАЛ (чтобы объект жил, пока играет звук)
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;
            
            if (proximityIcon != null) 
                proximityIcon.gameObject.SetActive(false);

            // УДАЛЯЕМ через 1 секунду, чтобы звук доиграл
            Destroy(gameObject, 1.0f); 
        }
    }
}