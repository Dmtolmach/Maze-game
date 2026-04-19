using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{



    // Универсальный метод для проигрывания звука
    // --- ЛОГИКА КНОПОК ---

    public void GoToMainMenu()
    {
        // В кавычках напиши точное название твоей сцены с главным меню
        SceneManager.LoadScene("MainMenu"); 
    }
}