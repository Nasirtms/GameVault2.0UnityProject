using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
public struct FishTowardJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<float3> destinations;
    [ReadOnly] public NativeArray<float> speeds;
    [ReadOnly] public NativeArray<float3> starts;
    [ReadOnly] public NativeArray<byte> allowRedirect;   // 0/1
    public NativeArray<byte> redirected;      // 0/1 (we set it when we pass halfway)
    public NativeArray<byte> arrived;         // 0/1 (set when reached destination)
    public NativeArray<byte> needsRedirect;   // 0/1 (set when just crossed halfway)
    public float dt;

    public void Execute(int index, TransformAccess tr)
    {
        float3 pos = tr.position;
        float3 dest = destinations[index];
        float spd = speeds[index];

        float3 toDest = dest - pos;
        float dist = math.length(toDest);

        if (dist > 1e-4f)
        {
            float3 dir = toDest / dist;
            float step = spd * dt;

            if (step >= dist)
            {
                pos = dest;
                arrived[index] = 1;
            }
            else
            {
                pos += dir * step;
            }

            // rotate in 2D to face movement (Z-axis up)
            float ang = math.degrees(math.atan2(dir.y, dir.x));
            tr.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        // halfway redirect check
        if (allowRedirect[index] == 1 && redirected[index] == 0)
        {
            float full = math.distance(starts[index], destinations[index]);
            float traveled = math.distance(starts[index], pos);
            if (full > 1e-4f && traveled >= 0.5f * full)
            {
                needsRedirect[index] = 1; // signal main thread to pick a new destination
                redirected[index] = 1;    // make sure we only do this once
            }
        }

        tr.position = pos;
    }
}

public class FishMoverJobs : MonoBehaviour
{
    public static FishMoverJobs Instance { get; private set; }

    // Main-thread mirrors (dynamic, easy to modify)
    readonly List<Transform> _ts = new();
    readonly List<float3> _dest = new();
    readonly List<float3> _start = new();
    readonly List<float> _speed = new();
    readonly List<byte> _allowRedirect = new();
    readonly List<byte> _redirected = new();

    readonly Dictionary<Transform, int> _indexOf = new();

    // Native mirrors (used by jobs)
    TransformAccessArray taa;
    NativeArray<float3> nDest, nStart;
    NativeArray<float> nSpeed;
    NativeArray<byte> nAllowRedirect, nRedirected, nArrived, nNeedsRedirect;

    bool _dirtyArrays = false;

    // Injected from FishManager to resolve redirect destinations
    Func<Vector3, Vector3> _redirectResolver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void SetRedirectResolver(Func<Vector3, Vector3> resolver)
    {
        _redirectResolver = resolver;
    }

    // Register a fish with initial destination/speed and redirect policy
    public void RegisterFish(Transform t, Vector3 start, Vector3 destination, float speed, bool allowRedirect)
    {
        if (!t) return;
        if (_indexOf.ContainsKey(t)) return;

        int idx = _ts.Count;
        _indexOf[t] = idx;
        _ts.Add(t);
        _start.Add(start);
        _dest.Add(destination);
        _speed.Add(speed);
        _allowRedirect.Add(allowRedirect ? (byte)1 : (byte)0);
        _redirected.Add(0);

        _dirtyArrays = true;
    }

    public void DeregisterFish(Transform t)
    {
        if (t == null) return;
        if (!_indexOf.TryGetValue(t, out int idx)) return;
        if (idx < 0 || idx >= _ts.Count) return; // ✅ safety guard

        int last = _ts.Count - 1;
        if (last < 0) return; // ✅ empty list check

        SwapRemove(idx, last);
        _dirtyArrays = true;
    }

    void SwapRemove(int idx, int last)
    {
        if (idx < 0 || last < 0 || idx >= _ts.Count || last >= _ts.Count)
        {
            Debug.LogWarning($"SwapRemove skipped: invalid index idx={idx}, last={last}, count={_ts.Count}");
            return;
        }

        if (idx != last)
        {
            _ts[idx] = _ts[last];
            _start[idx] = _start[last];
            _dest[idx] = _dest[last];
            _speed[idx] = _speed[last];
            _allowRedirect[idx] = _allowRedirect[last];
            _redirected[idx] = _redirected[last];

            _indexOf[_ts[idx]] = idx;
        }

        // remove last safely
        _indexOf.Remove(_ts[last]);
        _ts.RemoveAt(last);
        _start.RemoveAt(last);
        _dest.RemoveAt(last);
        _speed.RemoveAt(last);
        _allowRedirect.RemoveAt(last);
        _redirected.RemoveAt(last);
    }

    // Allow manager to change a destination mid-flight
    public void SetDestination(Transform t, Vector3 newDest)
    {
        if (!_indexOf.TryGetValue(t, out int idx)) return;
        _dest[idx] = newDest;
        _dirtyArrays = true; // cheap route: rebuild this frame
    }

    // Call once per frame
    void Update()
    {
        //if (_ts.Count == 0) return;

        //if (_dirtyArrays) RebuildNativeArrays();

        //var job = new FishTowardJob
        //{
        //    destinations = nDest,
        //    speeds = nSpeed,
        //    starts = nStart,
        //    allowRedirect = nAllowRedirect,
        //    redirected = nRedirected,
        //    arrived = nArrived,
        //    needsRedirect = nNeedsRedirect,
        //    dt = Time.deltaTime
        //};

        //job.Schedule(taa).Complete();

        //// Handle redirects and arrivals on main thread
        //HandleRedirectsAndArrivals();
    }

    void HandleRedirectsAndArrivals()
    {
        // Redirects
        if (_redirectResolver != null)
        {
            for (int i = 0; i < _ts.Count; i++)
            {
                if (nNeedsRedirect[i] == 1)
                {
                    nNeedsRedirect[i] = 0; // consume
                    var tr = _ts[i];
                    if (!tr) continue;

                    Vector3 newDest = _redirectResolver(tr.position);
                    _dest[i] = newDest;  // update managed copy
                    // also reflect in native without rebuild:
                    nDest[i] = newDest;
                }
            }
        }

        // Arrivals
        for (int i = _ts.Count - 1; i >= 0; i--)
        {
            if (nArrived[i] == 1)
            {
                nArrived[i] = 0; // consume
                var tr = _ts[i];
                if (!tr) continue;

                // Let FishManager handle despawn/death. We only deregister.
                FishManager.Instance.HandleArrivedFromMover(tr);
                // FishManager will call DeregisterFish inside its despawn.
            }
        }
    }

    void RebuildNativeArrays()
    {
        // dispose old
        if (taa.isCreated) taa.Dispose();
        DisposeNative(ref nDest);
        DisposeNative(ref nStart);
        DisposeNative(ref nSpeed);
        DisposeNative(ref nAllowRedirect);
        DisposeNative(ref nRedirected);
        DisposeNative(ref nArrived);
        DisposeNative(ref nNeedsRedirect);

        // rebuild
        taa = new TransformAccessArray(_ts.ToArray());
        int n = _ts.Count;

        nDest = new NativeArray<float3>(n, Allocator.Persistent);
        nStart = new NativeArray<float3>(n, Allocator.Persistent);
        nSpeed = new NativeArray<float>(n, Allocator.Persistent);
        nAllowRedirect = new NativeArray<byte>(n, Allocator.Persistent);
        nRedirected = new NativeArray<byte>(n, Allocator.Persistent);
        nArrived = new NativeArray<byte>(n, Allocator.Persistent);
        nNeedsRedirect = new NativeArray<byte>(n, Allocator.Persistent);

        for (int i = 0; i < n; i++)
        {
            nDest[i] = _dest[i];
            nStart[i] = _start[i];
            nSpeed[i] = _speed[i];
            nAllowRedirect[i] = _allowRedirect[i];
            nRedirected[i] = _redirected[i];
            nArrived[i] = 0;
            nNeedsRedirect[i] = 0;
        }

        _dirtyArrays = false;
    }

    static void DisposeNative<T>(ref NativeArray<T> arr) where T : struct
    {
        if (arr.IsCreated) arr.Dispose();
    }

    void OnDestroy()
    {
        if (taa.isCreated) taa.Dispose();
        DisposeNative(ref nDest);
        DisposeNative(ref nStart);
        DisposeNative(ref nSpeed);
        DisposeNative(ref nAllowRedirect);
        DisposeNative(ref nRedirected);
        DisposeNative(ref nArrived);
        DisposeNative(ref nNeedsRedirect);

        if (Instance == this) Instance = null;
    }
}
