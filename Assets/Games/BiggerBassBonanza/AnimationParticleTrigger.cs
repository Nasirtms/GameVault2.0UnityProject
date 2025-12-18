using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class AnimationParticleTrigger : MonoBehaviour
{
    public List<ParticleSystem> particleEffect = new List<ParticleSystem>();
    // This method will be called from an Animation Event
    public void PlayParticle()
    {
        for (int i = 0; i < particleEffect.Count; i++)
        {
            if (particleEffect[i] != null)
            {
                particleEffect[i].Play();
            }
        }
    }
    // Optional: Stop particles (if needed in animation)
    public void StopParticle()
    {
        for (int i = 0; i < particleEffect.Count; i++)
        {
            if (particleEffect[i] != null)
            {
                particleEffect[i].Stop();
            }
        }
    }
}