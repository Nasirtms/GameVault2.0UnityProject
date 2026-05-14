using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

[Serializable]
public class FishInfo
{
    public bool applyMovement = false;
    public Transform transform;
    public Vector3 destination;
    public float speed;
    public float rotationSpeed = 0.5f;
    public Fish fish;

    //public bool redirected = false;   // has this fish redirected mid-path?
    public bool allowRedirect = false; // should this fish try redirecting?

    public Vector3 initialStart; // to detect midpoint

    public bool destinationChangedTriggeredOnce = false;   // has this fish redirected mid-path?
}



[RequireComponent(typeof(Manager))]
public class FishManager : MonoBehaviour
{
 
    public static FishManager Instance;
    [Header("FishSpawner")]
    [SerializeField] FishSpawner fishspawner_new;

    [Header("Fish Prefabs")]
    [Tooltip("All single-fish and school-prefabs (batchSize>1)")]
    //[SerializeField] private GameObject fishPrefabs;


    [Header("Fish Database")]
    [SerializeField] private FishDatabase fishDatabase;
    [SerializeField] private Transform fishContainer;
    [SerializeField] private int fishPoolSize = 120;
    [SerializeField] private float stepCountSpeed = 1;
    [SerializeField] private float stepCountSpeed_normal = 1;
    [SerializeField] private float stepCountSpeed_bonusTriggered = 3;


    [Serializable]
    private class SpawnState
    {
        public string fishGuid;
        public List<string> fishBatchGuids = new List<string>();
        public GameObject prefab; 
        public FishData fishData; // using your custom fish data
        public float timer;
    }

    [Header("Debug Spawn States (Inspector View)")]
    [SerializeField] private List<SpawnState> spawnStates = new List<SpawnState>();
    //public int SpawnStatesCount => spawnStates.Count;

    private List<FishInfo> activeFishes = new List<FishInfo>();
    public int ActiveFishCount => activeFishes.Count;


    [SerializeField] private Transform[] movementPoints; // size 20
    [SerializeField] private int movementGap = 8;

    [Header("Wipe Animation")]
    [Tooltip("Reference to the WipeAnimation component")]
    [SerializeField] private WipeAnimation wipeAnimator;

    [Header("Prize Popup Settings")]
    [Tooltip("Prefab (Text) to show +prize on fish death")]
    [SerializeField] private GameObject prizePopupPrefab;

    [Tooltip("Canvas under which to spawn the popup")]
    [SerializeField] private Canvas uiCanvas;

    [Tooltip("Float speed (pixels/sec)")]
    [SerializeField] private float popupFloatSpeed = 50f;
    [Tooltip("How long the popup stays up (sec)")]
    [SerializeField] private float popupDuration = 1f;

    [Header("Bonus Round Settings")]
    [Tooltip("Enable bonus rounds?")]
    private bool isBonus = false;
    private bool bonusRoundStarted = false;
    [SerializeField] public bool bonusTriggered = false;
    private float gameTimer = 0f;
    private float totalGameTimer = 0f;
    private int bonusRoundsTriggeredCount = 0;

    [Tooltip("How long bonus lasts (sec)")]
    [SerializeField] public float bonusDuration = 30f;
    [SerializeField] public Transform bonusSpawnPoint;
    [SerializeField] public Transform bonusDestPoint;
    [SerializeField] public Transform bonusCenterPoint1;
    [SerializeField] public Transform bonusCenterPoint2;

    [Tooltip("Two prefabs sets for two types of bonus")]
    [SerializeField] public GameObject[] bonusPrefabsType1;
    [SerializeField] public GameObject[] bonusPrefabsType2;

    [Tooltip("Seconds from start when bonus begins")]
    [SerializeField] private Vector2 bonusStartTimeMinMax = new Vector2(1200f, 1800f);
    [SerializeField] private float bonusStartTime = 50f;
    [SerializeField] private bool requirePool = true;
    private Coroutine spawnLoop;

    [Header("Coin Animation")]
    [Tooltip("Where coins should be parented so they render under the root Canvas")]
    [SerializeField] private Transform coinContainer;
    [SerializeField] private float coinFlyDuration = 0.5f;
    [SerializeField] private float coinSpawnDelay = 0.05f;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private float horizontalSpacing = 0.5f;

    [SerializeField] private int spawnFishMinHealth;
    [SerializeField] private int spawnFishMaxHealth;

    private List<GameObject> activeBonusFishes = new List<GameObject>();

    [Header("Spawn Settings")]
    [SerializeField] private float globalSpawnIntervalMin = 1f;
    [SerializeField] private float globalSpawnIntervalMax = 3f;
    [SerializeField] private int totalFishCapacity = 40;
    private float globalSpawnTimer = 0f;
    private bool IsFirstBonusRound = true;
    private bool spawningInProgress = false;

    [SerializeField] private GameObject fullScreenBombExplosionPrefab;
    [SerializeField] private float fullScreenBombFXDuration = 1.2f;
    [SerializeField] private float bombFXScale = 0.8f;

    [Header("Coral Reef Powerup")]
    [SerializeField] private float coralReefDuration = 30f;
    [SerializeField] private Vector2Int coralReefMultiplierRange = new Vector2Int(1, 5);

    private float playerMultiplier = 1f;
    private float playerMultiplierExpireAt = 0f;

    [SerializeField] private bool bonusTriggerEnabled = true;
    private bool bonusTriggerConsumed = false;

    private int activeFSBombCarriers = 0;
    private int activeBonusTriggerCarriers = 0;
    [SerializeField] List<SpawnState> newSpawnStatesFromApi = new List<SpawnState>();
    bool ignoreFirst;
    [SerializeField] List<string> bonusFishNames = new List<string>();


    public int GenerateRandomHealth() => Random.Range(15, 51);

    // Start is called before the first frame update
    void Start()
    {
        if (fishDatabase == null || fishDatabase.fishList == null)
        {
            Debug.LogError("FishManager: FishDatabase or fishList is null.");
            return;
        }

        stepCountSpeed = stepCountSpeed_normal;
        IsFirstBonusRound = true;

        bonusTriggered = false;
        bonusStartTime = Random.Range(bonusStartTimeMinMax.x, bonusStartTimeMinMax.y);

        for (int i = 0; i < fishDatabase.fishList.Count; i++)
        {
            Debug.Log($"Adding fish from database: {fishDatabase.fishList[i].fishName}");
            var data = fishDatabase.fishList[i];
            if (!FishSpawnAccordingToHealth(data))
                continue;

            if (FishPool.Instance != null && fishDatabase.fishList[i].sprite != null)
                FishPool.Instance.Prewarm(fishDatabase.fishList[i].fishName, fishDatabase.fishList[i].sprite, fishPoolSize, fishContainer);

            spawnStates.Add(new SpawnState
            {
                prefab = fishDatabase.fishList[i].sprite,
                fishData = data,
                timer = Random.Range(globalSpawnIntervalMin, globalSpawnIntervalMax)
            });
            
            Debug.Log($"Adding fish from database: Added: {fishDatabase.fishList[i].fishName}");
        }

        Fish.OnFishKilledByPlayer += HandleFishKilledByPlayer;
        Fish.OnFishKilledByBot += HandleFishKilledByBot;

        SpawnFish();
        InvokeRepeating(nameof(CheckForEmptyFishes), 0f, 5f);

    }

    void CheckForEmptyFishes()
    {
        //if (fishspawner_new.GetTotalActiveCount() <= 40 && !isBonus && ignoreFirst && !bonusRoundStarted)
        if (fishspawner_new.GetTotalActiveCount() <= totalFishCapacity && ignoreFirst)
        {
            //SpawnBufferFishes();
            Debug.Log("total Active Fishes " + fishspawner_new.GetTotalActiveCount());
            SpawnMoreFishes(true);
        }

    }
    [ContextMenu("SpawnFishes")]
    public void SpawnFish() {
        StartCoroutine(nameof(WaitForData), false);
    }

    public void SpawnMoreFishes(bool isReset)
    {
        MermaidBackendBridge.instance.LoadBackendProfiles(isBonus ? 0 : Random.Range(15, 20), Manager.totalBetAmountPerInterval,Manager.totalFishWinAmountPerInterval);
        StartCoroutine(nameof(WaitForData), isReset);
    }

    public void SpwanBonusFishesFromAPI()
    {
        MermaidBackendBridge.instance.LoadBackendProfiles(bonusFishNames.Count, Manager.totalBetAmountPerInterval, Manager.totalFishWinAmountPerInterval, true, bonusFishNames);
        StartCoroutine(nameof(WaitForBonusFishes), false);
    }
    IEnumerator WaitForBonusFishes()
    {

        yield return new WaitUntil(() => MermaidBackendBridge.instance.IsReady());
        Debug.Log("currentActive Fish Count  " + fishspawner_new.GetTotalActiveCount());
        newSpawnStatesFromApi.Clear();
        //yield return new WaitForSeconds(2);
        foreach (var fish in MermaidAPIManager.Instance.fishProfilesInInspector)
        {
            var getfishFromSpawnList = spawnStates.FindAll(x => x.fishData.fishName.ToLower() == fish.fishName);

            if (getfishFromSpawnList.Count > 0)
            {
                for (int i = 0; i < getfishFromSpawnList.Count; i++)
                {
                    SpawnState newSpawn = new SpawnState();
                    newSpawn.fishData = getfishFromSpawnList[i].fishData;
                    newSpawn.prefab = getfishFromSpawnList[i].prefab;
                    newSpawn.timer = getfishFromSpawnList[i].timer;
                    newSpawn.fishGuid = fish.fishId;
                    newSpawn.fishBatchGuids = fish.fishIds.ToList();
                    newSpawn.fishData.maxHealth = fish.adjustedHealth;
                    newSpawn.fishData.fishMultiplyer = fish.fishMultiplyer;
                    //Debug.Log($"Fish Spawn 111 ___ fish: {newSpawn.fishData.fishName} ___ newSpawn.fishData.fishMultiplyer: {newSpawn.fishData.fishMultiplyer}");
                    newSpawnStatesFromApi.Add(newSpawn);

                }
            }

        }

    }




    IEnumerator WaitForData(bool isReset)
    {
        yield return new WaitUntil(() => MermaidBackendBridge.instance.IsReady());

        if (!spawningInProgress)
        {
            spawningInProgress = true;

            if (bonusRoundStarted)
                yield break;
            newSpawnStatesFromApi.Clear();
            Debug.Log("currentActive Fish Count  " + fishspawner_new.GetTotalActiveCount());
            ignoreFirst = true;
            if (isReset)
            {
                //Manager.totalBetAmountPerInterval = 0;
                //Manager.totalFishWinAmountPerInterval = 0;
            }
            //yield return new WaitForSeconds(2);
            foreach (var fish in MermaidAPIManager.Instance.fishProfilesInInspector)
            {
                var getfishFromSpawnList = spawnStates.FindAll(x => x.fishData.fishName.ToLower() == fish.fishName);

                if (getfishFromSpawnList.Count > 0)
                {
                    for (int i = 0; i < getfishFromSpawnList.Count; i++)
                    {
                        SpawnState newSpawn = new SpawnState();
                        newSpawn.fishData = getfishFromSpawnList[i].fishData;
                        newSpawn.prefab = getfishFromSpawnList[i].prefab;
                        newSpawn.timer = getfishFromSpawnList[i].timer;
                        newSpawn.fishGuid = fish.fishId;
                        newSpawn.fishBatchGuids = fish.fishIds.ToList();
                        newSpawn.fishData.maxHealth = fish.adjustedHealth;
                        newSpawn.fishData.batchSize = fish.batchSize;
                        newSpawn.fishData.fishMultiplyer = fish.fishMultiplyer;
                        //Debug.Log($"Fish Spawn 444 ___ fish: {newSpawn.fishData.fishName} ___ newSpawn.fishData.fishMultiplyer: {newSpawn.fishData.fishMultiplyer}");
                        newSpawnStatesFromApi.Add(newSpawn);
                        Debug.Log($"Adding Fish: {newSpawn.fishData.fishName} __ {newSpawn.fishGuid}");
                    }
                }

            }

            newSpawnStatesFromApi = newSpawnStatesFromApi.DistinctBy(x => x.fishGuid).ToList();

            StartCoroutine(SpawnAllFishes(newSpawnStatesFromApi));
        }
    }

    void SpawnBufferFishes()
    {

        int countToPick = Mathf.Min(10, newSpawnStatesFromApi.Count);
        List<SpawnState> shuffled = newSpawnStatesFromApi.OrderBy(x => Random.value).ToList();
        var randomUniqueSpawnStates = shuffled.Take(countToPick).ToList();
        StartCoroutine(SpawnAllFishes(randomUniqueSpawnStates));
    }

    #region New Code

    [Header("RTP Settings")]
    [SerializeField, Range(0.5f, 1.0f)] private float targetRTP = 0.95f;
    [SerializeField] private float rtpAdjustmentStrength = 0.5f; // how strongly RTP affects rarity

    private double totalBets = 0;
    private double totalPayouts = 0;

    public double CurrentRTP => totalBets > 0 ? totalPayouts / totalBets : 0;


    #endregion


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

    public bool FishSpawnAccordingToHealth(FishData fish)
    {
        //Debug.Log("Adding fish from database: fish: " + fish.fishName);
        //Debug.Log("Adding fish from database: fish.maxHealth: " + fish.maxHealth);
        //Debug.Log("Adding fish from database: spawnFishMinHealth: " + spawnFishMinHealth);
        //Debug.Log("Adding fish from database: spawnFishMaxHealth: " + spawnFishMaxHealth);
        //Debug.Log("Adding fish from database: booool: " + (fish.maxHealth >= spawnFishMinHealth) + " " + (fish.maxHealth <= spawnFishMaxHealth) + " " + (fish.maxHealth >= spawnFishMinHealth && fish.maxHealth <= spawnFishMaxHealth));
        if (fish.maxHealth >= spawnFishMinHealth && fish.maxHealth <= spawnFishMaxHealth)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public double CalculateCurrentRTP(float totalWinAmount, float totalBetAmount)
    {
        if (totalBetAmount <= 0f)
            return 0f; // Avoid divide-by-zero errors

        double currentRTP = (totalWinAmount / totalBetAmount) * 100f;
        return currentRTP;
    }
    int totalKilled;
    //private void HandleFishKilled(Fish fish)
    //{
    //    float prize = fish.maxHealth;
    //    //int bet = 1;

    //    if ((UnityEngine.Object)fish.lastAttacker == GetComponent<GunManager>())
    //    {
    //        totalKilled++;
    //        //if (totalKilled % 5 == 0 && !bonusRoundStarted)
    //        if (totalKilled % 5 == 0)
    //        {
    //            if (fishspawner_new.GetTotalActiveCount() <= totalFishCapacity)
    //            {
    //                //SpawnMoreFishes(true);
    //            }
    //        }
    //        //if (CalculateCurrentRTP(Manager.totalFishWinAmountPerInterval, Manager.totalBetAmountPerInterval) >= Manager.targetRTBFromBackend)
    //        //{
    //        //    Manager.onHealthMultiplier?.Invoke();
    //        //}

    //    }


    //    if (fish.lastAttacker is BotController bot)
    //    {
    //        bot.AddBalance(prize);
    //        ShowPrizePopup($"+{prize}", fish.transform.position, fish.prizeAmount);
    //        StartCoroutine(CoinBurstRoutine(fish.transform.position, (int)fish.prizeAmount, bot.transform.position));
    //    }
    //    else
    //    {
    //        Debug.Log($"Fish killed: fish: {JsonUtility.ToJson(fish)} ____________  fishData: {JsonUtility.ToJson(fish.fishData)}");

    //        float currentBetAmount = fish.currentBetamount == 0 ? Manager.currentBetAmoun : fish.currentBetamount;
    //        float newPayout = fish.fishData.fishMultiplyer * currentBetAmount;
    //        Debug.Log($"Fish killed: {fish.fishData.fishName} ___ multiplier: {fish.fishData.fishMultiplyer} ___ currentBetAmount: {currentBetAmount}");
    //        Manager.Instance.balance += newPayout;
    //        Manager.Instance.UpdateBalanceUI();
    //        ShowPrizePopup($"+{newPayout}", fish.transform.position, fish.prizeAmount);
    //        StartCoroutine(CoinBurstRoutine(fish.transform.position, (int)fish.prizeAmount, GunManager.Instance.gunTransform.position));

    //        Manager.totalFishWinAmountPerInterval += newPayout;

    //        if (!Manager.fishKilledListPerInterval.ContainsKey(fish.fishData.fishName))
    //            Manager.fishKilledListPerInterval.Add(fish.fishData.fishName, currentBetAmount);
    //        Manager.fishKilledListPerInterval[fish.fishData.fishName] += currentBetAmount;
    //    }
    //    for (int i = activeFishes.Count - 1; i >= 0; i--)
    //    {
    //        if (activeFishes[i].transform == fish.transform)
    //        {
    //            activeFishes.RemoveAt(i);
    //            break;
    //        }
    //    }
    //}

    private void HandleFishKilled(Fish fish)
    {
        float prize = fish.maxHealth;
        //int bet = 1;

        if ((UnityEngine.Object)fish.lastAttacker == GetComponent<GunManager>())
        {
            totalKilled++;
            //if (totalKilled % 5 == 0 && !bonusRoundStarted)
            if (totalKilled % 5 == 0)
            {
                if (fishspawner_new.GetTotalActiveCount() <= totalFishCapacity)
                {
                    //SpawnMoreFishes(true);
                }
            }
            //if (CalculateCurrentRTP(Manager.totalFishWinAmountPerInterval, Manager.totalBetAmountPerInterval) >= Manager.targetRTBFromBackend)
            //{
            //    Manager.onHealthMultiplier?.Invoke();
            //}

        }


        if (fish.lastAttacker is BotController bot)
        {
            bot.AddBalance(prize);
            ShowPrizePopup($"+{prize}", fish.transform.position, fish.prizeAmount);
            StartCoroutine(CoinBurstRoutine(fish.transform.position, (int)fish.prizeAmount, bot.transform.position));
        }
        //else
        //{
        //    Debug.Log($"Fish killed: fish: {JsonUtility.ToJson(fish)} ____________  fishData: {JsonUtility.ToJson(fish.fishData)}");

        //    float currentBetAmount = fish.currentBetamount == 0 ? Manager.currentBetAmoun : fish.currentBetamount;
        //    float newPayout = fish.fishData.fishMultiplyer * currentBetAmount;
        //    Debug.Log($"Fish killed: {fish.fishData.fishName} ___ multiplier: {fish.fishData.fishMultiplyer} ___ currentBetAmount: {currentBetAmount}");
        //    Manager.Instance.balance += newPayout;
        //    Manager.Instance.UpdateBalanceUI();
        //    ShowPrizePopup($"+{newPayout}", fish.transform.position, fish.prizeAmount);
        //    StartCoroutine(CoinBurstRoutine(fish.transform.position, (int)fish.prizeAmount, GunManager.Instance.gunTransform.position));

        //    Manager.totalFishWinAmountPerInterval += newPayout;

        //    if (!Manager.fishKilledListPerInterval.ContainsKey(fish.fishData.fishName))
        //        Manager.fishKilledListPerInterval.Add(fish.fishData.fishName, currentBetAmount);
        //    Manager.fishKilledListPerInterval[fish.fishData.fishName] += currentBetAmount;
        //}
        for (int i = activeFishes.Count - 1; i >= 0; i--)
        {
            if (activeFishes[i].transform == fish.transform)
            {
                activeFishes.RemoveAt(i);
                break;
            }
        }
    }

    void HandleFishKilledByBot(Fish fish, FishWSNetworkMessages.FishHit_Response response, bool fromBombKill)
    {
        float prize = fish.GetWinAmountByFormula(response.bulletCost);

        if (fish.lastAttacker is BotController bot)
        {
            if (!fromBombKill)
            {
                bot.AddBalance(prize);
            }
            ShowPrizePopup($"+{prize}", fish.transform.position, fish.prizeAmount);
            StartCoroutine(CoinBurstRoutine(fish.transform.position, (int)fish.prizeAmount, bot.transform.position));
        }

        for (int i = activeFishes.Count - 1; i >= 0; i--)
        {
            if (activeFishes[i].transform == fish.transform)
            {
                activeFishes.RemoveAt(i);
                break;
            }
        }
    }

    void HandleFishKilledByPlayer(Fish fish, FishWSNetworkMessages.FishHit_Response response, bool fromBombKill)
    {
        float prize;
        if (!fromBombKill)
        {
            prize = response.winAmount;
            Manager.Instance.balance += response.winAmount;
            Manager.Instance.UpdateBalanceUI();
        }
        else
        {
            prize = fish.GetWinAmountByFormula(response.bulletCost);
        }

        ShowPrizePopup($"+{prize}", fish.transform.position, fish.prizeAmount);
        StartCoroutine(CoinBurstRoutine(fish.transform.position, (int)fish.prizeAmount, GunManager.Instance.gunTransform.position));

        Manager.totalFishWinAmountPerInterval += response.winAmount;

        //if (!Manager.fishKilledListPerInterval.ContainsKey(fishInfo.fish.fishData.fishName))
        //    Manager.fishKilledListPerInterval.Add(fishInfo.fish.fishData.fishName, currentBetAmount);
        //Manager.fishKilledListPerInterval[fishInfo.fish.fishData.fishName] += currentBetAmount;

        for (int i = activeFishes.Count - 1; i >= 0; i--)
        {
            if (activeFishes[i].transform == fish.transform)
            {
                activeFishes.RemoveAt(i);
                break;
            }
        }
    }


    public IEnumerator CoinBurstRoutine(Vector3 worldStart, int count, Vector3 GoToPos)
    {
        var coins = new List<Transform>(count);
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = worldStart + Vector3.right * horizontalSpacing * i;

            var go = Instantiate(coinPrefab, spawnPos, Quaternion.identity, coinContainer);
            coins.Add(go.transform);
        }
        yield return new WaitForSeconds(coinSpawnDelay);
        foreach (var coin in coins)
        {
            FlyCoinWorld(coin, GoToPos);
           
        }
    }
    public void NotifyFishKilled(Fish fish)
    {
        if (fish.fishData != null && fish.fishData.allowOnlyOne)
        {
            activeFishCount[fish.fishData] = 0;
        }
    }
    private void FlyCoinWorld(Transform coin, Vector3 targetWorld)
    {
        coin.DOMove(targetWorld, coinFlyDuration).SetDelay(0.2f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => Destroy(coin.gameObject));
    }

    public void ShowPrizePopup(string text, Vector3 worldPos, float coinCount)
    {
        var go = Instantiate(prizePopupPrefab, uiCanvas.transform);
        var txt = go.GetComponent<Text>();
        txt.text = text;
        Vector2 screenPos = Manager.Instance.mainCam.WorldToScreenPoint(worldPos);
        go.transform.position = screenPos;
        StartCoroutine(PopupRoutine(go, txt));
    }
    private IEnumerator PopupRoutine(GameObject go, Text txt)
    {
        Color startColor = txt.color;
        Vector3 startPos = go.transform.position;
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            float t = elapsed / popupDuration;
            go.transform.position = startPos + Vector3.up * (popupFloatSpeed * t);
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
            txt.color = Color.Lerp(startColor, endColor, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(go);
    }

    // Update is called once per frame
    void Update()
    {
        MoveFishes(stepCountSpeed);

        totalGameTimer += Time.deltaTime;
        gameTimer += Time.deltaTime;

        if (!isBonus && !bonusTriggered && gameTimer >= bonusStartTime)
        {
            EnableBonusRound();
        }
        else if (isBonus)
        {
            gameTimer = 0;
        }
    }

    public void EnableBonusRound()
    {
        Debug.Log("deepak, enabling bonus");
        if (!isBonus)
        {
            bonusRoundStarted = true;
            bonusTriggered = true;
            Debug.Log("deepak, enabled bonus");
            StartCoroutine(BonusTransitionRoutine());
        }

    }

    private void SpawnRandomFishBatch(List<SpawnState> spawnStates)
    {
        if (spawnStates.Count == 0) return;

        float totalWeight = 0f;
        List<float> weights = new List<float>();


        foreach (var ss in spawnStates)
        {
            var data = ss.fishData;

            // ✅ Skip if only-one rule active and already alive
            if (data.allowOnlyOne && activeFishCount.ContainsKey(data) && activeFishCount[data] > 0)
            {
                weights.Add(0f);
                continue;
            }

            // ✅ Base weight from health (low health = common, high health = rare)
            float t = Mathf.InverseLerp(spawnFishMinHealth, spawnFishMaxHealth, data.maxHealth);
            float healthWeight = Mathf.Pow(1f - t, 3); // cubic makes high HP very rare

            // ✅ Apply rarityWeight directly
            float rarityWeight = Mathf.Clamp(data.rarityWeight, 1, 100) / 100f;

            // ✅ Combine health + rarity
            float finalWeight = healthWeight * rarityWeight;

            weights.Add(finalWeight);
            totalWeight += finalWeight;
        }

        if (totalWeight <= 0f) return;

        // ✅ Weighted random pick
        float rand = Random.value * totalWeight;
        for (int i = 0; i < spawnStates.Count; i++)
        {
            if (rand < weights[i])
            {
                var ss = spawnStates[i];
                SpawnBatch(ss.prefab, ss.fishData, ss.fishGuid, ss.fishBatchGuids);
                return;
            }
            rand -= weights[i];
        }
    }

    private IEnumerator SpawnAllFishes(List<SpawnState> spawnStates)
    {
        //Debug.Log("Spawning fish Count " + spawnStates.Count);
        if (spawnStates == null || spawnStates.Count == 0)
          yield return null;

        var safeList = new List<SpawnState>(spawnStates);

        foreach (var ss in safeList)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            var data = ss.fishData;

            //if (data.allowOnlyOne &&
            //    activeFishCount.ContainsKey(data) &&
            //    activeFishCount[data] > 0)
            if (data.allowOnlyOne &&
                activeFishes.Exists(x => x.fish.fishData.fishName == data.fishName))
            {
                continue;
            }

            SpawnBatch(ss.prefab, data, ss.fishGuid, ss.fishBatchGuids);
            yield return new WaitForSeconds(0.25f);
        }
        //for (int i=safeList.Count - 1; i>=0; i--)
        //{
        //    var data = safeList[i].fishData;
        //    if (!data.fishName.ToLower().Contains("shark"))
        //    {
        //        //Debug.Log($"Spawning from safelist: Not Shark, skipping..");
        //        continue;
        //    }
        //    Debug.Log($"Spawning from safelist: data.fishName: {data.fishName} ___________________");
        //    try
        //    {
        //        Debug.Log($"Spawning from safelist: data.allowOnlyOne: {data.allowOnlyOne} ___ activeFishCount.ContainsKey(data): {activeFishCount.ContainsKey(data)} ___ activeFishCount[data] > 0): {activeFishCount[data] > 0}");
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Debug.Log($"Spawning from safelist Exception: Message: {ex.Message}");
        //    }

        //    foreach (var key in activeFishCount)
        //    {
        //        Debug.Log($"Key: {key.Key.fishName} ___ Value: {key.Value}");
        //    }
        //    //if (data.allowOnlyOne &&
        //    //    activeFishCount.ContainsKey(data) &&
        //    //    activeFishCount[data] > 0)
        //    if (data.allowOnlyOne &&
        //        activeFishes.Exists(x=>x.fish.fishData.fishName == data.fishName))
        //    {
        //        continue;
        //    }

        //    yield return new WaitForSeconds(0.25f);
        //    SpawnBatch(safeList[i].prefab, data);
        //}

        spawningInProgress = false;
    }
    // Track which fish types are currently alive
    private Dictionary<FishData, int> activeFishCount = new Dictionary<FishData, int>();


    private void SpawnBonusFishes(GameObject[] chosen)
    {
        Dictionary<string, List<string>> spawnedFishes = new Dictionary<string, List<string>>();

        foreach (var prefab in chosen)
        {
            if (prefab == null) continue;

            var controller = prefab.GetComponent<BonusFishController>();
            if (controller == null)
            {
                //Debug.LogWarning($"BonusFishController missing on prefab: {prefab.name}");
                continue;
            }

            foreach (var bonusSet in fishDatabase.bonusFishes)
            {
                // Only apply sets that match the movement type of the prefab
                if (bonusSet.movementType != controller.movementType)
                    continue;

                // Set spawn point based on movement type
                Vector3 spawnBase;
                if (bonusSet.movementType == BonusFishMovementType.ClockWiseRotate)
                {
                    spawnBase = bonusCenterPoint2.position;
                }
                else if (bonusSet.movementType == BonusFishMovementType.AntiClockWiseRotate)
                {
                    spawnBase = bonusCenterPoint1.position;
                }
                else
                {
                    spawnBase = bonusSpawnPoint.position;
                }

                Vector3 spawnPos = spawnBase + (Vector3)bonusSet.spawnOffset;


                var go = Instantiate(prefab, spawnPos, Quaternion.identity);
                activeBonusFishes.Add(go);

                controller = go.GetComponent<BonusFishController>();
                if (controller == null) continue;

                // 🟦 BIG FISHES
                if (controller.bigFishRoot != null)
                {
                    var bigFishComponents = controller.bigFishRoot.GetComponentsInChildren<Fish>(true);

                    for (int i = 0; i < bigFishComponents.Length; i++)
                    {
                        FishData dataToApply = bonusSet.bigFishes.Count > i
                            ? bonusSet.bigFishes[i]
                            : bonusSet.bigFishes.Count > 0 ? bonusSet.bigFishes[0] : null;

                        if (dataToApply != null)
                            bigFishComponents[i].ApplyData(dataToApply);
                        go.name = prefab.name;
                        bigFishComponents[i].gameObject.name = bigFishComponents[i].fishData.fishName;
                        bigFishComponents[i].InitializeMovement(bonusDestPoint.position);
                        bigFishComponents[i].PlayAnimation();
                        bigFishComponents[i].speed = bonusSet.bonusFishSpeed;
                        //Debug.Log($"Fish Spawn 555 ___ fish: {bigFishComponents[i].fishData.fishName} ___ newSpawn.fishData.fishMultiplyer: {bigFishComponents[i].fishData.fishMultiplyer}");
                        var getMaxHealthFromAPI = newSpawnStatesFromApi.Find(x => x.fishData.fishName == bigFishComponents[i].fishData.fishName);
                        if (getMaxHealthFromAPI != null)
                        {
                            //bigFishComponents[i].fishData.maxHealth = getMaxHealthFromAPI.fishData.maxHealth;
                            bigFishComponents[i].fishData.fishMultiplyer = getMaxHealthFromAPI.fishData.fishMultiplyer;
                            //Debug.Log($"Fish Spawn 555 111 ___ fish: {bigFishComponents[i].fishData.fishName} ___ getMaxHealthFromAPI.fishData.maxHealth: {getMaxHealthFromAPI.fishData.maxHealth} ___ bigFishComponents[i].fishData.maxHealth: {bigFishComponents[i].fishData.maxHealth}");
                            //Debug.Log($"Fish Spawn 555 222 ___ fish: {bigFishComponents[i].fishData.fishName} ___ getMaxHealthFromAPI.fishData.fishMultiplyer: {getMaxHealthFromAPI.fishData.fishMultiplyer} ___ bigFishComponents[i].fishData.fishMultiplyer: {bigFishComponents[i].fishData.fishMultiplyer}");
                        }
                        bigFishComponents[i].fishData.maxHealth = GenerateRandomHealth();
                        bigFishComponents[i].fishGuid = Guid.NewGuid().ToString();

                        activeFishes.Add(new FishInfo
                        {
                            transform = go.transform,
                            speed = 1,
                            rotationSpeed = 1,
                            allowRedirect = false,
                            fish = bigFishComponents[i],
                            applyMovement = false
                        });

                        if (!spawnedFishes.ContainsKey(bigFishComponents[i].fishData.fishName))
                            spawnedFishes.Add(bigFishComponents[i].fishData.fishName, new List<string>());
                        spawnedFishes[bigFishComponents[i].fishData.fishName].Add(bigFishComponents[i].fishGuid);

                        controller.StartMovement(bonusDestPoint.position, bonusSet.bonusFishSpeed);
                    }
                }

                // 🟨 PATTERN GROUPS
                for (int p = 0; p < bonusSet.patternFishGroups.Count && p < controller.rootObject.Count; p++)
                {
                    var group = bonusSet.patternFishGroups[p];
                    var rootObj = controller.rootObject[p];

                    if (group.fishData == null || rootObj == null) continue;

                    var patternFishComponents = rootObj.GetComponentsInChildren<Fish>(true);

                    foreach (var fish in patternFishComponents)
                    {
                        var getMaxHealthFromAPI = newSpawnStatesFromApi.Find(x => x.fishData.fishName == group.fishData.fishName);
                        if (getMaxHealthFromAPI != null)
                        {
                            group.fishData.maxHealth = getMaxHealthFromAPI.fishData.maxHealth;
                            group.fishData.fishMultiplyer = getMaxHealthFromAPI.fishData.fishMultiplyer;
                            //Debug.Log($"Fish Spawn 333 ___ fish: {group.fishData.fishName} ___ newSpawn.fishData.fishMultiplyer: {group.fishData.fishMultiplyer}");
                        }
                        fish.gameObject.name = group.fishData.fishName;
                        go.name = prefab.name;
                        fish.ApplyData(group.fishData);
                        fish.InitializeMovement(bonusDestPoint.position);
                        fish.PlayAnimation();
                        fish.speed = bonusSet.bonusFishSpeed;
                        var getMaxHealthFromAPI_smallFishes = newSpawnStatesFromApi.Find(x => x.fishData.fishName == fish.fishData.fishName);
                        if (getMaxHealthFromAPI_smallFishes != null)
                        {
                            //fish.fishData.maxHealth = getMaxHealthFromAPI.fishData.maxHealth;
                            fish.fishData.fishMultiplyer = getMaxHealthFromAPI.fishData.fishMultiplyer;
                            //Debug.Log($"Fish Spawn 222 ___ fish: {fish.fishData.fishName} ___ newSpawn.fishData.fishMultiplyer: {fish.fishData.fishMultiplyer}");
                        }

                        fish.fishData.maxHealth = GenerateRandomHealth();
                        fish.fishGuid = Guid.NewGuid().ToString();

                        activeFishes.Add(new FishInfo
                        {
                            transform = go.transform,
                            speed = 1,
                            rotationSpeed = 1,
                            allowRedirect = false,
                            fish = fish,
                            applyMovement = false
                        });

                        if (!spawnedFishes.ContainsKey(fish.fishData.fishName))
                            spawnedFishes.Add(fish.fishData.fishName, new List<string>());
                        spawnedFishes[fish.fishData.fishName].Add(fish.fishGuid);

                        controller.StartMovement(bonusDestPoint.position, bonusSet.bonusFishSpeed);
                    }
                }

                break; // Stop after applying one matching set
            }
        }

        StartCoroutine(SendBonusSpawnedFishesListToBackend(spawnedFishes));
    }

    IEnumerator SendBonusSpawnedFishesListToBackend(Dictionary<string, List<string>> spawnedFishes)
    {
        List<SpawnedFishesListToServer_RequestBody.FishGroup> spawnedFishesGroups = new List<SpawnedFishesListToServer_RequestBody.FishGroup>();

        foreach (var key in spawnedFishes.Keys)
        {
            SpawnedFishesListToServer_RequestBody.FishGroup fishGroup = new SpawnedFishesListToServer_RequestBody.FishGroup();
            fishGroup.fishName = key;

            foreach (var value in spawnedFishes[key])
                fishGroup.fishIds.Add(value);

            spawnedFishesGroups.Add(fishGroup);
        }

        StartCoroutine(MermaidAPIManager.Instance.SendSpawnedFishesListToServer(spawnedFishesGroups));

        yield return new WaitForEndOfFrame();
    }


    private IEnumerator BonusTransitionRoutine()
    {
        GameObject[] chosen;
        bonusFishNames.Clear();
        if (IsFirstBonusRound)
        {
            //chosen = bonusPrefabsType1;
            IsFirstBonusRound = false;
        }
        else
        {
            //chosen = bonusPrefabsType2;
            IsFirstBonusRound = true;
        }

        bonusRoundsTriggeredCount++;
        if (bonusRoundsTriggeredCount % 2 == 0)
        {
            chosen = bonusPrefabsType2;
        }
        else
        {
            chosen = bonusPrefabsType1;
        }

        foreach (var item in chosen)
        {
            var controller = item.GetComponent<BonusFishController>();
            var getBonusFishDatabase = fishDatabase.bonusFishes.FindAll(x => x.movementType == controller.movementType).ToList();

            foreach (var getAll in getBonusFishDatabase)
            {
                bonusFishNames.Add(getAll.bigFishes[0].fishName);//because big fish is only 1
                int fishManhoosCount = controller.rootObject.Count;

                for (int i = 0; i < fishManhoosCount; i++)
                {

                    bonusFishNames.Add(getAll.patternFishGroups[i].fishData.fishName);
                }
            }
        }

        SpwanBonusFishesFromAPI();
        isBonus = true;

        float scsTemp = stepCountSpeed;
        Tweener tw = DOVirtual.Float(scsTemp, stepCountSpeed_bonusTriggered, .4f, (x) => stepCountSpeed = x).SetEase(Ease.OutSine);
        //stepCountSpeed = stepCountSpeed_bonusTriggered;
        //const float speedMultiplier = 3f;

        //foreach (var info in activeFishes)
        //{
        //    info.speed *= speedMultiplier;
        //}

        float waitingTime = totalGameTimer + 15;

        //if (activeFishes.Count != 0)
        //{
        //    yield return new WaitForSeconds(10); // jugar ager fish nah bund hoti sometimes manhoos disable nah hoti pata nah q??
        //    activeFishes.Clear();
        //}

        //List<FishInfo> activeFishesLeftTemp = new List<FishInfo>();

        yield return new WaitUntil(() => (activeFishes.Count == 0 || totalGameTimer >= waitingTime));

        for (int i = activeFishes.Count - 1; i >= 0; i--)
        {
            activeFishes[i].fish.ForceReachDestination();
        }

        tw.Kill();

        yield return new WaitForEndOfFrame();

        if (activeFishes.Count > 0)
            activeFishes.Clear();

        stepCountSpeed = stepCountSpeed_normal;

        yield return new WaitForEndOfFrame();

        while (activeFishes.Count > 0)
            yield return null;
        if (wipeAnimator != null)
        {
            wipeAnimator.PlayWipe(() => SpawnBonusFishes(chosen));
        }
        else
        {
            SpawnBonusFishes(chosen);
        }
        StartCoroutine(EndBonusAfterDelay());
    }

    //bool CheckIfActiveFishesInCamera(out List<FishInfo> activeFishesLeft) {

    //}

    private IEnumerator EndBonusAfterDelay()
    {
        yield return new WaitForSeconds(bonusDuration);
        EndBonusRound();
    }
    private void EndBonusRound()
    {
        isBonus = false;
        stepCountSpeed = stepCountSpeed_normal;
        spawningInProgress = false;

        // Destroy all normal fishes
        for (int i = activeFishes.Count - 1; i >= 0; i--)
        {
            Destroy(activeFishes[i].transform.gameObject);
            activeFishes.RemoveAt(i);
        }

        // Destroy all bonus fish prefabs
        for (int i = activeBonusFishes.Count - 1; i >= 0; i--)
        {
            if (activeBonusFishes[i] != null)
                Destroy(activeBonusFishes[i]);
        }

        activeBonusFishes.Clear();
        bonusTriggerConsumed = false;
        bonusRoundStarted = false;
        bonusTriggered = false;
        bonusStartTime = Random.Range(bonusStartTimeMinMax.x, bonusStartTimeMinMax.y);
    }


    private void SpawnBatch(GameObject prefab, FishData fishData, string fishGuid, List<string> fishBatchGuids)
    {
        if (movementPoints == null || movementPoints.Length < 2) return;

        int pointCount = movementPoints.Length;
        int spawnIndex = Random.Range(0, pointCount);
        int direction = Random.value < 0.5f ? 1 : -1;
        int destIndex = (spawnIndex + direction * movementGap + pointCount) % pointCount;

        Transform spawnAnchor = movementPoints[spawnIndex];
        Transform destAnchor = movementPoints[destIndex];

        Vector3 dir = (destAnchor.position - spawnAnchor.position).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized;
        Vector2 jitter = Random.insideUnitCircle * fishData.spawnOffset;

        Vector3 baseSpawnPos = spawnAnchor.position + new Vector3(jitter.x, jitter.y, 0f);
        Vector3 baseDestPos = destAnchor.position;


        //var tempGO = SpawnFish(fishData.fishName, prefab, new Vector3(99999f, 99999f, 0f), Quaternion.identity, fishContainer);

        //if (tempGO == null)
        //{
        //    Debug.LogWarning("Fish GameObject reference is null, skipping...");
        //    return;
        //}

        //var tempFish = tempGO.GetComponent<Fish>();
        //if (tempFish == null)
        //{
        //    DespawnFish(tempGO);
        //    return;
        //}

        //tempFish.ApplyData(fishData);
        //tempFish.fishGuid = fishGuid;

        //SpriteRenderer sr = tempFish.sr;
        //if (sr == null)
        //{
        //    DespawnFish(tempGO);
        //    return;
        //}


        //float fishWidth = sr.bounds.size.x;
        //DespawnFish(tempGO);


        SpriteRenderer sr = prefab.GetComponent<Fish>().sr;
        float fishWidth;
        if (sr != null)
        {
            fishWidth = sr.bounds.size.x;
        }
        else
            return;


        FishBatchSpawner.GenerateBatchPositions(
            fishData,
            baseSpawnPos,
            baseDestPos,
            dir,
            perp,
            sr,
            Direction.Left,
            Direction.Left,
            0f,
            0f,
            out var spawnPositions,
            out var destPositions
        );

        float spacing = fishWidth * 1.5f; //the greater the value the greater space manhoos jugar
        for (int i = 0; i < fishData.batchSize; i++)
        {
            Vector3 offset = perp * (i - (fishData.batchSize - 1) / 2f) * spacing;
            Vector2 rand = Random.insideUnitCircle * 0.15f; 
            spawnPositions[i] = baseSpawnPos + offset + new Vector3(rand.x, rand.y, 0f);
            destPositions[i] = baseDestPos + offset + new Vector3(rand.x, rand.y, 0f);
        }

        float consBatchSpeed = Random.Range(fishData.minSpeed, fishData.maxSpeed);

 
        for (int i = 0; i < fishData.batchSize; i++)
        {
            var go = SpawnFish(
                fishData.fishName,
                prefab,
                spawnPositions[i],
                Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg),
                fishContainer
            );

            //Debug.Log($"Spawning Fish: fishName: {fishData.fishName} ___ batchSize: {fishData.batchSize}");

            if (go == null)
            {
                Debug.LogWarning("Fish GameObject reference is null, skipping...");
                continue;
            }

            var fc = go.GetComponent<Fish>();
            if (fc == null)
            {
                DespawnFish(go);
                continue;
            }

            fc.ApplyData(fishData);
            fc.fishGuid = fishBatchGuids[i];
            fc.InitializeMovement(destPositions[i]);

   
            float individualSpeed = consBatchSpeed * Random.Range(0.95f, 1.05f);

            activeFishes.Add(new FishInfo
            {
                transform = go.transform,
                destination = destPositions[i],
                speed = individualSpeed,
                rotationSpeed = fc.rotationSpeed,
                allowRedirect = fc.isRotatable,
                initialStart = spawnPositions[i],
                fish = fc,
                applyMovement = true
            });

            var getFishFromSpawnner = fishspawner_new.fishData.Find(x => x.fishId == fishData.fishName);
            if (getFishFromSpawnner != null)
            {
                getFishFromSpawnner.currentActive++;
                if (getFishFromSpawnner.currentActive >= getFishFromSpawnner.fishObject.Count)
                {
                    fishspawner_new.OverrideFishDataFromActive(fishData);
                    getFishFromSpawnner.currentActive = getFishFromSpawnner.fishObject.Count;
                }
            }
        }

 
        if (!activeFishCount.ContainsKey(fishData))
            activeFishCount[fishData] = 0;
        activeFishCount[fishData] += fishData.batchSize;

        ChangeDestinationAfterDelay();
    }

    void ChangeDestinationAfterDelay()
    {
        for (int i = 0; i < activeFishes.Count; i++)
        {
            if (activeFishes[i].allowRedirect && !activeFishes[i].destinationChangedTriggeredOnce && Random.Range(0, 3) > 0)
            {
                StartCoroutine(nameof(ChangeDestinationAfterDelay_Coroutine), activeFishes[i]);
            }
        }
    }

    IEnumerator ChangeDestinationAfterDelay_Coroutine(FishInfo fishInfo)
    {
        fishInfo.destinationChangedTriggeredOnce = true;
        Transform temp = fishInfo.transform;

        yield return new WaitForSeconds(Random.Range(5f, 8f));

        if (!bonusRoundStarted)
        {
            if (fishInfo != null && fishInfo.transform == temp)
            {
                Vector3 newDestination = movementPoints[Random.Range(0, movementPoints.Length)].position;
                //Debug.Log($"Changing Destination: Fish Index: {activeFishes.IndexOf(fishInfo)} ___ FROM: {fishInfo.destination}");
                fishInfo.destination = newDestination;
                fishInfo.transform.GetComponent<Fish>().InitializeMovement(newDestination);
                //Debug.Log($"Changing Destination Fish Index: {activeFishes.IndexOf(fishInfo)} ___ TO: {fishInfo.destination}");
            }
        }
    }


    private void MoveFishes(float stepCount = 1)
    {
        for (int i = activeFishes.Count - 1; i >= 0; i--)
        {
            var info = activeFishes[i];
            if (info.transform == null)
            {
                activeFishes.RemoveAt(i);
                continue;
            }

            Transform t = info.transform;

            Vector3 toDest = info.destination - t.position;
            float dist;

            if (info.applyMovement)
            {
                toDest = info.destination - t.position;
                dist = toDest.magnitude;
            }
            else
            {
                dist = 10;
            }

            if (dist > 0.01f)
            {
                if (info.applyMovement)
                {
                    if (!bonusRoundStarted)
                    {
                        Vector3 dir = toDest.normalized;

                        // Smooth rotation toward destination
                        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);
                        float rotationSmoothness = info.rotationSpeed;
                        t.rotation = Quaternion.Slerp(t.rotation, targetRot, rotationSmoothness * Time.deltaTime);
                    }

                    // Move toward destination without overshooting
                    float step = info.speed * Time.deltaTime * stepCount;
                    if (step > dist)
                        step = dist; // clamp to stop exactly at destination

                    //t.position += dir * step;
                    t.position += t.right * step;

                    //Debug.Log($"Moving... fishIndex: {activeFishes.IndexOf(info)} ___ step: {step} ___ fishInfo: {JsonUtility.ToJson(info)}");
                }
            }
            else
            {
                // Reached destination
                FishReachedDestination(info, t, i);
                //var locked = LockManager.GetLockedFish();
                //if (locked != null && locked.transform == t)
                //    LockManager.ClearLockedFish();

                //var fishComp = t.GetComponent<Fish>();
                //if (fishComp != null)
                //    fishComp.CallDeathEvent();

                //DespawnFish(t.gameObject);
                //activeFishes.RemoveAt(i);
                continue;
            }
        }

    }

    public void FishReachedDestination(Fish fish)
    {
        FishInfo fishInfo = activeFishes.Find(x => x.transform == fish.transform);

        if (fishInfo == null)
        {
            //Debug.Log($"Fish reached screen exit... Fish not in activeFishes: {fish.fishData.fishName}");
            return;
        }

        FishReachedDestination(fishInfo, fishInfo.transform, activeFishes.IndexOf(fishInfo));
    }

    void FishReachedDestination(FishInfo fishInfo, Transform t, int index)
    {
        var locked = LockManager.GetLockedFish();
        if (locked != null && locked.transform == t)
            LockManager.ClearLockedFish();

        if (!fishInfo.fish.despawnCallSentToBackend)
        {
            fishInfo.fish.despawnCallSentToBackend = true;

            FishWSNetworkMessages.FishDespawn_Request fd_req = new FishWSNetworkMessages.FishDespawn_Request()
            {
                requestId = Guid.NewGuid().ToString(),
                gameId = SceneManagement.currentGameID,
                fishId = fishInfo.fish.fishGuid
            };
            FishWSNetworkManager.Instance.Send(fd_req);
        }

        var fishComp = t.GetComponent<Fish>();
        if (fishComp != null)
            fishComp.CallDeathEvent();

        DespawnFish(t.gameObject);
        activeFishes.RemoveAt(index);
    }


    private Vector3 GetClosestMovementPoint(Vector3 fromPos)
    {
        if (movementPoints == null || movementPoints.Length == 0) return fromPos;

        Transform closest = movementPoints[0];
        float minDist = Vector3.Distance(fromPos, closest.position);

        foreach (var point in movementPoints)
        {
            float dist = Vector3.Distance(fromPos, point.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = point;
            }
        }

        return closest.position;
    }
    private GameObject SpawnFish(string fishId,GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
    {
        var p = parent != null ? parent : fishContainer;
        if (FishPool.Instance != null)
        {
            var pooled = FishPool.Instance.Get(fishId, prefab, pos, rot, p);
            if (pooled != null)
            {
                return pooled;
            }
            Debug.LogWarning("FishPool.Get returned null; falling back to Instantiate.");
            return null;
           
        }
        return null;
        //if (prefab == null) { Debug.LogError("SpawnFish: prefab is null."); return null; }
        //// Instantiate prefab with proper transform
        //var go = Instantiate(prefab, pos, rot, p);
        //go.name = fishId;
        //Debug.LogError("SpawnFish: prefab is Nasir.");
        //return go;
    }

    private void DespawnFish(GameObject go)
    {
        if (!go) return;

        if (FishPool.Instance != null) FishPool.Instance.Release(go);     
        else Destroy(go);
    }

    //private IEnumerator SpawnLoop()
    //{
    //    // Give one frame so other components run their Awake/Start
    //    yield return null;

    //    // If you require a pool, wait until it exists (or bail out if you prefer fallback)
    //    while (requirePool && FishPool.Instance == null)
    //        yield return null;

    //    // Optional: validate critical deps once
    //    if (!ValidateDeps()) yield break;

    //    while (true)
    //    {
    //        if (!isBonus)
    //            SpawnRandomFishBatch();

    //        // Randomized cadence using your min/max
    //        float wait = Random.Range(globalSpawnIntervalMin, globalSpawnIntervalMax);
    //        yield return new WaitForSeconds(wait);
    //    }
    //}
    private bool ValidateDeps()
    {
        //if (fishPrefabs == null) { Debug.LogError("FishManager: fishPrefabs is null."); return false; }
        if (fishDatabase == null || fishDatabase.fishList == null) { Debug.LogError("FishManager: fishDatabase/fishList null."); return false; }
        if (movementPoints == null || movementPoints.Length < 2) { Debug.LogError("FishManager: movementPoints not set."); return false; }
        foreach (var t in movementPoints) if (t == null) { Debug.LogError("FishManager: movementPoints contains null."); return false; }
        return true;
    }

    public void TriggerBomb(Fish sourceFish, FishWSNetworkMessages.FishHit_Response response)
    {
        // find all fishes of same prefab/type (excluding source)
        var sameTypeFishes = new List<Fish>();
        foreach (var info in new List<FishInfo>(activeFishes))
        {
            if (info == null)
            {
                Debug.LogWarning("Fish GameObject reference is null, skipping...");
                return;
            }
            Fish f = info.transform.GetComponent<Fish>();
            if (f != null && f.fishData == sourceFish.fishData && f != sourceFish)
            {
                sameTypeFishes.Add(f);
            }
        }

        float currentBetAmount = response.bulletCost;
        //float totalPrize = sourceFish.prizeAmount;
        float totalPrize = 0;

        List<Fish> killedFishes = sameTypeFishes.ToList();
        Debug.Log("Fishes killed by bomb: " + JsonUtility.ToJson(killedFishes));

        // FX + kill for each chain target
        foreach (var f in sameTypeFishes)
        {
            totalPrize += f.GetWinAmountByFormula(currentBetAmount);

            // 🔥 scaled FX at each fish wiped by Bomb
            SpawnPowerupFX(f.transform.position, bombFXScale, fullScreenBombExplosionPrefab, fullScreenBombFXDuration);

            StartCoroutine(forceKill(f, response));
        }

        FishWSNetworkMessages.FishHit_Request fishHit_request = new FishWSNetworkMessages.FishHit_Request()
        {
            requestId = Guid.NewGuid().ToString(),
            gameId = SceneManagement.currentGameID,
            bulletId = response.bulletId,
            fishId = "",
            fishIdsKilledByBomb = killedFishes.Select(x => x.fishGuid).ToList(),
            bulletCost = currentBetAmount.ToString("G17", CultureInfo.InvariantCulture),
            killedByBomb = true,
            killedByBot = response.killedByBot
        };

        FishWSNetworkManager.Instance.Send(fishHit_request);
        Debug.Log($"Killed By Bomb ___ currentBetAmount: {currentBetAmount}");

        // 🔥 also spawn FX where the Bomb carrier died
        SpawnPowerupFX(sourceFish.transform.position, bombFXScale, fullScreenBombExplosionPrefab, fullScreenBombFXDuration);

        RewardAttacker(response.killedByBot == false ? GunManager.Instance : sourceFish.lastAttacker, totalPrize);
    }


    public void TriggerFullScreenBomb(Fish sourceFish, FishWSNetworkMessages.FishHit_Response response)
    {
        SpawnPowerupFX(sourceFish.transform.position, 5f, fullScreenBombExplosionPrefab, fullScreenBombFXDuration);

        float totalPrize = 0;
        var fishesToKill = new List<Fish>();

        //float currentBetAmount = sourceFish.currentBetamount == 0 ? Manager.currentBetAmoun : sourceFish.currentBetamount;
        float currentBetAmount = response.bulletCost;

        foreach (var info in new List<FishInfo>(activeFishes))
        {
            if (info == null)
            {
                Debug.LogWarning("Fish GameObject reference is null, skipping...");
                continue;
            }
            Fish f = info.transform.GetComponent<Fish>();
            if (f != null && f != sourceFish)
            {
                fishesToKill.Add(f);
                //totalPrize += f.prizeAmount;
                totalPrize += f.GetWinAmountByFormula(currentBetAmount);
            }
        }

        List<Fish> killedFishes = fishesToKill.ToList();
        Debug.Log("Fishes killed by bomb: " + JsonUtility.ToJson(killedFishes));

        foreach (var f in fishesToKill)
            StartCoroutine(forceKill(f, response));

        //totalPrize += sourceFish.prizeAmount;

        FishWSNetworkMessages.FishHit_Request fishHit_request = new FishWSNetworkMessages.FishHit_Request()
        {
            requestId = Guid.NewGuid().ToString(),
            gameId = SceneManagement.currentGameID,
            bulletId = response.bulletId,
            fishId = "",
            fishIdsKilledByBomb = killedFishes.Select(x => x.fishGuid).ToList(),
            bulletCost = currentBetAmount.ToString("G17", CultureInfo.InvariantCulture),
            killedByBomb = true,
            killedByBot = response.killedByBot
        };

        FishWSNetworkManager.Instance.Send(fishHit_request);
        Debug.Log($"Killed By FullScreenBomb ___ currentBetAmount: {currentBetAmount}");

        RewardAttacker(response.killedByBot == false? GunManager.Instance : sourceFish.lastAttacker, totalPrize);
    }

    public void TriggerCoralReef(Fish sourceFish, FishWSNetworkMessages.FishHit_Response response)
    {
        int mult = UnityEngine.Random.Range(coralReefMultiplierRange.x, coralReefMultiplierRange.y + 1);
        ApplyMultiplierToShooter(sourceFish.lastAttacker, mult, coralReefDuration);
    }

    public void TriggerCannonCard(Fish sourceFish, FishWSNetworkMessages.FishHit_Response response)
    {
        // Give the one-shot cannon only to the killer (player)
        if (sourceFish.lastAttacker is GunManager)
        {
            GunManager.Instance.ActivateCannonCardOneShot();
        }
    }


    private IEnumerator forceKill(Fish f, FishWSNetworkMessages.FishHit_Response response)
    {
        if (!response.killedByBot)
        {
            yield return f.StartCoroutine(f.DieWithFeedback(response, true));
        }
        else
        {
            yield return f.StartCoroutine(f.HitOrDieWithFeedback(response, true));
        }
    }

    private void RewardAttacker(object attacker, float prize)
    {
        if (attacker is GunManager)
        {
            Debug.Log($"BOMB WIN: ___ currentBalance: {Manager.Instance.balance} ___ prizeAmountLocal: {prize}");

            //Manager.Instance.balance += prize;
            //Manager.Instance.UpdateBalanceUI();

            //float mult = GetPlayerMultiplier();        // NEW
            ////Manager.Instance.balance += prize * mult;  // NEW
            //Manager.Instance.UpdateBalanceUI();

            //Manager.totalFishWinAmountPerInterval += prize * mult;
        }
        else if (attacker is BotController bot)
        {
            bot.AddBalance(prize);
        }
    }

    private void SpawnPowerupFX(Vector3 position, float scale, GameObject prefab, float fallbackLifetime)
    {
        if (prefab == null) return;

        var fx = Instantiate(prefab, position, Quaternion.identity);
        fx.transform.localScale = Vector3.one * scale;

        float lifetime = fallbackLifetime;

        var ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            float dur = main.duration;
#if UNITY_2018_3_OR_NEWER
            float maxLife = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startLifetime.constantMax
                : main.startLifetime.constant;
#else
        float maxLife = main.startLifetime.constantMax;
#endif
            lifetime = Mathf.Max(lifetime, dur + maxLife);
        }

        Destroy(fx, lifetime); // harmless if the prefab self-destroys
    }
    private float GetPlayerMultiplier()
    {
        return Time.time <= playerMultiplierExpireAt ? playerMultiplier : 1f;
    }

    private void ApplyMultiplierToShooter(object attacker, int mult, float duration)
    {
        if (attacker is GunManager)
        {
            playerMultiplier = Mathf.Max(1, mult);
            playerMultiplierExpireAt = Time.time + Mathf.Max(0.01f, duration);
            // Optional: popup/UI hook can go here
        }
    }


    private void OnDestroy()
    {
        Fish.OnFishKilledByPlayer -= HandleFishKilledByPlayer;
        Fish.OnFishKilledByBot -= HandleFishKilledByBot;
    }

    public void MoveAllFishForward(int stepCount)
    {
        MoveFishes(stepCount);
    }

    public void FishHitResponse(FishWSNetworkMessages.FishHit_Response response)
    {
        if (response.success)
        {
            if (response.killedByBomb)
            {
                Manager.Instance.balance += response.winAmount;
                Manager.Instance.UpdateBalanceUI();
            }
            else
            {
                FishInfo fishInfo = activeFishes.FirstOrDefault(x => x.fish.fishGuid == response.fishId);
                if (fishInfo != null)
                {
                    if (!response.killedByBot)
                    {
                        fishInfo.fish.TakeDamageByPlayer(response);
                    }
                    else
                    {
                        fishInfo.fish.TakeDamageByBot(response, null, 1, response.bulletCost);
                    }
                }
            }
        }
    }

    #region FishJobs

    // Called by FishMoverJobs when a fish hits its destination
    public void HandleArrivedFromMover(Transform t)
    {
        // existing end-of-path logic you already have in MoveFishes():
        var locked = LockManager.GetLockedFish();
        if (locked != null && locked.transform == t) LockManager.ClearLockedFish();
        if (t == null)
        {
            Debug.LogWarning("Fish GameObject reference is null, skipping...");
            return;
        }

        var fishComp = t.GetComponent<Fish>();
        if (fishComp != null)
        {
            fishComp.CallDeathEvent();
            DespawnFish(t.gameObject); // this will DeregisterFish via our new hook below
        }

        // Also remove it from activeFishes (your list), like you already do:
        for (int i = activeFishes.Count - 1; i >= 0; i--)
        {
            if (activeFishes[i].transform == t)
            {
                activeFishes.RemoveAt(i);
                break;
            }
        }
    }

    // Redirect resolver: reuse your helper that picked the closest movement point
    private Vector3 ResolveRedirect(Vector3 fromPos)
    {
        return GetClosestMovementPoint(fromPos);
    }


    #endregion



}
