using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Dreamteck.Splines;

public class LevelBuilder : MonoBehaviour
{
    // Inspector
    [Header("Debug / Testing")]
    [Tooltip("0 = play normally (PlayerPrefs). Value > 0 forces that level on start.")]
    public int forceDebugLevel = 0;

    [Header("Settings")]
    [Tooltip("Size of each grid cell in world units. Adjust to fit your level design.")]
    public float cellSize = 0.8f;

    [Header("Color Palette")]
    [Tooltip("ScriptableObject asset — create via Right Click > Create > Game > Cube Color Palette")]
    [SerializeField] private CubeColorPalette colorPalette;

    public static LevelBuilder Instance;

    private GameObject _levelContainer;
    private Transform _activeConveyorCubesContainer;
    private Transform sceneRoot;

    private readonly List<GameObject> _activeCubes = new List<GameObject>();

    // Current level index
    public int CurrentLoadedLevel { get; private set; } = -1;

    private void Awake()
    {
        if (sceneRoot == null)
        {
            var found = GameObject.Find("[SCENE]");

            if (found != null)
                sceneRoot = found.transform;
            else
                GameLogger.LogError(GameLogger.LogCategory.System, $"[LevelBuilder] '[SCENE]' object not found in the scene! (Context: {gameObject.name})");
        }

        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }

        DG.Tweening.DOTween.SetTweensCapacity(2000, 500);
    }

    private void Start()
    {
        if (forceDebugLevel > 0)
        {
            PlayerPrefs.SetInt("CurrentLevel", forceDebugLevel);
            PlayerPrefs.Save();

            GameLogger.Log(GameLogger.LogCategory.System, $"LevelBuilder: Forcing Level {forceDebugLevel}");
        }
    }

    [ContextMenu("Load Debug Level")]
    public void ReloadDebugLevel()
    {
        int target = forceDebugLevel > 0 ? forceDebugLevel : PlayerPrefs.GetInt("CurrentLevel", 1);

        if (forceDebugLevel > 0)
        {
            PlayerPrefs.SetInt("CurrentLevel", forceDebugLevel);
            PlayerPrefs.Save();
        }

        LoadLevelAsync(target).Forget(); 
    }

    public GameObject GetCube(string colorCode, out Color cubeColor)
    {
        cubeColor = colorPalette != null ? colorPalette.GetColor(colorCode) : Color.magenta;
        
        GameObject cube = CubePoolManager.Instance.GetCube();
        
        if (_activeConveyorCubesContainer != null)
        {
            cube.transform.SetParent(_activeConveyorCubesContainer, false);
        }

        cube.transform.localPosition = Vector3.zero;
        cube.transform.localRotation = Quaternion.identity;

        _activeCubes.Add(cube);
        
        return cube;
    }

    public void ReturnCubeToPool(GameObject cube)
    {
        if (cube == null) return;

        _activeCubes.Remove(cube);
        
        CubePoolManager.Instance.ReleaseCube(cube);
    }

    public async UniTask LoadLevelAsync(int levelIndex)
    {
        try
        {
            ReturnAllCubesToPool();
            DestroyLevelContainer();

            await ResourceManager.Instance.LoadLevelPrefabsAsync();

            LevelData currentLevel = Resources.Load<LevelData>($"Levels/Level_{levelIndex}");

            if (currentLevel == null)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.System, $"Level {levelIndex} not found. Looping back.");

                levelIndex = 1;

                PlayerPrefs.SetInt("CurrentLevel", 1);
                PlayerPrefs.Save();
                
                currentLevel = Resources.Load<LevelData>($"Levels/Level_1");
            }

            if (currentLevel != null)
            {
                _levelContainer = new GameObject($"Level_{levelIndex}");
                _levelContainer.transform.SetParent(sceneRoot, false);

                Vector3 offset = CalculateCenterOffset(currentLevel);

                BuildConveyor(currentLevel, offset);
                BuildTrucks(currentLevel, offset);

                CurrentLoadedLevel = levelIndex;
                
                if (LevelRuleManager.Instance != null)
                    LevelRuleManager.Instance.UpdateCubeProgress();
            }
            else 
            {
                GameLogger.LogError(GameLogger.LogCategory.System, "Critical Error: Even Level_1 could not be found in Resources/Levels!");
            }
        }
        catch (System.Exception e) {
            GameLogger.LogError(GameLogger.LogCategory.System, $"LEVEL BUILDER ERROR: {e.Message}\n{e.StackTrace}");
        }
    }

    private void BuildConveyor(LevelData levelData, Vector3 offset)
    {
        if (levelData.Splines.Count == 0)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.System, "BuildConveyor: no spline points in this level.");

            return;
        }

        var ordered = levelData.Splines.OrderBy(s => s.SplineIndex).ToList();

        SplineComputer spline = Instantiate(ResourceManager.Instance.SplinePrefab, Vector3.zero, Quaternion.identity);

        spline.name = $"Conveyor_Level_{levelData.LevelNumber}";
        spline.transform.SetParent(_levelContainer.transform);

        var cubesObj = new GameObject("Cubes");

        cubesObj.transform.SetParent(spline.transform, false);
        _activeConveyorCubesContainer = cubesObj.transform;

        var points = new SplinePoint[ordered.Count];

        for (int i = 0; i < ordered.Count; i++)
        {
            points[i] = new SplinePoint
            {
                position = GetWorldPosition(ordered[i].Row, ordered[i].Col) + offset,
                normal   = Vector3.up,
                size     = 1f,
                color    = Color.white
            };
        }

        spline.SetPoints(points);
        spline.Close();
        spline.RebuildImmediate();

        GameLogger.Log(GameLogger.LogCategory.System, $"Conveyor built for Level {levelData.LevelNumber} ({ordered.Count} points).");
    }

    private void BuildTrucks(LevelData levelData, Vector3 offset)
    {
        foreach (var truck in levelData.Trucks)
        {
            Vector3 pos = GetWorldPosition(truck.Row, truck.Col) + offset;
            Quaternion rot = Quaternion.Euler(truck.Rotation);

            GameObject obj = Instantiate(ResourceManager.Instance.TruckPrefab, pos, rot);

            obj.name = $"Truck_{truck.TruckName}";
            obj.transform.SetParent(_levelContainer.transform);

            if (Application.isPlaying) 
            {
                if (obj.TryGetComponent<TruckController>(out var controller))
                {
                    controller.Initialize(truck.CubeColors, this, _activeConveyorCubesContainer);
                }
            }
        }

        if (Application.isPlaying)
        {
            GameLogger.Log(GameLogger.LogCategory.System, $"Spawned {levelData.Trucks.Count} trucks for Level {levelData.LevelNumber}.");
        }
    }

    private Vector3 GetWorldPosition(int row, int col)
        => new Vector3(col * cellSize, 0f, -row * cellSize);

    private Vector3 CalculateCenterOffset(LevelData levelData)
    {
        if (levelData.Splines.Count == 0 && levelData.Trucks.Count == 0)
            return Vector3.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        void Expand(int row, int col)
        {
            Vector3 p = GetWorldPosition(row, col);
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p.z > maxZ) maxZ = p.z;
        }

        foreach (var s in levelData.Splines) Expand(s.Row, s.Col);
        
        foreach (var t in levelData.Trucks)  Expand(t.Row, t.Col);

        return new Vector3(-(minX + maxX) / 2f, 0f, -(minZ + maxZ) / 2f);
    }

    private void ReturnAllCubesToPool()
    {
        foreach (var cube in _activeCubes.ToList())
            ReturnCubeToPool(cube);
    }

    private void DestroyLevelContainer()
    {
        if (_levelContainer != null)
        {
            Destroy(_levelContainer);
            
            _levelContainer = null;
        }
    }
}