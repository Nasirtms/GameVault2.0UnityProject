using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class SuperBombFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static SuperBombFreeGameTransitionController Instance;

    [SerializeField] public GameObject[] reSpinBg;
    [SerializeField] public float fadeDuration = 0.5f;

    private SuperBombFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<SuperBombFreeSpinController>();   
    }

    #endregion

    #region Public References

    [ContextMenu("Start")]
    public void StartFreeSpinTransition()
    {
        if (!SuperBombSlotMachine.Instance.isFreeSpinWhenNoPayline)
        {
            SuperBombSlotMachine.Instance.isFreeGameReady = false;
        }
        SuperBombSlotMachine.Instance.isFreeGame = true;
        StartCoroutine(StartFreeSpin());
    }

    [ContextMenu("End")]
    public void EndFreeSpinTransition()
    {
        Debug.Log(" End free spin transition");
        StartCoroutine(EndFreeSpin());
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        Debug.Log(" Deepak from free spin transition controller updating free spins: " + freeSpins);
        freeSpinController.UpdateFreeSpins(freeSpins);
    }

    #endregion

    #region Game Transition

    public IEnumerator ConvertRellsToWild(int reelNumber)
    {
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(SuperBombPaylineController.Instance.ConvertTriggeredReelsToWild());
        yield return new WaitForSeconds(0.3f);


        for (int i = 0; i < reelNumber; i++)
        {
            if (SuperBombSlotMachine.Instance.triggerReelsMask[1])
            {
                reSpinBg[0].SetActive(true);
                var img = reSpinBg[0].transform.GetComponent<Image>();
                var anim = reSpinBg[0].transform.GetChild(0).GetComponent<Animator>();
                if(anim != null)
                {
                    anim.SetBool("Fade_In", true);
                }
                img.DOFade(1, fadeDuration).SetEase(Ease.Linear);
            }

            if (SuperBombSlotMachine.Instance.triggerReelsMask[2])
            {
                reSpinBg[1].SetActive(true);
                var img = reSpinBg[1].transform.GetComponent<Image>();
                var anim = reSpinBg[1].transform.GetChild(0).GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetBool("Fade_In", true);
                }
                img.DOFade(1, fadeDuration).SetEase(Ease.Linear);
                img.DOFade(1, fadeDuration).SetEase(Ease.Linear);
            }

            if (SuperBombSlotMachine.Instance.triggerReelsMask[3])
            {
                reSpinBg[2].SetActive(true);
                var img = reSpinBg[2].transform.GetComponent<Image>();
                var anim = reSpinBg[2].transform.GetChild(0).GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetBool("Fade_In", true);
                }
                img.DOFade(1, fadeDuration).SetEase(Ease.Linear);
                img.DOFade(1, fadeDuration).SetEase(Ease.Linear);
            }
            yield return new WaitForSeconds(fadeDuration);
        }
    }

    private IEnumerator StartFreeSpin() 
    {
        yield return new WaitUntil(() => SuperBombSlotMachine.Instance.isPaylineCompleted);
        SuperBombPaylineController.Instance.ClearPaylineData();
        yield return new WaitForSeconds(0.5f);

        SuperBombUIManager.Instance.UpdateButtons("Free Spin");
        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log(" Ending Free Spins 1");
        foreach (var bgs in reSpinBg)
        {
            var img = bgs.transform.GetComponent<Image>();
            var anim = reSpinBg[0].transform.GetChild(0).GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("Fade_Out", true);
            }
            img.DOFade(1, fadeDuration).SetEase(Ease.Linear);
            img.DOFade(0, fadeDuration).SetEase(Ease.Linear);
            yield return new WaitForSeconds(fadeDuration);
            bgs.SetActive(false);
        }
        Debug.Log(" Ending Free Spins ");

        SuperBombUIManager.Instance.UpdateButtons("Base Game Transition");
        freeSpinController.ResetFreeSpins();
        SuperBombSlotMachine.Instance.ResetFreeGameLocks();
    }

    public void HighlightNewWildReel(int reel)
    {
        StartCoroutine(HighlightReelCo(reel));
    }

    private IEnumerator HighlightReelCo(int reel)
    {
        yield return new WaitUntil(() => SuperBombSlotMachine.Instance.isPaylineCompleted);

        // convert after paylines, before showing the extra BG
        SuperBombPaylineController.Instance.ConvertTriggeredReelsToWild();

        // map 2/3/4 → 0/1/2 like StartFreeSpinTransition
        int idx = reel == 2 ? 0 : (reel == 3 ? 1 : 2);
        reSpinBg[idx].SetActive(true);
        var img = reSpinBg[idx].transform.GetComponent<Image>();
        img.DOFade(1, fadeDuration).SetEase(Ease.Linear);
    }

    #endregion
}
