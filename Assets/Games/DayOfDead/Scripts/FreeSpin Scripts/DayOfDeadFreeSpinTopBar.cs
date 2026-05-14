using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayOfDeadFreeSpinTopBar : MonoBehaviour
{
    #region Variables

    [Header("References")]
    [SerializeField] public GameObject topbarArea;   
    [SerializeField] private GameObject tokenPrefab;  

    public List<GameObject> tokens = new List<GameObject>();

    public GameObject wildParticles;
    [Header("Layout")]
    // this is done manually in unity
    [SerializeField] private float tokenSpacing = 1.7f;      // distance along X
    [SerializeField] private float firstTokenX = -5.37f;   // X of index 0
    [SerializeField] private float tokenY = -0.3f;

    #endregion 

    public void CreateInitialTokens(int count)
    {
        ClearAllTokens();

        for (int i = 0; i < count; i++)
            AddToken();
    }

    public GameObject AddToken()
    {
        if (tokenPrefab == null || topbarArea == null)
        {
            Debug.LogWarning("Topbar missing prefab or area.");
            return null;
        }

        GameObject inst = Instantiate(tokenPrefab, topbarArea.transform);
        tokens.Add(inst);

        RepositionTokens();
        return inst;
    }

    public void RemoveLastToken()
    {
        if (tokens.Count == 0)
            return;

        GameObject last = tokens[tokens.Count - 1];
        tokens.RemoveAt(tokens.Count - 1);

        if (last != null)
            Destroy(last);

        RepositionTokens();
    }

    public void ClearAllTokens()
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] != null)
                Destroy(tokens[i]);
        }

        tokens.Clear();
    }

    private void RepositionTokens()
    {
        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] == null) continue;

            Transform t = tokens[i].transform;

            float x = firstTokenX + i * tokenSpacing;
            float y = tokenY;

            t.localPosition = new Vector3(x, y, t.localPosition.z);
        }
    }
    public GameObject GetToken()
    {
        if (tokens.Count == 0)
            return null;

        return tokens[tokens.Count - 1];
    }

    public void MoveParticles(GameObject token, Vector3 targetPosition, System.Action onComplete = null)
    {
        if (token == null) return;

        wildParticles.transform.position = token.transform.position;
        Debug.Log("LovKumar Target Position : " + targetPosition);
        StartCoroutine(MoveAndResetParticles(targetPosition, onComplete));
    }

    private IEnumerator MoveAndResetParticles(Vector3 targetLocalPosition, System.Action onComplete)
    {
        Vector3 originalPosition = wildParticles.transform.position;
        Debug.Log("LovKumar Particles Original Pos : " + originalPosition);
        wildParticles.SetActive(true);
        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(0.25f)
           .Append(wildParticles.transform
               .DOLocalMove(targetLocalPosition, 0.8f)
               .SetEase(Ease.Linear))
           .AppendInterval(1f)
           .OnComplete(() =>
           {
               wildParticles.SetActive(false);
               wildParticles.transform.position = originalPosition;
           });
        yield return seq.WaitForCompletion();
        yield return new WaitForSeconds(0.5f);
        onComplete?.Invoke();
    }
}