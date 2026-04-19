using UnityEngine;
using UnityEngine.UI; 
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System.Collections;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.SceneManagement; // Для перезагрузки игры
using UnityEngine.Rendering.Universal;
using System.Reflection;


public class MazeGenerator : MonoBehaviour
{
    [System.Serializable]
    public class RoomRow
    {
        public bool[] columns = new bool[10];
    }
    public NavMeshSurface surface;

    [Header("Настройки Tilemap")]
    public Tilemap wallTilemap;
    public Tilemap floorTilemap;
    public TileBase wallTile;

    [Header("Настройки красивого пола")]
    public TileBase mainFloorTile; // Основная чистая клетка
    public TileBase[] rareFloorTiles; // Массив клеток с грязью/трещинами
    [Range(0, 100)] public int rareTileChance = 15; // Шанс (в процентах), что выпадет редкий тайл

    // [Header("Префабы")]
    public GameObject exitPrefab;
    public GameObject startPrefab;
    public Transform player;

    [Header("UI")]
    public GameObject winPanel;
    public Image keyProximityIcon; 

    [Header("Размеры (Нечетные!)")]
    public int width = 51;
    public int height = 51;

    [Header("Ручная комната 10x10")]
    public List<RoomRow> startRoomLayout = new List<RoomRow>();

    [Header("Параметры")]
    [Range(0, 0.2f)] public float loopChance = 0.1f;

    [Header("Монстры")]
    public GameObject monsterPrefab; 
    public Transform playerTransform; 

    [Header("Навигация")]
    public GameObject wallObstaclePrefab; 

    [Header("Настройки Ключа")]
    public GameObject keyPrefab;
    public bool useRandomPosition = true;
    public Vector3 fixedKeyPosition;

    [Header("Настройки сковороды")]
    public GameObject panPrefab;

    [Header("Настройки кровати")]
    public GameObject bedPrefab; 
    [Header("Настройки Rule Tile")]
    public TileBase smartWallTile; // Сюда в инспекторе перетащи свой файл "SmartWall"

    private int[,] map;
    private List<GameObject> currentLevelObjects = new List<GameObject>();
    private Vector2Int roomOffset = new Vector2Int(1, 1);
    private List<Vector3> allFloorPositions = new List<Vector3>();




    private void OnValidate()
    {
        if (startRoomLayout == null) startRoomLayout = new List<RoomRow>();
        while (startRoomLayout.Count < 10) startRoomLayout.Add(new RoomRow());
        foreach (var row in startRoomLayout)
        {
            if (row.columns == null || row.columns.Length != 10) row.columns = new bool[10];
        }
    }



    void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        GenerateNewLevel();
        StartCoroutine(SetupNavigation());
        SpawnKey(allFloorPositions);
        SpawnPan(allFloorPositions); 
    }

    IEnumerator SetupNavigation()
    {
        yield return new WaitForSeconds(1.0f);
        Physics2D.SyncTransforms();

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.RemoveData();
        surface.BuildNavMesh();

        if (surface != null)
        {
            surface.RemoveData();
            surface.BuildNavMesh();
        }

        yield return new WaitForSeconds(1.5f);
        yield return new WaitForEndOfFrame();

        TilemapCollider2D tc = wallTilemap.GetComponent<TilemapCollider2D>();
        CompositeCollider2D cc = wallTilemap.GetComponent<CompositeCollider2D>();
        if (tc == null)
        {
            Debug.LogError("TilemapCollider2D is NULL!");
        }
        else
        {
        tc.compositeOperation = Collider2D.CompositeOperation.Merge; // ← this is the fix
        Debug.Log($"tc found. compositeOperation={tc.compositeOperation}");
        }
        Rigidbody2D rb = wallTilemap.GetComponent<Rigidbody2D>();

        // --- DIAGNOSTIC ---
        Debug.Log($"TilemapCollider2D: {(tc == null ? "MISSING" : $"enabled={tc.enabled}, usedByComposite={tc.usedByComposite}, shapeCount={tc.shapeCount}")}");
        Debug.Log($"CompositeCollider2D: {(cc == null ? "MISSING" : $"enabled={cc.enabled}, pathCount={cc.pathCount}, geometryType={cc.geometryType}")}");
        Debug.Log($"Rigidbody2D: {(rb == null ? "MISSING" : $"bodyType={rb.bodyType}")}");
        Debug.Log($"wallTilemap tile count: {wallTilemap.GetUsedTilesCount()}");

        // --- FORCE REBUILD ---
        tc.enabled = false;
        cc.enabled = false;
        yield return new WaitForEndOfFrame();

        tc.enabled = true;
        yield return new WaitForEndOfFrame();
        tc.ProcessTilemapChanges();
        yield return new WaitForEndOfFrame();

        cc.enabled = true;
        cc.GenerateGeometry();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Debug.Log($"AFTER REBUILD — pathCount: {cc.pathCount}, shapeCount: {tc.shapeCount}");

        GenerateGlobalShadows();
        ActivateMonster();
    }

    public void GenerateNewLevel()
    {
        // 1. Очистка
        wallTilemap.ClearAllTiles();
        floorTilemap.ClearAllTiles();
        foreach (var obj in currentLevelObjects) if (obj != null) Destroy(obj);
        currentLevelObjects.Clear();

        // 2. Инициализация карты
        map = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++) map[x, y] = 1;

        PlaceManualRoom();

        // 3. Генерация проходов
        Vector2Int exitFromRoom = new Vector2Int(roomOffset.x + 5, roomOffset.y + 10);
        map[exitFromRoom.x, exitFromRoom.y] = 0; 
        CarvePassage(new Vector2Int(exitFromRoom.x, exitFromRoom.y + 1));

        CreateLoops();
        BuildLevelWithTiles();

        // 4. ОПРЕДЕЛЕНИЕ СТАРТОВОЙ ТОЧКИ (ОДИН РАЗ)
        Vector3 startPos = new Vector3(roomOffset.x + 5, roomOffset.y + 5, 0);

        // 5. СПАВН КРОВАТИ
        if (bedPrefab != null)
        {
            GameObject spawnedBed = Instantiate(bedPrefab, startPos, Quaternion.identity);
            currentLevelObjects.Add(spawnedBed);
        }

        // 6. УСТАНОВКА ИГРОКА НА КРОВАТЬ
        // Смещение 0.2f вверх, чтобы спрайт игрока лежал на кровати, а не под ней
        Vector3 playerBedPos = startPos + new Vector3(-0.5f, 0.2f, 0); 
        ResetPlayer(playerBedPos);

        // 7. Остальные объекты
        if (startPrefab != null)
            currentLevelObjects.Add(Instantiate(startPrefab, startPos, Quaternion.identity));
        SofaTrigger sofaTrigger = Object.FindFirstObjectByType<SofaTrigger>();
        if (sofaTrigger != null) sofaTrigger.ResetTrigger();

        PlaceExit(new Vector2Int((int)startPos.x, (int)startPos.y));
        //UpdateShadows();
    }

    void PlaceManualRoom()
    {
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                bool isWall = startRoomLayout[9 - y].columns[x];
                map[roomOffset.x + x, roomOffset.y + y] = isWall ? 1 : 0;
            }
        }
    }

    void CarvePassage(Vector2Int pos)
    {
        map[pos.x, pos.y] = 0;
        List<Vector2Int> dirs = new List<Vector2Int> { Vector2Int.up * 2, Vector2Int.down * 2, Vector2Int.left * 2, Vector2Int.right * 2 };
        for (int i = 0; i < dirs.Count; i++)
        {
            int r = Random.Range(i, dirs.Count);
            var t = dirs[i]; dirs[i] = dirs[r]; dirs[r] = t;
        }
        foreach (var d in dirs)
        {
            Vector2Int next = pos + d;
            if (next.x > 0 && next.x < width - 1 && next.y > 0 && next.y < height - 1 && map[next.x, next.y] == 1)
            {
                map[pos.x + d.x / 2, pos.y + d.y / 2] = 0;
                CarvePassage(next);
            }
        }
    }

    void CreateLoops()
    {
        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
                if (map[x, y] == 1 && Random.value < loopChance)
                    if ((map[x-1, y] == 0 && map[x+1, y] == 0) || (map[x, y-1] == 0 && map[x, y+1] == 0))
                        map[x, y] = 0;
    }


  void BuildLevelWithTiles()
    {
        allFloorPositions.Clear();
        wallTilemap.ClearAllTiles();
        floorTilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int p = new Vector3Int(x, y, 0);
                
                // 1. Сначала определяем, какой тайл пола положить
                TileBase selectedFloor;

                // Кидаем "кубик" от 0 до 100
                if (Random.Range(0, 100) < rareTileChance && rareFloorTiles.Length > 0)
                {
                    // Выпал шанс на редкую клетку! Берем случайную из массива
                    selectedFloor = rareFloorTiles[Random.Range(0, rareFloorTiles.Length)];
                }
                else
                {
                    // В большинстве случаев кладем обычный пол
                    selectedFloor = mainFloorTile;
                }

                // Создаем матрицу поворота (случайно 0, 90, 180 или 270 градусов)
                float[] rotations = { 0, 90, 180, 270 };
                float randomRot = rotations[Random.Range(0, rotations.Length)];
                Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, randomRot), Vector3.one);

                // Устанавливаем тайл с поворотом
                floorTilemap.SetTile(p, selectedFloor);
                floorTilemap.SetTransformMatrix(p, matrix);

                if (map[x, y] == 1) 
                {
                    // Ставим спрайт стены
                    wallTilemap.SetTile(p, smartWallTile);

                    // --- ВОТ ЭТОТ БЛОК НУЖНО ВСТАВИТЬ СЮДА ---
                    // Создаем невидимый 3D коллайдер для NavMesh
                    GameObject wall3D = new GameObject("Wall3D_Collider");
                    // Смещаем на 0.5f, чтобы куб был точно в центре клетки тайла
                    wall3D.transform.position = new Vector3(x + 0.5f, y + 0.5f, 1f); 
                    wall3D.transform.parent = wallTilemap.transform;

                    // Добавляем 3D куб
                    BoxCollider box = wall3D.AddComponent<BoxCollider>();
                    box.size = new Vector3(1f, 1f, 2f); 

                    // Помечаем как непроходимую зону
                    NavMeshModifier mod = wall3D.AddComponent<NavMeshModifier>();
                    mod.overrideArea = true;
                    mod.area = NavMesh.GetAreaFromName("Not Walkable");

                    // Добавляем в список для удаления при регенерации
                    currentLevelObjects.Add(wall3D);
                    // ------------------------------------------
                }
                else
                {
                    // Если это не стена, добавляем в список пола для спавна предметов
                    allFloorPositions.Add(new Vector3(x + 0.5f, y + 0.5f, 0f));
                }
            }
        }
        //UpdateShadows();
    }
    public void GenerateGlobalShadows()
    {
        GameObject existing = GameObject.Find("GlobalShadows");
        if (existing != null) Destroy(existing);

        CompositeCollider2D composite = wallTilemap.GetComponent<CompositeCollider2D>();
        if (composite == null || composite.pathCount == 0)
        {
            Debug.LogError($"CompositeCollider2D has {(composite == null ? "null" : "0 paths")} — aborting shadow gen.");
            return;
        }

        GameObject shadowRoot = new GameObject("GlobalShadows");
        shadowRoot.transform.SetParent(wallTilemap.transform);
        shadowRoot.transform.localPosition = Vector3.zero;

        FieldInfo shapePathField = typeof(ShadowCaster2D)
            .GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo shapePathHashField = typeof(ShadowCaster2D)
            .GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);

        for (int i = 0; i < composite.pathCount; i++)
        {
            int pointCount = composite.GetPathPointCount(i);
            Vector2[] path2D = new Vector2[pointCount];
            composite.GetPath(i, path2D);

            Vector3[] path3D = new Vector3[pointCount];
            for (int j = 0; j < pointCount; j++)
                path3D[j] = path2D[j];

            GameObject casterObj = new GameObject($"ShadowCaster_{i}");
            casterObj.transform.SetParent(shadowRoot.transform);
            casterObj.transform.localPosition = Vector3.zero;

            ShadowCaster2D caster = casterObj.AddComponent<ShadowCaster2D>();
            caster.selfShadows = false;

            // Only cast shadows onto the Floor layer, not the Wall layer
            FieldInfo applyToSortingLayersField = typeof(ShadowCaster2D)
                .GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (applyToSortingLayersField != null)
            {
                int wallLayerID = SortingLayer.NameToID("Wall");
                List<int> layerIDs = new List<int>();
                foreach (var layer in SortingLayer.layers)
                    if (layer.id != wallLayerID)
                        layerIDs.Add(layer.id);
                applyToSortingLayersField.SetValue(caster, layerIDs.ToArray());
            }
            if (shapePathField != null)
                shapePathField.SetValue(caster, path3D);
            if (shapePathHashField != null)
                shapePathHashField.SetValue(caster, Random.Range(int.MinValue, int.MaxValue));
        }

        shadowRoot.AddComponent<CompositeShadowCaster2D>();
        Debug.Log($"Wall shadows refreshed. {composite.pathCount} casters created.");
    }
    // Вспомогательный метод для вычисления "соседства"
    int GetWallIndex(int x, int y)
    {
        int index = 0;

        // Бинарная маска: Сверху(1), Справа(2), Снизу(4), Слева(8)
        if (IsWallAt(x, y + 1)) index += 1; // North
        if (IsWallAt(x + 1, y)) index += 2; // East
        if (IsWallAt(x, y - 1)) index += 4; // South
        if (IsWallAt(x - 1, y)) index += 8; // West

        return index;
    }

    bool IsWallAt(int x, int y)
    {
        // Считаем границы лабиринта стенами для красоты стыковки
        if (x < 0 || x >= width || y < 0 || y >= height) return true;
        return map[x, y] == 1;
    }

    void PlaceExit(Vector2Int start)
    {
        Vector2Int farPos = start;
        float maxD = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (map[x, y] == 0)
                {
                    float d = Vector2.Distance(start, new Vector2(x, y));
                    if (d > maxD) { maxD = d; farPos = new Vector2Int(x, y); }
                }
        currentLevelObjects.Add(Instantiate(exitPrefab, new Vector3(farPos.x + 0.5f, farPos.y + 0.5f, 0), Quaternion.identity));
    }

    void ResetPlayer(Vector3 pos)
    {
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.position = pos; }
            player.position = pos;
        }
    }

   // void UpdateShadows()
   // {
   //     var comp = wallTilemap.GetComponent<CompositeCollider2D>();
   //     if (comp != null) comp.GenerateGeometry();
   //     wallTilemap.gameObject.SendMessage("SyncShadows", SendMessageOptions.DontRequireReceiver);
   // }

    public void ShowWinPanel()
    {
        if (winPanel != null)
        {
            StopAllCoroutines(); 
            StartCoroutine(HidePanelRoutine());
        }
    }

    private IEnumerator HidePanelRoutine()
    {
        winPanel.SetActive(true); 
        yield return new WaitForSeconds(5f); 
        winPanel.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
 
    }

    void ActivateMonster()
    {
        NavMeshTriangulation navData = NavMesh.CalculateTriangulation();
        if (navData.vertices.Length == 0) return;
        Vector3 basePos = allFloorPositions[Random.Range(0, allFloorPositions.Count)];
        Vector3 spawnPos = new Vector3(basePos.x, basePos.y, surface.transform.position.z);
        if (monsterPrefab == null) return;
        GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
        currentLevelObjects.Add(monster);
        NavMeshAgent agent = monster.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false; 
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(spawnPos, out hit, 2.0f, NavMesh.AllAreas))
            {
                monster.transform.position = hit.position;
                agent.enabled = true;
                agent.Warp(hit.position); 
            }
        }
    }

    public void SpawnKey(List<Vector3> floorPositions)
    {
        if (keyPrefab == null || floorPositions.Count == 0) return;
        Vector3 spawnPos = useRandomPosition ? floorPositions[Random.Range(0, floorPositions.Count)] : fixedKeyPosition;
        GameObject keyObj = Instantiate(keyPrefab, spawnPos, Quaternion.identity);
        currentLevelObjects.Add(keyObj);
        KeyItem keyScript = keyObj.GetComponent<KeyItem>();
        if (keyScript != null) keyScript.Setup(player, keyProximityIcon);
    }
        public void SpawnPan(List<Vector3> floorPositions)
    {
        if (panPrefab == null || floorPositions.Count == 0) return;

        // Выбираем случайную точку пола (можно сделать проверку, чтобы не на кровать)
        Vector3 spawnPos = floorPositions[Random.Range(0, floorPositions.Count)];

        // Создаем сковородку
        if (panPrefab != null) // Используем твою переменную для префаба сковородки
        {
            GameObject panObj = Instantiate(panPrefab, spawnPos, Quaternion.identity);
            currentLevelObjects.Add(panObj);
        }
    }
}