using UnityEngine;
using UnityEngine.AI; // Для навигации
using UnityEngine.UI; // Для иконки и текста
using UnityEngine.SceneManagement; // Для перезагрузки игры
using System.Collections;

public class MonsterAI : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform player;          // Перетащи сюда Игрока
    public Animator animator;        // Компонент Animator монстра
    public Image proximityIcon;      // Иконка монстра на UI
    public GameObject gameOverPanel; // Панель проигрыша

    [Header("Настройки движения")]
    public float playerSpeed = 5f;    // Скорость игрока
    public float detectionRadius = 15f; // Расстояние, когда иконка начинает светиться
    
    private float fastSpeed; 
    private float slowSpeed; 

    private NavMeshAgent agent;
    private bool isDeadly = true;
    private AudioSource audioSource;

    [Header("Звуки")]
    public AudioClip hitSound; // Звук звона сковородки

    [Header("Событие Диван")]
    public Transform sofaPoint;      
    public GameObject sofaLight;     
    public Vector3 sittingScale = new Vector3(1f, 1f, 1f); 

    private bool isSofaEventActive = false;
    private Vector3 originalScale;
    private bool isScared = false; // Состояние страха после удара
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();

        // Настройки для 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // Рассчитываем скорости
        fastSpeed = playerSpeed * 3f;
        slowSpeed = playerSpeed * 0.3f;

        agent.speed = slowSpeed;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (player == null || agent == null) return;
        if (isSofaEventActive || isScared) return; // Если спит на диване или убегает в страхе — не преследует

        if (!agent.isOnNavMesh || !agent.enabled) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Логика изменения скорости преследования
        if (distance > 10f)
        {
            agent.speed = fastSpeed;
            agent.acceleration = 60f;
            agent.angularSpeed = 600f;
        }
        else
        {
            agent.speed = slowSpeed;
            agent.acceleration = 10f;
        }

        agent.SetDestination(player.position);

        // Анимация ходьбы (используем DirX/DirY как в твоем коде)
        if (agent.velocity.magnitude > 0.1f)
        {
            Vector2 moveDir = agent.velocity.normalized;
            animator.SetFloat("DirX", moveDir.x);
            animator.SetFloat("DirY", moveDir.y);
        }

        // Яркость иконки на UI
        float alpha = 1 - Mathf.Clamp01(distance / detectionRadius);
        if (proximityIcon != null)
        {
            Color c = proximityIcon.color;
            c.a = alpha;
            proximityIcon.color = c;
        }
    }

    // --- ПОЛУЧЕНИЕ УДАРА ---
    public void GetHit(Vector3 playerPos)
    {
        // Если монстр уже напуган или на диване — игнорируем удар
        if (isScared || isSofaEventActive) return;

        Debug.Log("Монстр получил сковородкой!");
        
        // 1. Звук удара с вариацией Pitch (для сочности)
        if (audioSource != null && hitSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f); 
            audioSource.PlayOneShot(hitSound);
        }

        // 2. Анимация удара (триггер Hit)
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }

        // 3. Логика побега
        StartCoroutine(RunAwayRoutine(playerPos));
    }

    IEnumerator RunAwayRoutine(Vector3 playerPosition)
    {
        isScared = true;
        float currentBaseSpeed = agent.speed;

        // Рассчитываем точку в противоположной стороне от игрока
        Vector3 runDirection = (transform.position - playerPosition).normalized;
        Vector3 targetPoint = transform.position + runDirection * 12f; 

        agent.speed = playerSpeed * 2.5f; // Убегает быстрее обычного игрока
        agent.SetDestination(targetPoint);

        yield return new WaitForSeconds(5f); // 5 секунд паники

        agent.speed = slowSpeed; // Возвращаем обычную скорость
        isScared = false;
    }

    // --- ОСТАЛЬНЫЕ СОБЫТИЯ ---
    public void StartSofaEvent()
    {
        if (!isSofaEventActive) StartCoroutine(SofaRoutine());
    }

    IEnumerator SofaRoutine()
    {
        isSofaEventActive = true;
        originalScale = transform.localScale;
        agent.SetDestination(sofaPoint.position);

        while (Vector2.Distance(transform.position, sofaPoint.position) > 0.2f)
        {
            yield return null;
        }

        agent.isStopped = true;
        transform.localScale = sittingScale;
        animator.SetBool("IsSitting", true);
        if (sofaLight != null) sofaLight.SetActive(true);

        yield return new WaitForSeconds(10f);

        if (sofaLight != null) sofaLight.SetActive(false);
        animator.SetBool("IsSitting", false);
        yield return null; 

        transform.localScale = originalScale; 
        agent.isStopped = false;
        isSofaEventActive = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && isDeadly && !isScared)
        {
            StartCoroutine(GameOverRoutine());
        }
    }

    IEnumerator GameOverRoutine()
    {
        isDeadly = false; 
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}