using UnityEngine;

public class SofaTrigger : MonoBehaviour 
{
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что это монстр и что событие еще не случалось
        if (!hasTriggered && other.CompareTag("Monster")) 
        {
            MonsterAI monster = other.GetComponent<MonsterAI>();
            if (monster != null)
            {
                monster.StartSofaEvent(); // Запускаем событие в скрипте монстра
                hasTriggered = true;     // Больше не сработает
            }
        }

        // Это сработает вообще на любой объект, даже если теги не настроены
        Debug.Log("ФИЗИЧЕСКИЙ КОНТАКТ С: " + other.gameObject.name);
        
        if (other.CompareTag("Monster")) 
        {
            Debug.Log("ТРИГГЕР УВИДЕЛ МОНСТРА!");
            // ... твой код ...
        }
    }
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}