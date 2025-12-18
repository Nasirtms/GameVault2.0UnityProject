using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BotManager : MonoBehaviour
{
    public static BotManager instance;

    [Header("Bot Setup")]
    [SerializeField] private List<GameObject> botGunInstantiatedList = new List<GameObject>();
    [SerializeField] private List<GunSpawnPoint> botGunPositions = new List<GunSpawnPoint>();
    //public Transform botParent;
    [HideInInspector] public float[] betOptions;
    //public GameObject botUI;

    public float minBotBalance = 50;
    public float maxBotBalance = 1000;

    public int minBotQuantity = 1;
    public int maxBotQuantity = 6;

    public float minShootInterval = 0.5f;
    public float maxShootInterval = 1.5f;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

     IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        GunManager.Instance.UpdateGun(0);
        betOptions = Manager.Instance.betOptions;
        for (int i = 0; i < Random.Range(minBotQuantity, maxBotQuantity) && i < botGunPositions.Count; i++)
        {
            int betIndex = Random.Range(0, betOptions.Length);
            Debug.Log("Bet Amount: " + betIndex);

            SpawnBotGun(betIndex, i);
        }
    }

    private void FixedUpdate()
    {
        //float.TryParse(axe.text, out Axe);
        //float.TryParse(why.text, out Why);
        //float.TryParse(zed.text, out Zed);
        //if (botGunInstantiatedList.Count > 0)
        //{
        //    botGunInstantiatedList[0].transform.position = new Vector3(Axe, Why, Zed);
        //}
    }

    private void SpawnBotGun(int betIndex, int botNumber = 0)
    {
        var gunLevels = GunManager.Instance.gunDatabase.gunLevels;

        if (betIndex < 0 || betIndex >= gunLevels.Count)
        {
            Debug.LogWarning("Invalid betIndex for bot gun. " + betIndex);
            return;
        }

        var free = new List<int>();
        for (int i = 0; i < botGunPositions.Count; i++)
        {
            var sp = botGunPositions[i].GetComponent<GunSpawnPoint>();
            if (sp != null && !sp.booked) free.Add(i);
        }
        if (free.Count == 0) return;

        int randomPos = free[Random.Range(0, free.Count)];

        GameObject gunPrefab = gunLevels[Mathf.Clamp(betIndex, 0 , gunLevels.Count -1)].fireSystemPrefab;
        GameObject botGun = Instantiate(gunPrefab, botGunPositions[randomPos].transform);
        botGun.transform.localPosition = new Vector3(0,-0.1f,0);
        //botGun.transform.localPosition = botGunPositions[randomPos].transform.position;
        //botGun.transform.localPosition = botGunPositions[randomPos].transform.position;
        //botUI.transform.localPosition = new Vector3(botGunPositions[randomPos].uiPos, Manager.Instance.botUI.transform.localPosition.y, Manager.Instance.botUI.transform.localPosition.z);
        //Debug.Log("BOt Position " + botGun.transform.position);
        botGunPositions[randomPos].GetComponent<GunSpawnPoint>().booked = true;
        botGunPositions[randomPos].GetComponent<GunSpawnPoint>().uiComponent.SetActive(true);
        botGunPositions[randomPos].GetComponent<GunSpawnPoint>().betText.text = betOptions[betIndex].ToString();
        botGun.name = $"Bot{botNumber + 1}";
        botGunInstantiatedList.Add(botGun);

        SetupBotController(botGun, betIndex, randomPos, botGunPositions[randomPos].GetComponent<GunSpawnPoint>());
    }

    private void SetupBotController(GameObject botGun, int betIndex, int botPos, GunSpawnPoint spawnPoint)
    {
        if (botGun == null) return;

        BotController botController = botGun.GetComponent<BotController>();
        if (botController == null)
        {
            botController = botGun.AddComponent<BotController>();
        }

        botController.spawnPoint = spawnPoint;
        botController.botPosition = botGunPositions[botPos].GetComponent<GunSpawnPoint>().gunPosition;
        botController.botFiringPoint = botGun.transform.Find("FiringPoint");
        botController.betIndex = betIndex;
        botController.bulletContainer = GunManager.Instance.bulletContainer.transform;
        botController.startingBalance = Random.Range(minBotBalance, maxBotBalance);
        botController.botShootInterval = Random.Range(minShootInterval, maxShootInterval);
        botController.StartShooting();
    }
}