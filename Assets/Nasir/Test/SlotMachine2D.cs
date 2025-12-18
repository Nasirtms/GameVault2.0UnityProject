using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpriteRenderer-based 2D slot machine (no Canvas dependency).
/// Attach to a GameObject named "SlotMachine".
/// Each Reel has a parent Transform that moves vertically.
/// Symbols are SpriteRenderers instantiated under that parent and cycled while spinning.
/// </summary>
public class SlotMachine2D : MonoBehaviour
{
    [Serializable]
    public class Reel
    {
        [Tooltip("The parent Transform that scrolls up/down (e.g., an empty GameObject in the scene).")]
        public Transform reelRoot;

        [Tooltip("Prefab with a SpriteRenderer component (one symbol cell).")]
        public GameObject symbolPrefab;

        [Tooltip("List of available symbols this reel can cycle through.")]
        public List<Sprite> symbols = new List<Sprite>();

        [Tooltip("How many symbols are visible at once (typically 3).")]
        public int visibleCount = 3;

        [Tooltip("World units distance between symbol centers vertically.")]
        public float symbolSpacing = 1.0f;

        [Tooltip("Extra pooled cells to make scrolling seamless (>=2 recommended).")]
        public int bufferCells = 2;

        // Runtime
        [NonSerialized] public List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        [NonSerialized] public int feedHeadIndex = 0; // next symbol index to feed at the top when scrolling
        [NonSerialized] public float scrollOffset = 0f; // reelRoot local Y offset ([-symbolSpacing, 0))
        [NonSerialized] public bool isSpinning = false;
        [NonSerialized] public bool isStopping = false;
        [NonSerialized] public int targetSymbolIndex = -1; // index in `symbols` that must land on the center row
        [NonSerialized] public Coroutine runner;
        [NonSerialized] public float speed = 0f;
    }

    [Header("Reels")]
    public List<Reel> reels = new List<Reel>();

    [Header("Spin Tuning")]
    [Tooltip("Initial upward acceleration during spin-up (units/sec^2).")]
    public float acceleration = 20f;

    [Tooltip("Maximum scroll speed (units/sec).")]
    public float maxSpeed = 25f;

    [Tooltip("Minimum cruising speed before stop is allowed (units/sec).")]
    public float minStopSpeed = 4f;

    [Tooltip("Deceleration used while stopping (units/sec^2).")]
    public float deceleration = 35f;

    [Tooltip("Delay between stopping consecutive reels (seconds).")]
    public float stopDelayBetweenReels = 0.25f;

    [Tooltip("Optional ease at the very end to softly clamp onto the exact boundary.")]
    public AnimationCurve finalEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Start/Stop Options")]
    [Tooltip("If true, all reels start spinning together. If false, the first starts and others chain automatically.")]
    public bool startAllAtOnce = true;

    [Tooltip("If false, reels accelerate then cruise; if true, they cruise at max as soon as possible.")]
    public bool quickToMax = true;

    // Internal
    private bool _inSpin = false;

    // Optional: if you want to set results first then call StopSpin() without params.
    private int[] _pendingResults;

    // ---------------------- Public API ----------------------

    /// <summary>
    /// Call once on load or from inspector via context menu.
    /// Builds the visible symbol cells for each reel.
    /// </summary>
    [ContextMenu("Rebuild Reels")]
    public void BuildReels()
    {
        if (AnyReelActive())
        {
            Debug.LogWarning("Cannot rebuild while reels are spinning. Stop them first.");
            return;
        }

        foreach (var r in reels)
        {
            //if (!ValidateReelConfig(r)) continue;
            BuildReelVisuals(r);
        }
    }



    /// <summary>
    /// Begin spinning all reels.
    /// </summary>
    public void StartSpin()
    {
        if (_inSpin) return;
        _inSpin = true;

        // Start each reel (together or chain)
        if (startAllAtOnce)
        {
            for (int i = 0; i < reels.Count; i++)
                StartReel(reels[i]);
        }
        else
        {
            StartCoroutine(StartReelsChained());
        }
    }

    /// <summary>
    /// Provide a result array and stop reels in sequence to land on those symbols.
    /// resultIndices: length == reels.Count; each value is an index into Reel.symbols
    /// representing the symbol that must end up on the middle (visibleCount/2) cell.
    /// </summary>
    public void StopSpin(int[] resultIndices)
    {
        if (!_inSpin || resultIndices == null || resultIndices.Length != reels.Count)
        {
            Debug.LogWarning("StopSpin: invalid state or results array.");
            return;
        }
        _pendingResults = (int[])resultIndices.Clone();
        StartCoroutine(StopReelsInOrder());
    }

    /// <summary>
    /// Alternative: set results now, then later call StopSpin() with no params.
    /// </summary>
    public void SetPendingResult(int[] resultIndices)
    {
        if (resultIndices == null || resultIndices.Length != reels.Count)
        {
            Debug.LogWarning("SetPendingResult: invalid results array.");
            return;
        }
        _pendingResults = (int[])resultIndices.Clone();
    }

    /// <summary>
    /// When results were set via SetPendingResult earlier, call this to stop.
    /// </summary>
    public void StopSpin()
    {
        if (_pendingResults == null)
        {
            Debug.LogWarning("StopSpin(): no pending results set.");
            return;
        }
        StopSpin(_pendingResults);
    }

    // ---------------------- Setup Helpers ----------------------
    private void BuildReelVisuals(Reel r)
    {
        if (r.reelRoot == null || r.symbolPrefab == null)
        {
            Debug.LogError("Reel missing reelRoot or symbolPrefab.");
            return;
        }

        // Ensure no coroutine is touching this reel while we rebuild
        KillReelCoroutine(r);

        // Clear previous children safely
        for (int i = r.reelRoot.childCount - 1; i >= 0; i--)
        {
            var child = r.reelRoot.GetChild(i);
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }

        r.renderers.Clear();

        int cellCount = Mathf.Max(1, r.visibleCount) + Mathf.Max(2, r.bufferCells);
        for (int i = 0; i < cellCount; i++)
        {
            var go = Instantiate(r.symbolPrefab, r.reelRoot);
            go.name = $"SymbolCell_{i}";
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError("symbolPrefab must have a SpriteRenderer.");
                return;
            }

            float y = (r.visibleCount / 2f) * r.symbolSpacing - i * r.symbolSpacing;
            go.transform.localPosition = new Vector3(0f, y, 0f);
            r.renderers.Add(sr);
        }

        r.feedHeadIndex = 0;
        for (int i = 0; i < r.renderers.Count; i++)
        {
            r.renderers[i].sprite = r.symbols.Count == 0 ? null : r.symbols[(r.feedHeadIndex + i) % Mathf.Max(1, r.symbols.Count)];
        }

        r.scrollOffset = 0f;
        r.isSpinning = false;
        r.isStopping = false;
        r.speed = 0f;
        r.targetSymbolIndex = -1;
    }

    private void StartReel(Reel r)
    {
        if (r.runner != null) StopCoroutine(r.runner);
        r.isSpinning = true;
        r.isStopping = false;
        r.targetSymbolIndex = -1;
        r.speed = 0f;
        r.runner = StartCoroutine(SpinRoutine(r));
    }

    private IEnumerator StartReelsChained()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            StartReel(reels[i]);
            yield return new WaitForSeconds(stopDelayBetweenReels * 0.75f); // small lead-in between starts
        }
    }

    // ---------------------- Spinning Logic ----------------------

    private IEnumerator SpinRoutine(Reel r)
    {
        // Spin up
        while (r.isSpinning && !r.isStopping)
        {
            // accelerate
            if (quickToMax)
                r.speed = Mathf.MoveTowards(r.speed, maxSpeed, acceleration * Time.deltaTime);
            else
                r.speed = Mathf.Min(maxSpeed, r.speed + acceleration * Time.deltaTime);

            Scroll(r, r.speed * Time.deltaTime);
            yield return null;
        }

        // When Stop phase is requested for this reel, we decelerate until safe to land:
        // "safe" = the next symbol center-crossing matches the target, and we're slow enough.
        while (r.isStopping)
        {
            r.speed = Mathf.Max(minStopSpeed, r.speed - deceleration * Time.deltaTime);
            Scroll(r, r.speed * Time.deltaTime);

            // If the next boundary cross would put the target in the middle row, and we're slow,
            // we perform a short, eased clamp to the exact boundary and finish.
            if (CanClampOnTarget(r))
            {
                yield return ClampToBoundary(r);
                r.isStopping = false;
                r.isSpinning = false;
                r.speed = 0f;
            }

            yield return null;
        }
    }

    // Move down by delta, and when passing one symbolSpacing step, recycle the bottom cell to top and feed next symbol.
    private void Scroll(Reel r, float delta)
    {
        // Basic guards
        if (r == null || r.reelRoot == null || r.renderers == null || r.renderers.Count == 0) return;
        if (r.symbolSpacing <= 0f) return;

        // Prune destroyed cells (prevents MissingReferenceException)
        for (int i = r.renderers.Count - 1; i >= 0; i--)
        {
            if (r.renderers[i] == null)
                r.renderers.RemoveAt(i);
        }
        if (r.renderers.Count == 0) return;

        r.scrollOffset -= delta;

        if (float.IsNaN(r.scrollOffset) || float.IsInfinity(r.scrollOffset))
            r.scrollOffset = 0f;

        int safety = 0;
        while (r.scrollOffset <= -r.symbolSpacing && safety++ < 128)
        {
            r.scrollOffset += r.symbolSpacing;

            int lastIdx = r.renderers.Count - 1;
            if (lastIdx < 0) break;

            var bottomSR = r.renderers[lastIdx];
            if (bottomSR == null) { r.renderers.RemoveAt(lastIdx); if (r.renderers.Count == 0) break; continue; }

            var bottom = bottomSR.transform;
            float topY = r.renderers[0].transform.localPosition.y + r.symbolSpacing;

            // Move bottom cell to the top
            bottom.SetSiblingIndex(0);
            bottom.localPosition = new Vector3(0f, topY, 0f);

            // Rotate list
            r.renderers.RemoveAt(lastIdx);
            r.renderers.Insert(0, bottomSR);

            // Feed next symbol
            if (r.symbols.Count > 0 && r.renderers[0] != null)
            {
                r.feedHeadIndex = Mod(r.feedHeadIndex - 1, r.symbols.Count);
                r.renderers[0].sprite = r.symbols[r.feedHeadIndex];
            }

            if (r.renderers.Count == 0) break;
        }

        var pos = r.reelRoot.localPosition;
        if (float.IsNaN(r.scrollOffset) || float.IsInfinity(r.scrollOffset)) r.scrollOffset = 0f;
        r.reelRoot.localPosition = new Vector3(pos.x, r.scrollOffset, pos.z);
    }


    private void OnDisable()
    {
        StopAllReelsImmediate();
    }

    private void OnDestroy()
    {
        StopAllReelsImmediate();
    }

    // Is the symbol currently at the middle row equal to the target?
    private bool CenterIsTarget(Reel r)
    {
        if (r.symbols.Count == 0) return false;
        int centerCell = Mathf.FloorToInt(r.visibleCount / 2f);
        var centerSprite = r.renderers[centerCell].sprite;
        if (centerSprite == null) return false;
        int idx = r.symbols.IndexOf(centerSprite);
        return idx == r.targetSymbolIndex;
    }

    // --- Lifecycle helpers ---
    private void KillReelCoroutine(Reel r)
    {
        if (r.runner != null)
        {
            try { StopCoroutine(r.runner); } catch { /* ignore */ }
            r.runner = null;
        }
        r.isSpinning = false;
        r.isStopping = false;
        r.speed = 0f;
    }

    private void StopAllReelsImmediate()
    {
        foreach (var r in reels)
        {
            KillReelCoroutine(r);
        }
    }

    // Optional: block rebuild while spinning (prevents mid-spin rebuilds)
    private bool AnyReelActive()
    {
        foreach (var r in reels)
        {
            if (r.isSpinning || r.isStopping || r.runner != null) return true;
        }
        return false;
    }


    // We only clamp at exact row boundaries (scrollOffset ~ 0).
    private bool CanClampOnTarget(Reel r)
    {
        // Near a boundary?
        bool nearBoundary = Mathf.Abs(r.scrollOffset) <= 0.025f;
        return nearBoundary && r.speed <= (minStopSpeed + 0.01f) && CenterIsTarget(r);
    }

    private IEnumerator ClampToBoundary(Reel r)
    {
        if (float.IsNaN(r.scrollOffset) || float.IsInfinity(r.scrollOffset))
            r.scrollOffset = 0f;
        // Smoothly push the offset to exactly 0 using a short ease
        float start = r.scrollOffset;
        float dur = 0.08f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float eased = finalEase.Evaluate(Mathf.Clamp01(t));
            r.scrollOffset = Mathf.Lerp(start, 0f, eased);
            r.reelRoot.localPosition = new Vector3(r.reelRoot.localPosition.x, r.scrollOffset, r.reelRoot.localPosition.z);
            yield return null;
        }
        r.scrollOffset = 0f;
        r.reelRoot.localPosition = new Vector3(r.reelRoot.localPosition.x, 0f, r.reelRoot.localPosition.z);
    }

    // ---------------------- Stop sequence ----------------------

    private IEnumerator StopReelsInOrder()
    {
        // Assign targets and request stop for each reel with a stagger
        for (int i = 0; i < reels.Count; i++)
        {
            var r = reels[i];
            r.targetSymbolIndex = Mathf.Clamp(_pendingResults[i], 0, Mathf.Max(0, r.symbols.Count - 1));
            r.isStopping = true; // the running coroutine will switch into the stopping loop

            // Wait a bit before requesting stop on the next reel
            if (i < reels.Count - 1)
                yield return new WaitForSeconds(stopDelayBetweenReels);
        }

        // Wait until all reels have fully stopped
        bool any;
        do
        {
            any = false;
            foreach (var r in reels)
                any |= r.isSpinning || r.isStopping;
            yield return null;
        } while (any);

        _inSpin = false;
        // TODO: trigger win evaluation / paylines / effects here.
    }

    // ---------------------- Utility ----------------------

    private static int Mod(int x, int m)
    {
        if (m == 0) return 0;
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    // ---------------------- Editor Helpers / Context Menus ----------------------
#if UNITY_EDITOR
    [ContextMenu("Start Spin (Go)")]
    private void CM_StartSpin()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to spin."); return; }
#endif
        if (AnyReelActive()) { Debug.LogWarning("Already spinning."); return; }
        // Assume reels are already built; or call BuildReels() once before play.
        StartSpin();
    }


    [ContextMenu("Stop Spin (Use Pending Result)")]
    private void CM_StopSpin_UsePending()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode to stop.");
            return;
        }
        StopSpin(); // uses SetPendingResult(...) if set earlier
    }

    [ContextMenu("Stop Spin (Random)")]
    private void CM_StopSpin_Random()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode to stop.");
            return;
        }

        if (reels == null || reels.Count == 0)
        {
            Debug.LogWarning("No reels configured.");
            return;
        }

        var results = new int[reels.Count];
        for (int i = 0; i < reels.Count; i++)
        {
            int count = Mathf.Max(1, reels[i].symbols.Count);
            results[i] = UnityEngine.Random.Range(0, count);
        }
        StopSpin(results);
    }

    [ContextMenu("Start + Auto Stop (Random, 1.2s delay)")]
    private void CM_StartThenAutoStop()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode to spin.");
            return;
        }
        BuildReels();
        StartSpin();
        StartCoroutine(_AutoStopRandomAfter(1.2f));
    }

    private IEnumerator _AutoStopRandomAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        var results = new int[reels.Count];
        for (int i = 0; i < reels.Count; i++)
        {
            int count = Mathf.Max(1, reels[i].symbols.Count);
            results[i] = UnityEngine.Random.Range(0, count);
        }
        StopSpin(results);
    }

    // Existing quick tester from earlier file:
    [ContextMenu("Test Spin 3 Reels")]
    private void TestSpin()
    {
        BuildReels();
        StartSpin();
        // Simulate a result: pick middle index for each reel after 1.5s, then stop.
        StartCoroutine(EditorTestStop());
    }
    private bool ValidateReelConfig(Reel r)
    {
        if (r.reelRoot == null)
        {
            Debug.LogError("Reel has no reelRoot assigned.");
            return false;
        }
        if (r.symbolPrefab == null || r.symbolPrefab.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogError("symbolPrefab must be assigned and contain a SpriteRenderer.");
            return false;
        }
        if (r.symbolSpacing <= 0f)
        {
            Debug.LogError($"symbolSpacing must be > 0 on reel '{r.reelRoot.name}'. Set it to your symbol height in world units.");
            return false;
        }
        if (r.visibleCount < 1) r.visibleCount = 1;
        if (r.bufferCells < 2) r.bufferCells = 2;
        return true;
    }

    private IEnumerator EditorTestStop()
    {
        yield return new WaitForSeconds(1.5f);
        var results = new int[reels.Count];
        for (int i = 0; i < results.Length; i++)
        {
            results[i] = reels[i].symbols.Count > 0 ? UnityEngine.Random.Range(0, reels[i].symbols.Count) : 0;
        }
        StopSpin(results);
    }
#endif
}
