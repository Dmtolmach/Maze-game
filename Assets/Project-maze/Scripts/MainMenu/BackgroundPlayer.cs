using UnityEngine;
using System.Collections;

public class BackgroundPlayer : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float wallDetectionDistance = 1.0f; // Увеличили для надежности
    public LayerMask wallLayer;
    public Transform lightPivot;
    public Animator animator;

    private Vector2 currentDirection = Vector2.right;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        StartCoroutine(RandomMovementRoutine());
    }

    void Update()
    {
        // Проверяем стену
        RaycastHit2D hit = Physics2D.Raycast(transform.position, currentDirection, wallDetectionDistance, wallLayer);
        
        // Визуально рисуем луч в окне SCENE
        Debug.DrawRay(transform.position, currentDirection * wallDetectionDistance, hit.collider != null ? Color.red : Color.blue);

        if (hit.collider != null)
        {
            Debug.Log("Вижу стену: " + hit.collider.name);
            FindFreeDirection();
        }
        else
        {
            transform.Translate(currentDirection * moveSpeed * Time.deltaTime);
        }
    }

    IEnumerator RandomMovementRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            FindFreeDirection();
        }
    }

    void FindFreeDirection()
    {
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        // Перемешиваем
        for (int i = 0; i < dirs.Length; i++) {
            int res = Random.Range(i, dirs.Length);
            var temp = dirs[i];
            dirs[i] = dirs[res];
            dirs[res] = temp;
        }

        foreach (Vector2 d in dirs)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, d, wallDetectionDistance, wallLayer);
            if (hit.collider == null)
            {
                currentDirection = d;
                UpdateVisuals();
                return;
            }
        }
    }

    void UpdateVisuals()
    {
        if (animator != null)
        {
            animator.SetFloat("InputX", currentDirection.x);
            animator.SetFloat("InputY", currentDirection.y);
        }

        if (lightPivot != null)
        {
            float angle = 0;
            if (currentDirection == Vector2.up) angle = 90;
            if (currentDirection == Vector2.down) angle = -90;
            if (currentDirection == Vector2.left) angle = 180;
            if (currentDirection == Vector2.right) angle = 0;
            lightPivot.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}