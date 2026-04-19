using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Окна")]
    public GameObject authorsPanel; 

    [Header("Настройки сцены")]
    public string gameSceneName = "GameLevel"; 

    private AudioSource audioSource;

    void Start()
    {
        // Ищем AudioSource на том же объекте, где висит скрипт (на Canvas)
        audioSource = GetComponent<AudioSource>();
        
        if (authorsPanel != null) authorsPanel.SetActive(false);

    // Загружаем фоновую сцену АДДИТИВНО (чтобы она работала вместе с меню)
        SceneManager.LoadScene("BackgroundScene", LoadSceneMode.Additive);
    }

    // Универсальный метод для проигрывания звука
    private void PlaySound()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    // --- ЛОГИКА КНОПОК ---

    public void PlayFreeMode()
    {
        PlaySound();
        // Загружаем сцену через небольшую паузу, чтобы звук успел начаться
        Invoke("LoadGame", 0.2f);
    }

    private void LoadGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ToggleAuthors(bool show)
    {
        PlaySound();
        if (authorsPanel != null) authorsPanel.SetActive(show);
    }

    public void ExitGame()
    {
        PlaySound();
        Debug.Log("Выход из игры...");
        // Выход сработает только в скомпилированной игре (.exe)
        Application.Quit();
    }
// Метод для перехода в главное меню
}