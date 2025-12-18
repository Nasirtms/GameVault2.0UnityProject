using Supabase.Storage;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Make sure pool awakens early (optional but helps)
[DefaultExecutionOrder(-1000)]
public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();

    private readonly Dictionary<GameObject, Transform> _parents = new();

    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();

    private readonly Dictionary<GameObject, int> _capacity = new();
    private readonly Dictionary<GameObject, int> _created = new();

    //public TMP_InputField bulletlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }
    //private void FixedUpdate()
    //{
    //    //if (int.TryParse(bulletlayer.text, out int layerOrder))
    //    //{
    //        foreach (var bullet in _activeBullets)
    //        {
    //            if (bullet != null && bullet.activeInHierarchy)
    //            {
    //                var sr = bullet.GetComponent<SpriteRenderer>();
    //                //if (sr != null)
    //                    //sr.sortingOrder = layerOrder;
    //            }
    //        }
    //    //}
    //}
    public void Prewarm(GameObject prefab, int requestedCount, Transform parent = null)
    {
        if (prefab == null || requestedCount <= 0) return;

        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        if (parent != null && !_parents.ContainsKey(prefab))
            _parents[prefab] = parent;

        int cap = _capacity.TryGetValue(prefab, out var existingCap)
                  ? Mathf.Max(existingCap, requestedCount)  
                  : requestedCount;

        _capacity[prefab] = cap;

        int created = _created.TryGetValue(prefab, out var c) ? c : 0;
        int toCreate = Mathf.Max(0, cap - created);

        for (int i = 0; i < toCreate; i++)
        {
            var go = CreateNew(prefab, _parents.GetValueOrDefault(prefab));
            go.SetActive(false);
            _pools[prefab].Enqueue(go);
        }

        _created[prefab] = created + toCreate;
    }

    public GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot, Transform parentOverride = null)
    {
        if (prefab == null) return null;

        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Queue<GameObject>();

        if (parentOverride != null && !_parents.ContainsKey(prefab))
            _parents[prefab] = parentOverride;

        int cap = _capacity.TryGetValue(prefab, out var capVal) ? capVal : 0;
        int created = _created.TryGetValue(prefab, out var cr) ? cr : 0;

        if (_pools[prefab].Count == 0)
        {
            if (created < cap)
            {
                var extra = CreateNew(prefab, _parents.GetValueOrDefault(prefab));
                _created[prefab] = created + 1;
                extra.SetActive(false);
                _pools[prefab].Enqueue(extra);
            }
            else if (cap == 0 && created == 0)
            {
                var goTemp = CreateNew(prefab, _parents.GetValueOrDefault(prefab));
                SetupSpawn(goTemp, pos, rot, parentOverride);
                _created[prefab] = 1;
                return goTemp;
            }
        }

        if (_pools[prefab].Count == 0)
            return null;

        var go = _pools[prefab].Dequeue();
        SetupSpawn(go, pos, rot, parentOverride);
        return go;
    }

    public void Release(GameObject instance)
    {
        if (instance == null) return;

        if (_activeBullets.Contains(instance))
            _activeBullets.Remove(instance);

        if (!_instanceToPrefab.TryGetValue(instance, out var prefabKey))
        {
            instance.SetActive(false);
            return;
        }

        instance.SetActive(false);
        _pools[prefabKey].Enqueue(instance);
    }

    private GameObject CreateNew(GameObject prefab, Transform parent)
    {
        var go = Instantiate(prefab, parent);
        _instanceToPrefab[go] = prefab;

        if (!go.TryGetComponent(out PooledBullet _))
            go.AddComponent<PooledBullet>().prefabKey = prefab;

        return go;
    }

    private readonly HashSet<GameObject> _activeBullets = new HashSet<GameObject>();
    private void SetupSpawn(GameObject go, Vector3 pos, Quaternion rot, Transform parentOverride)
    {
        var t = go.transform;
        if (parentOverride != null && t.parent != parentOverride) t.SetParent(parentOverride);
        t.SetPositionAndRotation(pos, rot);

        var rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d)
        {
            rb2d.velocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }

        _activeBullets.Add(go);
        go.SetActive(true);
    }
}

public class PooledBullet : MonoBehaviour
{
    public GameObject prefabKey;
}
