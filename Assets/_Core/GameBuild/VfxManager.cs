using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [System.Serializable]
    public struct VFXEntry
    {
        public string key;
        public GameObject prefab;
        [Min(1)] public int poolSize;
    }

    [Tooltip("VFX prefabs and their corresponding keys for pooling.")]
    [SerializeField] private List<VFXEntry> _vfxList;

    private Transform _vfxContainer;
    private readonly Dictionary<string, Queue<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, Coroutine> _activeCoroutines = new();
    private Transform sceneRoot;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (sceneRoot == null)
        {
            var found = GameObject.Find("[SCENE]");

            if (found != null)
                sceneRoot = found.transform;
            else
                GameLogger.LogError(GameLogger.LogCategory.System, $"[LevelBuilder] '[SCENE]' object not found in the scene! (Context: {gameObject.name})");
        }

        _vfxContainer = new GameObject("VFX_Container").transform;
        _vfxContainer.SetParent(sceneRoot, false);

        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var entry in _vfxList)
        {
            var pool = new Queue<GameObject>();

            for (int i = 0; i < entry.poolSize; i++)
            {
                GameObject obj = Instantiate(entry.prefab, _vfxContainer);

                obj.SetActive(false);

                pool.Enqueue(obj);
            }

            _pools[entry.key] = pool;
        }
    }

    public void PlayVFX(string key, Vector3 position)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            GameLogger.LogWarning(GameLogger.LogCategory.System, $"[VFXManager] Particle key '{key}' not found in dictionary. Make sure it's initialized.");

            return;
        }

        if (pool.Count == 0)
        {
            GameLogger.LogWarning(GameLogger.LogCategory.System, $"[VFXManager] Pool '{key}' is empty. Consider increasing the initial pool size.");

            return;
        }

        GameObject obj = pool.Dequeue();

        obj.transform.position = position;
        obj.SetActive(true);

        float duration = GetVFXDuration(obj);

        if (_activeCoroutines.TryGetValue(obj, out var existing))
            StopCoroutine(existing);

        _activeCoroutines[obj] = StartCoroutine(ReturnToPool(obj, key, duration));
    }

    private float GetVFXDuration(GameObject obj)
    {
        var systems = obj.GetComponentsInChildren<ParticleSystem>();

        if (systems.Length == 0) return 0.5f;

        float max = 0f;

        foreach (var ps in systems)
        {
            var lifetime = ps.main.startLifetime;

            float lifeMax = lifetime.mode == ParticleSystemCurveMode.Constant
                ? lifetime.constant
                : lifetime.constantMax;

            float duration = ps.main.duration + lifeMax;

            if (duration > max) 
            {
                max = duration;
            }
        }

        return max;
    }

    private IEnumerator ReturnToPool(GameObject obj, string key, float delay)
    {
        yield return new WaitForSeconds(delay);

        obj.SetActive(false);
        
        _pools[key].Enqueue(obj);

        _activeCoroutines.Remove(obj);
    }
}