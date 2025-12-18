using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-900)]
public class FishPool : MonoBehaviour
{
    public static FishPool Instance { get; private set; }

    [Header("FishSpawner")]
    [SerializeField] FishSpawner fishspawner_new;

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, Transform> _parents = new();
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();
    private readonly Dictionary<GameObject, int> _capacity = new();
    private readonly Dictionary<GameObject, int> _created = new();

    // Track which instances we've already sent to FishSpawner to prevent duplicates
    private readonly HashSet<GameObject> _registeredWithSpawner = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void Prewarm(string fishId,GameObject prefab, int requestedCount, Transform parent = null)
    {
        if (prefab == null || requestedCount <= 0) return;

        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();
        if (parent != null && !_parents.ContainsKey(prefab)) _parents[prefab] = parent;

        int cap = _capacity.TryGetValue(prefab, out var oldCap) ? Mathf.Max(oldCap, requestedCount) : requestedCount;
        _capacity[prefab] = cap;

        int created = _created.TryGetValue(prefab, out var c) ? c : 0;
        int toCreate = Mathf.Max(0, cap - created);

        for (int i = 0; i < toCreate; i++)
        {
            var go = CreateNew(fishId, prefab, _parents.TryGetValue(prefab, out var p) ? p : null);
            go.SetActive(false);
            go.name = fishId;
            _pools[prefab].Enqueue(go);
            // SetObject now happens inside CreateNew, so no need to do it here.
        }
        _created[prefab] = created + toCreate;

        // Optional safety pass: ensure all pooled fish are registered with the spawner.
        // Useful if you had older objects created before the centralization.
        foreach (var go in _pools[prefab])
        {
            EnsureRegisteredWithSpawner(go);
        }
    }

    public GameObject Get(string fishId,GameObject prefab, Vector3 pos, Quaternion rot, Transform parentOverride = null)
    {
        if (prefab == null) return null;

        if (!_pools.ContainsKey(prefab)) _pools[prefab] = new Queue<GameObject>();
        if (parentOverride != null && !_parents.ContainsKey(prefab)) _parents[prefab] = parentOverride;

        int cap = _capacity.TryGetValue(prefab, out var capVal) ? capVal : 0;
        int created = _created.TryGetValue(prefab, out var cr) ? cr : 0;

        if (_pools[prefab].Count == 0)
        {
            if (created < cap)
            {
                var extra = CreateNew(fishId, prefab, _parents.TryGetValue(prefab, out var p) ? p : null);
                _created[prefab] = created + 1;
                extra.SetActive(false);
                _pools[prefab].Enqueue(extra);
            }
            else if (cap == 0 && created == 0) // lazy first spawn if you forgot to prewarm
            {
                var temp = CreateNew(fishId, prefab, _parents.TryGetValue(prefab, out var p) ? p : null);
                SetupSpawn(temp, pos, rot, parentOverride);
                _created[prefab] = 1;
                return temp;
            }
        }

        if (_pools[prefab].Count == 0) return null;

        var go = _pools[prefab].Dequeue();
        SetupSpawn(go, pos, rot, parentOverride);
        return go;
    }

    public void Release(GameObject instance)
    {
        if (instance == null) return;
        if (!_instanceToPrefab.TryGetValue(instance, out var key))
        {
            instance.SetActive(false);
            return;
        }
        instance.SetActive(false);
        _pools[key].Enqueue(instance);
        // Already registered at creation time; nothing to do here.
    }

    private GameObject CreateNew(string fishId,GameObject prefab, Transform parent)
    {
        var go = Instantiate(prefab, parent);
        _instanceToPrefab[go] = prefab;
        go.name = fishId;
        // Centralized registration: every newly created fish is registered exactly once
        EnsureRegisteredWithSpawner(go);
        return go;
    }

    private void EnsureRegisteredWithSpawner(GameObject go)
    {
        if (go == null || _registeredWithSpawner.Contains(go)) return;
        fishspawner_new.SetObject(go, go.name);
        _registeredWithSpawner.Add(go);
    }

    private void SetupSpawn(GameObject go, Vector3 pos, Quaternion rot, Transform parentOverride)
    {
        var t = go.transform;
        if (parentOverride != null && t.parent != parentOverride) t.SetParent(parentOverride);
        t.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
    }
}
