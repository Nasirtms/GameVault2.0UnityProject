using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WipeAnimation : MonoBehaviour
{
    [Header("Object to Move")]
    [SerializeField] private Transform target;

    [Header("Positions (world space)")]
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private SpriteRenderer mainBG;
    [SerializeField] private SpriteRenderer bonusBG;
    [SerializeField] private Sprite[] bonusBgs;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private Ease ease = Ease.Linear;
    [SerializeField] private bool horizontalOnly = false;
    [SerializeField] private bool ignoreTimeScale = true;

    private Tween tween;
    private int wipeCount = 0;
    private int nextIdx;
    public GameObject spriteMaskObj;

    private void Awake()
    {
        if (target != null)
        {
            // Find the SpriteMask child once at startup
            var mask = target.GetComponentInChildren<SpriteMask>(true);
            if (mask != null)
                spriteMaskObj = mask.gameObject;

            // disable everything except mask
            SetTargetChildrenActive(false);
        }
    }
    private void Start()
    {
        nextIdx = (bonusBgs.Length >= 2) ? 2 % bonusBgs.Length : 0;
    }

    [ContextMenu("Test Wipe")]
    private void TestWipe() => PlayWipe();

    public virtual void PlayWipe(System.Action onComplete = null)
    {
        if (!target || !startPos || !endPos)
        {
            Debug.LogWarning("WipeAnimation: Assign Target, Start, End in the inspector.", this);
            return;
        }

        tween?.Kill();

        wipeCount++;

        // Odd = start→end, Even = end→start
        bool isOdd = (wipeCount % 2 == 1);
        Transform from = isOdd ? startPos : endPos;
        Transform to = isOdd ? endPos : startPos;


            // Enable children (except mask stays always active)
            SetTargetChildrenActive(true);
        target.position = from.position;

        var dest = horizontalOnly
            ? new Vector3(to.position.x, target.position.y, target.position.z)
            : to.position;

        tween = target.DOMove(dest, duration)
                      .SetEase(ease)
                      .SetUpdate(ignoreTimeScale)
                      .SetLink(target.gameObject)
                      .OnComplete(() =>
                      {
                          SetTargetChildrenActive(false); // hide children but keep mask
                          onComplete?.Invoke();
                          ChangeBg(wipeCount);
                      });
    }

    private void ChangeBg(int c)
    {
        if (bonusBgs == null || bonusBgs.Length == 0) return;
        if (bonusBgs.Length == 1) { mainBG.sprite = bonusBgs[0]; bonusBG.sprite = bonusBgs[0]; return; }

        if ((c & 1) == 1)
        {
            mainBG.sprite = bonusBgs[nextIdx];
        }
        else 
        {
            bonusBG.sprite = bonusBgs[nextIdx];
        }

        nextIdx = (nextIdx + 1) % bonusBgs.Length;
    }

    private void SetTargetChildrenActive(bool active)
    {
        if (target == null) return;

        foreach (Transform child in target)
        {
            if (spriteMaskObj != null && child.gameObject == spriteMaskObj)
                continue; // skip mask
            child.gameObject.SetActive(active);
        }

        // Ensure mask always stays enabled
        if (spriteMaskObj != null)
            spriteMaskObj.SetActive(true);
    }
}
