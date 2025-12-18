using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentExitController : MonoBehaviour
{
    public static EnvironmentExitController Instance;

    [SerializeField] public Button ExitToCantagoryEnvironment;
    [SerializeField] private Button ExitToCantagoryCards;
    [SerializeField] private GameObject LoadCanvas;
    [SerializeField] private GameObject MainEnv;
    [SerializeField] private GameObject[] CatagoryEnv;
    [SerializeField] private GameObject[] CatagoryEnteryPoints;
    [SerializeField] private Player player;

    private int index = 0;
    private float[] entrancePoints;

    private void Start()
    {
        entrancePoints = new float[CatagoryEnteryPoints.Length];

        for (int i = 0; i < CatagoryEnteryPoints.Length; i++)
        {
            entrancePoints[i] = CatagoryEnteryPoints[i].transform.position.x;
        }

        index = 0;
        ExitToCantagoryEnvironment.onClick.AddListener(OnExit);
        //ExitToCantagoryCards.onClick.AddListener(OnExit);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnExit()
    {
        StartCoroutine(OnExitToCantagoryEnvironment());
    }

    public IEnumerator OnExitToCantagoryEnvironment()
    {
        LoadCanvas.gameObject.SetActive(true);
        MainEnv.SetActive(true);

        foreach (var env in CatagoryEnv)
        {
            if (env.gameObject.activeSelf)
            {
                if (player.clickTargetIndicator != null && player.clickTargetIndicator.parent == null)
                    player.clickTargetIndicator.SetParent(player.transform);

                env.SetActive(false);
                player.transform.position = new Vector3(entrancePoints[index], player.transform.position.y, player.transform.position.z);
                player.moveTarget.position = player.transform.position;
                break;
            }
            else
            {
                index++;
                continue;
            }
        }

        yield return new WaitForSeconds(0.7f);

        ExitToCantagoryEnvironment.gameObject.SetActive(false);
        LoadCanvas.gameObject.SetActive(false);
        index = 0;
    }
}
