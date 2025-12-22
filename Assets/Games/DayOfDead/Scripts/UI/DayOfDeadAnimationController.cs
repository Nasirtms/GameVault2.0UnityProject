using System.Collections;
using UnityEngine;

public class DayOfDeadAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    public string smallToBigParam = "WildSmalltoBig";
    public string slotShiftParam = "WildSlotShift";

    public bool smallToBigPlayed;
    public Coroutine smallToBigRoutine;
    public Coroutine slotShiftRoutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void PlaySmallToBigOnce()
    {
        if (animator == null) return;
        if (smallToBigPlayed) return;

        smallToBigPlayed = true;

        if (smallToBigRoutine != null)
            StopCoroutine(smallToBigRoutine);

        smallToBigRoutine = StartCoroutine(PlayBoolOnce(smallToBigParam));
    }

    public void PlaySlotShift()
    {
        if (animator == null) return;

        if (slotShiftRoutine != null)
            StopCoroutine(slotShiftRoutine);

        slotShiftRoutine = StartCoroutine(PlayBoolOnce(slotShiftParam));
    }

    private IEnumerator PlayBoolOnce(string paramName)
    {
        yield return null;
        animator.SetBool(paramName, true);
        if(paramName == slotShiftParam)
        {
            yield return new WaitForEndOfFrame();
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }
        if(animator != null)
            animator.SetBool(paramName, false);
    }
    public void ResetAll()
    {
        if (smallToBigRoutine != null) StopCoroutine(smallToBigRoutine);
        if (slotShiftRoutine != null) StopCoroutine(slotShiftRoutine);

        if (animator != null)
        {
            animator.SetBool(smallToBigParam, false);
            animator.SetBool(slotShiftParam, false);
        }

        smallToBigPlayed = false;
    }
}
