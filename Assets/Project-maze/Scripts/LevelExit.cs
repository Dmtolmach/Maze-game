using UnityEngine;
using System.Collections;

public class LevelExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Эта строчка напишет сообщение в консоль Unity, когда кто-то коснется двери
        Debug.Log("Кто-то коснулся выхода: " + collision.gameObject.name);

        if (collision.CompareTag("Player"))
        {
            Debug.Log("Игрок найден! Запускаю победу.");
            
            // Находим генератор на сцене
            MazeGenerator generator = Object.FindFirstObjectByType<MazeGenerator>();
            
            if (generator != null)
            {
                StartCoroutine(WinSequence(generator));
            }
        }
    }

    IEnumerator WinSequence(MazeGenerator generator)
    {
        // ИСПРАВЛЕНО: используем имя переменной 'generator', так как она указана в скобках выше
        generator.ShowWinPanel();

        // Ждем 2 секунды (или сколько нужно), пока висит табличка, перед созданием нового уровня
        yield return new WaitForSeconds(2f);

        // Генерируем новый уровень
        generator.GenerateNewLevel();
    }
}