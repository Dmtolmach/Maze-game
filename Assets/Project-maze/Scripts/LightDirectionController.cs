using UnityEngine;

public class LightDirectionController : MonoBehaviour
{
    [Header("Настройки вращения")]
    public float rotationSpeed = 10f; // Скорость плавного поворота
    public float angleOffset = -90f;  // Корректировка, если свет смотрит не туда (обычно -90 для 2D)

    private Vector2 lastMoveDirection;

    void Update()
    {
        // 1. Получаем направление движения из ввода
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 moveInput = new Vector2(moveX, moveY);

        // 2. Если игрок движется — запоминаем направление
        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput.normalized;
        }

        // 3. Если у нас есть направление — поворачиваем свет
        if (lastMoveDirection.sqrMagnitude > 0.01f)
        {
            // Вычисляем угол в градусах
            float targetAngle = Mathf.Atan2(lastMoveDirection.y, lastMoveDirection.x) * Mathf.Rad2Deg;
            
            // Создаем целевой поворот вокруг оси Z
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle + angleOffset);

            // Плавный поворот к цели
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}