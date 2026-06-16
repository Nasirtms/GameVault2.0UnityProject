using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SpinWheelManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform SpinwheelParent;
    [SerializeField] private GameObject SpinWheelPrefab;
    [SerializeField] private Button SpinBtn;
    [SerializeField] private bool isGameHaveSpinWheel = false;
    [Header("Config")]
    [SerializeField] private int forcedPrizeIndex = 0;
    [SerializeField] private int cooldownSeconds = 3600;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] public bool canFreeSpin;
    private const string NextAvailableTicksKey = "FreeSpinNextUtcTicks";
    private Coroutine waitCo;
    private DateTime nextAvailableUtc;

    [SerializeField] private ParticleSystem winParticlesPrefab;   // assign a prefab in Inspector
    [SerializeField] private Transform particlesParent;           // optional (defaults to wheel parent if null)
    [SerializeField] private float particleLifetime = 3f;
    [SerializeField] private float particleStartDelay = 0.0f;

    private bool winFXPlayedThisSpin = false;

    private void Start()
    {
        isGameHaveSpinWheel = SceneManagement.isShowSpinWheel;
        if (SpinBtn != null)
        {
            SpinBtn.onClick.AddListener(OnSpinClicked);
        }
        RefreshAvailabilityFromSavedTime();
    }
    private void OnDestroy()
    {
        if (SpinBtn != null) SpinBtn.onClick.RemoveListener(OnSpinClicked);
    }
    private void OnSpinClicked()
    {
        if (!isGameHaveSpinWheel || !canFreeSpin) return;
        MainMenuUIManager.Instance?.ToggleMenuButtonsUI(false);
        winFXPlayedThisSpin = true;
        // Instantiate spin wheel
        var obj = Instantiate(SpinWheelPrefab, SpinwheelParent);
        var wheel = obj.GetComponent<SpinWheel>();
        if (wheel != null)
        {
            wheel.forcedPrizeIndex = forcedPrizeIndex;
            wheel.OnSpinFinished += HandleSpinFinished;
        }
        else
        {
            // No wheel -> nothing started, keep canFreeSpin = true
            Debug.LogWarning("SpinWheel component not found, spin not started. canFreeSpin stays true.");
        }
    }
    private void HandleSpinFinished()
    {
        var wheel = GetComponentInChildren<SpinWheel>();
        if (wheel != null) wheel.OnSpinFinished -= HandleSpinFinished;
        if (!winFXPlayedThisSpin)
        {
            winFXPlayedThisSpin = true;
            StartCoroutine(PlayWinParticlesCo());
        }
        //BeginCooldown();
        RefreshAvailabilityFromSavedTime();
    }
    private IEnumerator PlayWinParticlesCo()
    {
        if (particleStartDelay > 0f) yield return new WaitForSeconds(particleStartDelay);
        PlayWinParticles();
    }
    private void PlayWinParticles()
    {
        if (winParticlesPrefab == null || SpinwheelParent == null) return;

        Transform parent = particlesParent != null ? particlesParent : SpinwheelParent;

        // Instantiate
        ParticleSystem ps = Instantiate(winParticlesPrefab, SpinwheelParent.position, Quaternion.identity, parent);

        // 🔹 Force stop immediately so we can edit safely
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Configure main module
        var main = ps.main;
        main.playOnAwake = false;

        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
            main.startLifetime = Mathf.Min(0.4f, main.startLifetime.constant); // ~0.3–0.4s

        // 🔹 Now it's safe to set duration
        main.duration = Mathf.Min(0.35f, main.duration);
        main.stopAction = ParticleSystemStopAction.Destroy;

        // Fast fade-out using Color over Lifetime
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 0.6f),
            new GradientAlphaKey(0f, 1f) // fade to 0 alpha by end
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // 🔹 Now play cleanly
        ps.Play();

        // Cleanup
        Destroy(ps.gameObject, particleLifetime);
    }

    private void RefreshAvailabilityFromSavedTime()
    {
        canFreeSpin = true;
        long savedTicks;
        if (long.TryParse(PlayerPrefs.GetString(NextAvailableTicksKey, "0"), out savedTicks) && savedTicks > 0)
        {
            nextAvailableUtc = new DateTime(savedTicks, DateTimeKind.Utc);
            var nowUtc = DateTime.UtcNow;
            if (nowUtc < nextAvailableUtc)
            {
                canFreeSpin = false;
                ArmReenableAfter((float)(nextAvailableUtc - nowUtc).TotalSeconds);
            }
            else
            {
                ClearSavedNextSpinTime();
            }
        }
        UpdateCooldownUI();
    }

    public static void ClearSavedNextSpinTime()
    {
        PlayerPrefs.DeleteKey(NextAvailableTicksKey);
        PlayerPrefs.Save();
    }

    public static void SaveNextSpinTimeUtc(DateTime nextSpinUtc)
    {
        nextSpinUtc = nextSpinUtc.Kind == DateTimeKind.Utc
            ? nextSpinUtc
            : DateTime.SpecifyKind(nextSpinUtc.ToUniversalTime(), DateTimeKind.Utc);

        if (nextSpinUtc <= DateTime.UtcNow)
        {
            ClearSavedNextSpinTime();
            return;
        }

        PlayerPrefs.SetString(NextAvailableTicksKey, nextSpinUtc.Ticks.ToString());
        PlayerPrefs.Save();
    }

    //private void BeginCooldown()
    //{
    //    canFreeSpin = false;
    //    nextAvailableUtc = DateTime.UtcNow.AddSeconds(cooldownSeconds);
    //    PlayerPrefs.SetString(NextAvailableTicksKey, nextAvailableUtc.Ticks.ToString());
    //    PlayerPrefs.Save();
    //    ArmReenableAfter(cooldownSeconds);
    //    UpdateCooldownUI();
    //}
    private void ArmReenableAfter(float seconds)
    {
        if (waitCo != null) StopCoroutine(waitCo);
        waitCo = StartCoroutine(ReenableAfter(seconds));
    }
    private IEnumerator ReenableAfter(float seconds)
    {
        float remaining = seconds;
        while (remaining > 0)
        {
            UpdateCooldownUI();
            yield return new WaitForSeconds(1f);
            remaining = (float)(nextAvailableUtc - DateTime.UtcNow).TotalSeconds;
        }

        //yield return new WaitForSeconds(1f);

        RefreshAvailabilityFromSavedTime();
        //canFreeSpin = true;
        //UpdateCooldownUI();
        //waitCo = null;
    }
    private void UpdateCooldownUI()
    {
        if (cooldownText == null) return;
        if (SceneManagement.isShowSpinWheel)
        {
            if (canFreeSpin)
            {
                cooldownText.gameObject.transform.parent.gameObject.SetActive(false);
                SpinWheelButtonAnimator.Instance.SetActiveState(true);
                if (SpinBtn) SpinBtn.interactable = true;
            }
            else
            {
                cooldownText.gameObject.transform.parent.gameObject.SetActive(true);
                SpinWheelButtonAnimator.Instance.SetActiveState(false);
                TimeSpan remain = nextAvailableUtc - DateTime.UtcNow;
                if (remain.TotalSeconds < 0) remain = TimeSpan.Zero;
                var totalSeconds = (int)Math.Ceiling(remain.TotalSeconds);
                if (totalSeconds >= 3600)
                {
                    // 60+ minutes → hours (e.g. 60 min = 01:00:00, 180 min = 03:00:00)
                    cooldownText.text = string.Format(
                        "{0:D2}:{1:D2}:{2:D2}",
                        totalSeconds / 3600,
                        (totalSeconds % 3600) / 60,
                        totalSeconds % 60);
                }
                else
                {
                    // Under 60 minutes → minutes only (e.g. 5 min = 05:00)
                    cooldownText.text = string.Format(
                        "{0:D2}:{1:D2}",
                        totalSeconds / 60,
                        totalSeconds % 60);
                }
                if (SpinBtn) SpinBtn.interactable = false;
            }
            //Debug.Log("Can Free Spin: " + canFreeSpin);
        }
    }

}