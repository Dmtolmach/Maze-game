using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Состояние")]
    public bool hasPan = false;
    
    [Header("Ссылки")]
    public GameObject allPansGroup; // Перетащи сюда пустой объект-родитель (All_Pans), где лежат 4 вида сковородок
    public LayerMask monsterLayer;
    
    [Header("Настройки атаки")]
    public float attackRange = 1.5f;
    public float attackCooldown = 10f;
    private float lastAttackTime = -10f;

    [Header("Звуки")]
    public AudioClip swingSound; // Звук взмаха сковородкой
    private AudioSource audioSource;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // В начале игры сковородки скрыты
        if (allPansGroup != null) 
        {
            allPansGroup.SetActive(false);
        }
    }

    public void EquipWeapon(string weaponName)
    {
        Debug.Log("EquipWeapon вызван для: " + weaponName); // Проверка вызова
        if (weaponName == "Pan")
        {
            hasPan = true;
            
            if (allPansGroup != null) 
            {
                allPansGroup.SetActive(true);
                Debug.Log("Группа AllPansGroup включена");
            }

            if (animator != null)
            {
            animator.SetBool("HasPan", true); 
            Debug.Log("В Animator отправлено HasPan = true");
            
            Debug.Log("Сковородка активирована!");
            }
            else 
            {
                Debug.LogError("Аниматор не найден на игроке!");
            }            
        }
    }

    void Update()
    {
        if (hasPan && Input.GetMouseButtonDown(0))
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
            else
            {
                Debug.Log($"Перезарядка: {Mathf.CeilToInt(lastAttackTime + attackCooldown - Time.time)} сек.");
            }
        }
    }
    void Attack()
    {
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack");

        if (audioSource != null && swingSound != null)
        {
            audioSource.PlayOneShot(swingSound);
        }

        // Ищем ВСЕ коллайдеры в радиусе
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, monsterLayer);
        
        // Для отладки: выведем в консоль, сколько объектов мы зацепили
        Debug.Log("Взмах! Найдено целей: " + hitEnemies.Length);

        foreach (Collider2D enemy in hitEnemies)
        {
            MonsterAI monsterScript = enemy.GetComponent<MonsterAI>();
            if (monsterScript != null)
            {
                Debug.Log("ПОПАЛ ПО МОНСТРУ: " + enemy.name);
                monsterScript.GetHit(transform.position);
            }
        }
    }
    
}