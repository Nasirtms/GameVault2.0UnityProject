using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAnimationScript : MonoBehaviour
{
    public enum PlayType
    {
        DirectState,
        SetBool,
        SetTrigger
    }

    public string animStateOrParam;
    public PlayType playType;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (playType == PlayType.DirectState)
            animator.SetBool(animStateOrParam, true);
        else if (playType == PlayType.SetBool)
            animator.SetBool(animStateOrParam, true);
        else if (playType == PlayType.SetTrigger)
            animator.SetTrigger(animStateOrParam);
    }
}
