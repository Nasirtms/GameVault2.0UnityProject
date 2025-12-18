// ComputerChoiceManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ComputerChoiceManager_RPS : MonoBehaviour
{
    [Header("Your 3 choice images (in R, P, S order)")]
    public List<GameObject> objects = new List<GameObject>();

    [Header("Cycle speeds (in seconds)")]
    [SerializeField] private float normalInterval = 1f;
    [SerializeField] private float fastInterval = 0.1f;

    private Coroutine cycleRoutine;

    private void Awake()
    {
        StartCycle(normalInterval);
    }

    public void StartNormalCycle() => StartCycle(normalInterval);

    public void StartFastCycle() => StartCycle(fastInterval);

    public void StartCycle(float interval)
    {
        StopCycle();
        cycleRoutine = StartCoroutine(CycleImages(interval));
    }

    private IEnumerator CycleImages(float interval)
    {
        int idx = 0;
        while (true)
        {
            for (int i = 0; i < objects.Count; i++)
                objects[i].SetActive(i == idx);

            idx = (idx + 1) % objects.Count;
            yield return new WaitForSeconds(interval);
        }
    }

    public void StopCycleAndReveal(int comp)
    {
        StopCycle();
        int idx = comp;
        for (int i = 0; i < objects.Count; i++)
            objects[i].SetActive(i == idx);
    }

    public void StopCycle()
    {
        if (cycleRoutine != null)
            StopCoroutine(cycleRoutine);
    }
}
