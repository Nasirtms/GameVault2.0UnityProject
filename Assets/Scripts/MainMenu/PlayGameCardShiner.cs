using Coffee.UIEffects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayGameCardShiner : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    public string animationTriggerName = "Play";

    public void StartShineAnimation()
    {
        Debug.Log("StartShineAnimation 1");
        PlayShineAnimation();
    }

    void PlayShineAnimation()
    {
        UIShiny uiShiny = transform.parent.GetChild(0).GetChild(0).GetComponent<UIShiny>();
        if (uiShiny != null)
        {
            uiShiny.enabled = true;
        }
        animator.SetBool(animationTriggerName, true);
    }
}
