using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // За кем следить (Игрок)
    public float smoothSpeed = 0.125f; // Плавность (чем меньше, тем плавнее)
    public Vector3 offset = new Vector3(0, 0, -5); // Сдвиг (Z должен быть -10)

    void LateUpdate()
    {
        if (target == null) return;

        // Куда камера хочет попасть
        Vector3 desiredPosition = target.position + offset;
        
        // Плавно перемещаем камеру из текущей позиции в желаемую
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        transform.position = smoothedPosition;
    }
}