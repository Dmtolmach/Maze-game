using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using System.Reflection;

public class ShadowShapeSync : MonoBehaviour
{
    private List<GameObject> shadowPool = new List<GameObject>();

    // Кэшируем поля рефлексии для скорости
    private FieldInfo shapePathField;
    private FieldInfo applyToSortingLayersField;
    private FieldInfo castShadowsField;

    private void Awake()
    {
        // Получаем доступ к скрытым настройкам ShadowCaster2D
        shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        applyToSortingLayersField = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        castShadowsField = typeof(ShadowCaster2D).GetField("m_CastShadows", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public void SyncShadows()
    {
        Debug.Log("[ShadowSync] Запуск синхронизации (Цель: Floor)...");

        // 1. Очистка старых теней
        foreach (var obj in shadowPool) if (obj != null) Destroy(obj);
        shadowPool.Clear();

        // 2. Проверка компонентов
        var tilemapCollider = GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        var composite = GetComponent<CompositeCollider2D>();
        if (tilemapCollider == null || composite == null) return;

        // 3. Обновление геометрии
        tilemapCollider.ProcessTilemapChanges();
        composite.GenerateGeometry();

        int pathCount = composite.pathCount;
        if (pathCount == 0) {
            Debug.LogWarning("[ShadowSync] Пути не найдены! Проверьте 'Used By Composite' на коллайдере.");
            return;
        }

        // Получаем ID слоя Floor
        int[] targetLayers = new int[SortingLayer.layers.Length];
        for (int j = 0; j < SortingLayer.layers.Length; j++) {
            targetLayers[j] = SortingLayer.layers[j].id;
}

        // 4. Генерация объектов теней
        for (int i = 0; i < pathCount; i++)
        {
            Vector2[] points = new Vector2[composite.GetPathPointCount(i)];
            composite.GetPath(i, points);
            if (points.Length < 3) continue;

            GameObject sObj = new GameObject("Shadow_Segment_" + i);
            sObj.transform.SetParent(transform, false);
            sObj.transform.localPosition = Vector3.zero;
            shadowPool.Add(sObj);

            ShadowCaster2D caster = sObj.AddComponent<ShadowCaster2D>();
            
            // --- ЖЕСТКИЕ НАСТРОЙКИ ---
            caster.selfShadows = false; // Чтобы стены не чернели сами от себя

            // Включаем Cast Shadows (m_CastShadows = true)
            if (castShadowsField != null) castShadowsField.SetValue(caster, true);

            // Устанавливаем только слой Floor
            if (applyToSortingLayersField != null) applyToSortingLayersField.SetValue(caster, targetLayers);

            // Записываем форму (точки)
            Vector3[] shape = new Vector3[points.Length];
            for (int j = 0; j < points.Length; j++) shape[j] = (Vector3)points[j];
            if (shapePathField != null) shapePathField.SetValue(caster, shape);
            
            // Перезапуск для применения
            caster.enabled = false;
            caster.enabled = true;
        }

        Debug.Log($"[ShadowSync] Готово! Создано {shadowPool.Count} теней для слоя Floor.");
    }
}