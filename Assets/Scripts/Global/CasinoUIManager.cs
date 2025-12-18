using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CasinoUIManager : MonoBehaviour
{
    public static CasinoUIManager Instance;
    public GameObject errorCanvas = null;
    public GameObject errorCanvasPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowErrorCanvas(int child, string msg)
    {
        if (errorCanvasPrefab != null)
        {
            if (errorCanvas == null)
            {
                errorCanvas = Instantiate(errorCanvasPrefab);
            }
        }

        switch (child)
        {
            case 0:
                errorCanvas.transform.GetChild(0).gameObject.SetActive(true);
                errorCanvas.transform.GetChild(1).gameObject.SetActive(false);

                errorCanvas.GetComponent<UIStatusController>().ShowLoader();
                break;
            case 1:
                if (!string.IsNullOrEmpty(msg))
                {
                    errorCanvas.transform.GetChild(1).gameObject.SetActive(true);
                    errorCanvas.transform.GetChild(0).gameObject.SetActive(false);

                    errorCanvas.GetComponent<UIStatusController>().ShowMessage(msg);
                }
                break;
            case 2:
                errorCanvas.GetComponent<UIStatusController>().HideLoader();
                break;
        }
    }
}
