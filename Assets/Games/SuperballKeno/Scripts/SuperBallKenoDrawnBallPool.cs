using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

public class SuperBallKenoDrawnBallPool : MonoBehaviour
{
    [SerializeField] private GameObject drawnBallPrefab;
    [SerializeField] private Transform drawnBallContainer;
    [SerializeField] private int initialSize = 24;
    [SerializeField] private int maxSize = 48;

    private UnityEngine.Pool.ObjectPool<GameObject> pool;
    private Transform bin;

    void Awake()
    {
        bin = new GameObject("DrawnBallPoolBin").transform;
        bin.SetParent(drawnBallContainer ? drawnBallContainer.parent : null, false);
        bin.gameObject.SetActive(false);

        pool = new UnityEngine.Pool.ObjectPool<GameObject>(
            createFunc: () =>
            {
                var go = Instantiate(drawnBallPrefab, bin);
                go.SetActive(false);
                return go;
            },
            actionOnGet: go =>
            {
                go.SetActive(true);
                var t = go.transform;
                t.SetParent(drawnBallContainer ? drawnBallContainer.parent : null, false);
                t.localScale = Vector3.one;
                t.localRotation = Quaternion.identity;
                t.DOKill(true);

                var anim = go.GetComponent<Animator>();
                if (anim) anim.enabled = false;

                var txt = go.GetComponentInChildren<TMPro.TMP_Text>(true);
            },
            actionOnRelease: go =>
            {
                var t = go.transform;
                t.DOKill(true);
                t.SetParent(bin, false);
                t.localScale = Vector3.one;
                t.localRotation = Quaternion.identity;

                var anim = go.GetComponent<Animator>();
                if (anim) anim.enabled = false;

                go.SetActive(false);
            },
            actionOnDestroy: go => { if (go) Destroy(go); },
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: maxSize
        );

        // prewarm
        var warm = new List<GameObject>(initialSize);
        for (int i = 0; i < initialSize; i++) warm.Add(pool.Get());
        for (int i = 0; i < warm.Count; i++) pool.Release(warm[i]);
    }

    public GameObject SpawnAt(Vector3 worldPos, Transform tweenParent)
    {
        var go = pool.Get();
        var t = go.transform;
        t.SetParent(tweenParent, false); // tweenParent can be null; that’s fine
        t.position = worldPos;
        t.localScale = Vector3.one;
        return go;
    }

    public void FinalizeIntoGrid(GameObject go)
    {
        var t = go.transform;
        t.SetParent(drawnBallContainer, false);
        t.localScale = Vector3.one;

        var rect = drawnBallContainer as RectTransform;
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    public void Release(GameObject go) => pool.Release(go);

    public void ReleaseAllInGrid()
    {
        if (!drawnBallContainer) return;
        var list = new List<Transform>();
        foreach (Transform child in drawnBallContainer) list.Add(child);
        for (int i = 0; i < list.Count; i++) pool.Release(list[i].gameObject);
    }

    void OnDestroy()
    {
        ReleaseAllInGrid();
        if (bin)
        {
            foreach (Transform t in bin) Destroy(t.gameObject);
            Destroy(bin.gameObject);
        }
        pool = null;
    }
}
