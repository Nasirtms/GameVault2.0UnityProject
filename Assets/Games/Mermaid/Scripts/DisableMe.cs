using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableMe : MonoBehaviour
{
    public float delay;
    public bool destroy = false;

    private void OnEnable()
    {
        StopCoroutine("Disable_Coroutine");
        StartCoroutine("Disable_Coroutine");
    }

    IEnumerator Disable_Coroutine()
    {
        yield return new WaitForSeconds(delay);

        if (destroy)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
