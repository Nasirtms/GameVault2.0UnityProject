using DG.Tweening;
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
            case 3:


                var uiStatusController = errorCanvas.GetComponent<UIStatusController>();
                if (uiStatusController != null)
                {


                    uiStatusController.AddressablesError_Panel.SetActive(true);
                    uiStatusController.messagePanel.SetActive(false);
                    uiStatusController.loadingPanel.SetActive(false);
                    
                    if (!string.IsNullOrEmpty(msg))
                    {
                        uiStatusController.AddressablesError_txt.text = msg;
                    }

                    uiStatusController.AddressablesError_ScalePael.transform.GetComponent<RectTransform>().localScale = Vector3.zero;
                    uiStatusController.AddressablesError_ScalePael.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
                break;
        }
    }
}
