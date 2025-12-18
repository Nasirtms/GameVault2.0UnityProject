using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationTimer : MonoBehaviour
{
    [Tooltip("Assign GameObjects that have ParticleSystem components")]
    public GameObject[] particleObjects;

    [Tooltip("Time between particle activations")]
    public float repeatInterval = 3f;

    private int popupCount = 0;
    private int maxPlays = 3;

    void OnEnable()
    {
        popupCount = 0; // Reset count every time it's re-enabled
        InvokeRepeating(nameof(PlayParticles), 0f, repeatInterval);
    }
    
    void OnDisable()
    {
        CancelInvoke(nameof(PlayParticles)); // Ensure no repeats while inactive
    }

    void PlayParticles()
    {
        if (popupCount >= maxPlays)
        {
            CancelInvoke(nameof(PlayParticles));
            gameObject.SetActive(false); // Disable after limit
            return;
        }

        foreach (GameObject obj in particleObjects)
        {
            if (obj != null)
            {
                ParticleSystem ps = obj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }
                else
                {
                    Debug.LogWarning($"GameObject '{obj.name}' does not have a ParticleSystem component.");
                }
            }
        }

        popupCount++;
    }
}
