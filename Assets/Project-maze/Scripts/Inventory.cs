using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
   
    [Header("Иконки инвентаря")]
    public GameObject keyInInventoryIcon; 
    public GameObject panInInventoryIcon; // Эта переменная должна быть здесь!

    [Header("Состояние предметов")]
    public bool hasKey = false;
    public bool hasPan = false; // И эта тоже!

    void Start()
    {
        // В начале игры скрываем иконки, если они не должны быть видны сразу
        if (keyInInventoryIcon != null && !hasKey) keyInInventoryIcon.SetActive(false);
        if (panInInventoryIcon != null && !hasPan) panInInventoryIcon.SetActive(false);
    }
    public void AddKey()
    {
        // Если ключ уже есть, не выполняем код дальше
        if (hasKey) return; 

        hasKey = true;
        
        if (keyInInventoryIcon != null)
        {
            keyInInventoryIcon.SetActive(true); // Включаем картинку в инвентаре
        }

    }
    public void AddPan()
    {
        hasPan = true;
        if (panInInventoryIcon != null)
        {
            panInInventoryIcon.SetActive(true); // Включаем иконку в UI
        }
    }
}