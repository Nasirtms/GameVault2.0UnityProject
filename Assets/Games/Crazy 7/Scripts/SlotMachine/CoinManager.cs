using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public GameObject coinPrefab;
    public Transform coinParent;
    public Transform burstCenter;      // where coins spawn from (slot center)
    public Transform targetPoint1;     // intermediate pooling point
    public Transform targetPoint2;     // final coin counter

    public int coinAmount = 15;
    public float burstRadius = 1f;
    public float burstHeight = 1f;
    public Ease coinEase = Ease.InCubic;

    public float durationToTarget1 = 0.2f;
    public float durationToTarget2 = 0.4f;
    public float target1Radius = 0.5f; // 🎯 adjustable

    // --- runtime control ---
    private readonly List<Coroutine> _activeCoroutines = new();
    private readonly HashSet<GameObject> _activeCoins = new();
    private bool _cancelRequested = false;
    private object _tweenId; // DOTween kill handle

    void Awake()
    {
        _tweenId = this;
    }

    [ContextMenu("Burst Coins")]
    public void BurstCoins()
    {
        if (CrazySevenSlotMachine.Instance != null && CrazySevenSlotMachine.Instance.InSpin)
        {
            StopBurstCoins();
            return;
        }

        _cancelRequested = false;

        for (int i = 0; i < coinAmount; i++)
        {
            float delay = i * 0.02f;
            var c = StartCoroutine(SpawnCoinWithTwoStepMovement(delay));
            _activeCoroutines.Add(c);
        }
    }

    public void StopBurstCoins()
    {
        _cancelRequested = true;
        StopCoinCounterText();
        // Kill all tweens we created
        DOTween.Kill(_tweenId, complete: false);

        // Stop all coroutines started by this manager
        foreach (var c in _activeCoroutines)
            if (c != null) StopCoroutine(c);
        _activeCoroutines.Clear();

        // Destroy all active coins
        foreach (var coin in _activeCoins)
            if (coin != null) Destroy(coin);
        _activeCoins.Clear();
    }
    public void StopCoinCounterText()
    {
        if (CrazySevenUIManager.Instance != null)
        {
            CrazySevenUIManager.Instance.StopCoinCounter = true;
        }
    }
    private IEnumerator SpawnCoinWithTwoStepMovement(float delay)
    {
        // early exit if cancel arrives before spawn
        if (_cancelRequested) yield break;

        yield return new WaitForSeconds(delay);

        if (_cancelRequested) yield break;

        GameObject coin = Instantiate(coinPrefab, coinParent);
        _activeCoins.Add(coin);

        coin.transform.localScale = Vector3.one * 0.015f;

        // 🟢 Step 1: spawn around burstCenter
        Vector2 spawnOffset2D = Random.insideUnitCircle * burstRadius;
        Vector3 spawnPos = burstCenter.position + new Vector3(spawnOffset2D.x, spawnOffset2D.y, 0f);
        coin.transform.position = spawnPos;

        // 🔵 Step 2: move to a random point near targetPoint1
        Vector2 targetOffset2D = Random.insideUnitCircle * target1Radius;
        Vector3 target1WithRadius = targetPoint1.position + new Vector3(targetOffset2D.x, targetOffset2D.y, 0f);

        // Step 1
        Tween t1 = coin.transform
            .DOMove(target1WithRadius, durationToTarget1)
            .SetEase(Ease.OutSine)
            .SetId(_tweenId);

        yield return t1.WaitForCompletion();
        if (_cancelRequested) { CleanupCoin(coin); yield break; }

        // Step 2.5
        Sequence poolSequence = DOTween.Sequence().SetId(_tweenId);
        for (int j = 0; j < 3; j++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f),
                0f
            );
            poolSequence.Append(coin.transform.DOMove(target1WithRadius + randomOffset, 0.02f).SetEase(Ease.InOutSine));
        }
        poolSequence.Append(coin.transform.DOMove(target1WithRadius, 0.02f).SetEase(Ease.InOutSine));

        yield return poolSequence.WaitForCompletion();
        if (_cancelRequested) { CleanupCoin(coin); yield break; }

        // Step 3
        Sequence finalSequence = DOTween.Sequence().SetId(_tweenId);
        finalSequence.Append(coin.transform.DOMove(targetPoint2.position, durationToTarget2).SetEase(Ease.InCubic));
        finalSequence.Join(coin.transform.DOScale(0.005f, durationToTarget2).SetEase(Ease.InCubic));

        // one OnComplete only
        finalSequence.OnComplete(() => CleanupCoin(coin));

        // wait instead of a second OnComplete
        yield return finalSequence.WaitForCompletion();


        bool finalDone = false;
        finalSequence.OnComplete(() => { CleanupCoin(coin); finalDone = true; });
        while (!finalDone && !_cancelRequested) yield return null;

    }

    private void CleanupCoin(GameObject coin)
    {
        if (coin == null) return;

        // kill tweens bound to this coin transform (by ID kill already covers most);
        coin.transform.DOKill(false);

        if (_activeCoins.Contains(coin))
            _activeCoins.Remove(coin);

        Destroy(coin);
    }

    private void OnDisable()
    {
        StopBurstCoins();
    }
}
