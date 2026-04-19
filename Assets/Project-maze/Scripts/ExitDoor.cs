using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    [Header("Звуки")]
    public AudioSource audioSource;
    public AudioClip lockedSound;
    public AudioClip openSound;

    // Ссылка на коллайдер, чтобы его выключить при открытии
    private Collider2D doorCollider;

    void Start()
    {
        doorCollider = GetComponent<Collider2D>();
    }

    // ВАЖНО: Используем OnCollisionEnter2D (для твердых объектов)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, что это игрок
        if (collision.gameObject.CompareTag("Player"))
        {
            Inventory inv = collision.gameObject.GetComponent<Inventory>();

            // СТРОГАЯ ПРОВЕРКА: Если инвентарь есть И ключ в нем TRUE
            if (inv != null && inv.hasKey == true)
            {
                OpenDoor();
            }
            else
            {
                PlayLockedEffect();
            }
        }
    }
    void OpenDoor()
    {
        Debug.Log("Ключ подошел! Дверь открыта.");
        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);

        if (doorCollider != null) doorCollider.enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;

        // Использование нового метода вместо устаревшего
        MazeGenerator gen = Object.FindFirstObjectByType<MazeGenerator>();
        if (gen != null)
        {
            gen.ShowWinPanel();
            gen.Invoke("GenerateNewLevel", 1.5f); 
        }
// 
        // И здесь тоже меняем
        Inventory inv = Object.FindFirstObjectByType<Inventory>();
        if (inv != null)
        {
            inv.hasKey = false;
            if (inv.keyInInventoryIcon != null) 
                inv.keyInInventoryIcon.SetActive(false);
        }
    }
    void PlayLockedEffect()
    {
        Debug.Log("Дверь заперта! У игрока нет ключа.");
        if (audioSource != null && lockedSound != null)
        {
            if (!audioSource.isPlaying)
                audioSource.PlayOneShot(lockedSound);
        }
    }
}