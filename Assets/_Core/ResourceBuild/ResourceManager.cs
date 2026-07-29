using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using Dreamteck.Splines;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Addressable Keys")]
    public string SplineKey = "Prefabs/Conveyor_Spline";
    public string TruckKey = "Prefabs/Truck";
    public string CubeKey = "Prefabs/Cube";

    public SplineComputer SplinePrefab { get; private set; }
    public GameObject TruckPrefab { get; private set; }
    public GameObject CubePrefab { get; private set; }

    private AsyncOperationHandle<GameObject> _splineHandle;
    private AsyncOperationHandle<GameObject> _truckHandle;
    private AsyncOperationHandle<GameObject> _cubeHandle;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    public async UniTask LoadLevelPrefabsAsync()
    {
        if (SplinePrefab != null && TruckPrefab != null && CubePrefab != null) return;

        _splineHandle = Addressables.LoadAssetAsync<GameObject>(SplineKey);
        _truckHandle = Addressables.LoadAssetAsync<GameObject>(TruckKey);
        _cubeHandle = Addressables.LoadAssetAsync<GameObject>(CubeKey);

        await UniTask.WhenAll(
            _splineHandle.ToUniTask(),
            _truckHandle.ToUniTask(),
            _cubeHandle.ToUniTask()
        );

        if (_splineHandle.Status == AsyncOperationStatus.Succeeded)
            SplinePrefab = _splineHandle.Result.GetComponent<SplineComputer>();
            
        if (_truckHandle.Status == AsyncOperationStatus.Succeeded)
            TruckPrefab = _truckHandle.Result;
            
        if (_cubeHandle.Status == AsyncOperationStatus.Succeeded)
            CubePrefab = _cubeHandle.Result;

        GameLogger.Log(GameLogger.LogCategory.System, "ResourceManager: All base prefabs loaded successfully.");
    }

    private void OnDestroy()
    {
        if (_splineHandle.IsValid()) Addressables.Release(_splineHandle);
        if (_truckHandle.IsValid()) Addressables.Release(_truckHandle);
        if (_cubeHandle.IsValid()) Addressables.Release(_cubeHandle);
    }
}