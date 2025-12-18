using System.Collections.Generic;
using UnityEngine;

public class AnimationLooper : MonoBehaviour
{
    public Animator animator;
    public string animationTriggerName = "Play1"; // Set this to the name of your animation trigger

    void Start()
    {
        // Start repeating the PlayAnimation method every 5 seconds, after an initial delay of 0 seconds
        InvokeRepeating(nameof(PlayAnimation), 0f, 5f);
    }

    void PlayAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(animationTriggerName);
        }
    }
}






