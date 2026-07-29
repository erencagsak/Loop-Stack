using UnityEngine;
using UnityEngine.Pool;

public class CubePoolManager : MonoBehaviour
{
    public static CubePoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [Tooltip("Estimated number of cubes to be ready in the pool at game start.")]
    [SerializeField] private int defaultCapacity = 50;
    
    [Tooltip("Maximum number of cubes in the pool. Cubes exceeding this limit will be destroyed upon return.")]
    [SerializeField] private int maxSize = 200;

    private ObjectPool<GameObject> _cubePool;
    private Transform _poolContainer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _poolContainer = new GameObject("Cube_Pool_Container").transform;
        _poolContainer.SetParent(transform);

        _cubePool = new ObjectPool<GameObject>(
            createFunc: CreateCube,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true, 
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private GameObject CreateCube()
    {
        GameObject prefab = ResourceManager.Instance.CubePrefab;
        
        if (prefab == null)
        {
            GameLogger.LogError(GameLogger.LogCategory.System, "CubePoolManager: CubePrefab has not been loaded yet!");

            return null;
        }

        GameObject cube = Instantiate(prefab, _poolContainer);

        return cube;
    }

    private void OnTakeFromPool(GameObject cube)
    {
        cube.SetActive(true);

        cube.transform.SetParent(null); 
    }

    private void OnReturnedToPool(GameObject cube)
    {
        cube.SetActive(false);

        cube.transform.SetParent(_poolContainer);
    }

    private void OnDestroyPoolObject(GameObject cube)
    {
        Destroy(cube);
    }

    public GameObject GetCube()
    {
        return _cubePool.Get();
    }

    public void ReleaseCube(GameObject cube)
    {
        if (cube == null) return;
        
        if (!cube.activeInHierarchy && cube.transform.parent == _poolContainer) return; 

        _cubePool.Release(cube);
    }
}