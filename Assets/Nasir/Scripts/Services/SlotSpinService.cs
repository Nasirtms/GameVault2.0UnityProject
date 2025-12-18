using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SlotSpinService : MonoBehaviour
{
    public static SlotSpinService Instance { get; private set; }
    public string GameScenName = "";

    private BaseSlotMachine currentSlotMachine;
    private string currentRequestId;
    //public bool isCoinUpdaterOrNot = false;

    public bool sendCorrectAPI;
    public bool is2FreSpin;
    public bool is3FreSpin;
    public bool is4FreSpin;
    public bool isSaharaRiches;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void Spin(float betAmount)
    {
        // Get the slot machine based on the current scene
        currentSlotMachine = GameSlotRegistry.GetMachine(SceneManagement.currentGameName);
        ClickSessionManager.Instance?.ResetInactivityTimer();
        if (currentSlotMachine == null)
        {
            Debug.LogError($"❌ Slot machine not registered for scene: {SceneManagement.currentGameName}");
            return;
        }

        currentSlotMachine.ClearPaylines();

        StartCoroutine(CallSlotSpinApi(betAmount));
    }

    private IEnumerator CallSlotSpinApi(float betAmount)
    {
        currentRequestId = Guid.NewGuid().ToString();
        Debug.Log($"🎰 Calling Slot Spin API with betAmount: {betAmount}, requestId: {currentRequestId}");

        string sceneName = SceneManagement.currentGameName;
        var requestData = new object();

        switch (sceneName)
        {
            case "tentimeswins":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    lineMultiplier = TenTimesWinsUIManager.Instance.gameObject.GetComponent<TenTimesWinsBetController>().GetCurrentMultiplier()
                };
                Debug.Log("Line Multiplier : " + TenTimesWinsUIManager.Instance.gameObject.GetComponent<TenTimesWinsBetController>().GetCurrentMultiplier());
                break;
            case "quickhitvolcano":
                Debug.Log("Quick Hit Request Body: " + QuickHitVolcanoSlotMachine.Instance.isFreeGame);
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    IsFreeSpin = QuickHitVolcanoSlotMachine.Instance.isFreeGame
                };
                break;
            case "cleopatra":
                //Debug.Log("cleopatra Request Body: " + CleopatraSlotMachine.Instance.isFreeGame);
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    IsFreeSpin = CleopatraSlotMachine.Instance.isFreeGame
                    //IsFreeSpin = true
                };
                break;
            case "doublejackpotbullseye":
                Debug.Log("Bullseye Request Body: " + DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame);
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    IsFreeSpin = DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame
                    //IsFreeSpin = true
                };
                break;
            case "biggerbassbonanza":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = BiggerBassBonanzaSlotMachine.Instance.isFreeGame,
                    wildMultiplier = BiggerBassBonanzaSlotMachine.Instance.wildMultipliers[BiggerBassBonanzaSlotMachine.Instance.retriggerCount]
                };
                break;
            case "vegas7":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = VegasSevenSlotMachine.Instance.isFreeGame,
                    ForceBonusSymbols = VegasSevenSlotMachine.Instance.ForceBonusSymbols,
                    //scatterMultiplier = VegasSevenSlotMachine.Instance.chillyMultiplyer[VegasSevenSlotMachine.Instance.retriggerCount]
                    scatterMultiplier = VegasSevenSlotMachine.Instance.retriggerCount
                };
                break;
            case "fruitmary":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = FruitMarySlotMachine.Instance.isFreeGame,
                    isBonusGame = false,
                    forceWildPayline = FruitMarySlotMachine.Instance.forceWildPayline
                };
                break;
            case "fruitparadise":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = FruitParadiseSlotMachine.Instance.isFreeGame
                };
                break;
            case "atomicmeltdown":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = AtomicMeltdownSlotMachine.Instance.isFreeGame
                };
                break;
            case "crazy7":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = CrazySevenSlotMachine.Instance.isFreeGame
                };
                break;
            case "fruitslots":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = FruitSlotMachine.Instance.isFreeGame
                };
                break;
            case "piratesofthecaribbean":
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGame
                };
                break;
            case "starburstslots":
                requestData = new
                {
                    userId = UserManager.Instance.UserId,
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    IsFreeSpin = StarBurstSlotsSlotMachine.Instance.isFreeGame,
                    isReelTwoStopped = StarBurstSlotsSlotMachine.Instance.lockedReels[1],
                    isReelThreeStopped = StarBurstSlotsSlotMachine.Instance.lockedReels[2],
                    isReelFourStopped = StarBurstSlotsSlotMachine.Instance.lockedReels[3],

                    //IsFreeSpin = true
                };
                break;
            case "dayofdead":
                requestData = new
                {
                    userId = UserManager.Instance.UserId,
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    frozenWildReels = DayOfDeadSlotMachine.Instance.freeSpinWildReel,
                    IsFreeSpin = DayOfDeadSlotMachine.Instance.isFreeGame,
                    FreeGameWildCount = DayOfDeadFreeSpinController.Instance.topbar.tokens.Count,
                };
                break;

            case "thegreenmachinedeluxe":
                requestData = new
                {
                    userId = UserManager.Instance.UserId,
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    IsFreeSpin = TheGreenMachineDeluxeSlotMachine.Instance.isFreeGame
                };
                break;
            case "zombieparadise":
                requestData = new
                {
                    userId = UserManager.Instance.UserId,
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    IsFreeSpin = ZombieParadiseSlotMachine.Instance.isFreeGame
                };
                break;
            case "saharariches":
                // Create the request data
                requestData = new
                {
                    betAmount = betAmount,
                    userId = UserManager.Instance.UserId,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = SaharaRichesSlotMachine.Instance.isFreeGame,
                    lockedCashCollectSlots = SaharaRichesSlotMachine.Instance.lockedSlots
                };

                Debug.Log("Cash Collect Request Body: " + JsonUtility.ToJson(requestData, true));
                break;
            case "goldendragon":
                // Create the request data
                requestData = new
                {
                    betAmount = betAmount,
                    userId = UserManager.Instance.UserId,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    isFreeSpin = GoldenDragonSlotMachine.Instance.isFreeGame,
                    isMiniGame = GoldenDragonSlotMachine.Instance.isMiniGame,
                };

                Debug.Log("Cash Collect Request Body: " + JsonUtility.ToJson(requestData, true));
                break;
            case "pandafortune":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = PandaFortuneSlotMachine.Instance.isFreeGame,
                    freeSpinCurrentSpin = PandaFortuneSlotMachine.Instance.frozenIndexThisSpin,
                    freeSpinType = "first",
                    frozenColumns = PandaFortuneSlotMachine.Instance.frozenColumns.ToArray()
                };
                break;
            case "flamecombo":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = FlameComboSlotMachine.Instance.isFreeGame,
                };
                break;
            case "superbomb":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = SuperBombSlotMachine.Instance.isFreeGame,
                };
                break;
            case "cashmachine":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = CashMachineSlotMachine.Instance.isFreeGame,
                    isHighStake = !CashMachineSlotMachine.Instance.isHighStake
                };
                break;
            default:
                requestData = new
                {
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID
                };
                break;
        }

        string jsonData = JsonConvert.SerializeObject(requestData);
        currentSlotMachine.Spin();

        //if (sendCorrectAPI)
        //{
        //    ApiEndpoints.slotGameName = SceneManagement.currentGameName;
        //}
        //else
        //{
        //    ApiEndpoints.slotGameName = "";
        //}

        ApiEndpoints.slotGameName = SceneManagement.currentGameName;

        Debug.Log("Game Name: " + ApiEndpoints.slotGameName);
        //using (UnityWebRequest www = new UnityWebRequest(ApiEndpoints.slotGameSpin, "POST"))

        string spinApiUrl;
        if (is2FreSpin)
        {
            spinApiUrl = ApiEndpoints.slot2FreeSpin;
        }
        else if (is3FreSpin)
        {
            spinApiUrl = ApiEndpoints.slot3FreeSpin;
        }
        else if (is4FreSpin)
        {
            spinApiUrl = ApiEndpoints.slot4FreeSpin;
        }
        else
        {
            spinApiUrl = ApiEndpoints.slotGameSpin;
        }

        if (isSaharaRiches)
        {
            spinApiUrl = ApiEndpoints.saharaRichesTest;
        }
        using (UnityWebRequest www = new UnityWebRequest(spinApiUrl, "POST"))
        {
            //Debug.Log("📤 Starting Slot Spin API Call...");
            //Debug.Log("📡 Endpoint: " + ApiEndpoints.slotGameSpin);
            //Debug.Log("🔑 Current AuthToken: " + ApiEndpoints.AuthToken);
            Debug.Log("📦 Nasir_ Request Body: " + jsonData);
            Debug.Log("🎯 Bet Amount: " + betAmount);

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            // Send the request
            yield return www.SendWebRequest();

            //Debug.Log("🛰️ Request completed.");
            //Debug.Log("📥 Response Code: " + www.responseCode);
            //Debug.Log("🔁 Result: " + www.result);
            if (www.responseCode == 401)
            {
                // 1. Pass the coroutine function reference as a new argument
                yield return ApiEndpoints.CheckApiResponse(
                    www,
                    spinApiUrl,
                    jsonData,
                    "POST",
                    () => CallSlotSpinApi(betAmount) // <-- The function to be saved
                );
                yield break; // Stops execution here
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                //Debug.LogError("❌ Slot spin failed: " + www.error);
                AddCurrentBetCoinIntoUserCoin();
                currentSlotMachine.StopSpinGettingError();
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
                yield break;
            }

            string responseText = www.downloadHandler.text;
            SpinResult parsed = JsonUtility.FromJson<SpinResult>(responseText);

            Debug.Log("✅ Response Received 1 : " + parsed);
            Debug.Log("✅ New Balance : " + parsed.newBalance);
            Debug.Log("✅ Nasir_ Response Received 3 : " + responseText);

            try
            {
                if (string.IsNullOrEmpty(responseText))
                {
                    AddCurrentBetCoinIntoUserCoin();
                    Debug.LogError("❌ Response text is null or empty.");
                    CasinoUIManager.Instance.ShowErrorCanvas(1, "Empty server response");
                    yield break;
                }

                BaseSpinResult spinResult = sceneName.Equals("zombieparadise") ? ParseResponseZombieParadise(responseText) : ParseResponseNormal(responseText);

                if (spinResult == null)
                {
                    AddCurrentBetCoinIntoUserCoin();
                    Debug.LogError("❌ Failed to deserialize spin response.");
                    CasinoUIManager.Instance.ShowErrorCanvas(1, "Invalid response format");
                    currentSlotMachine.StopSpinGettingError();
                    yield break;
                }

                if (!string.IsNullOrEmpty(spinResult.requestId) && spinResult.requestId != currentRequestId)
                {
                    AddCurrentBetCoinIntoUserCoin();
                    Debug.LogWarning($"⚠️ Received outdated response (requestId: {spinResult.requestId}). Ignoring.");
                    Debug.Log("\nResponse ID: " + spinResult.requestId + "\nRequest ID: " + currentRequestId);
                    currentSlotMachine.StopSpinGettingError();
                    yield break;
                }

                if (SpinResultController.Instance != null)
                {
                    switch (sceneName)
                    {
                        case "zombieparadise":
                            // no reels in ZombieParadiseSpinResult
                            break;

                        //case "biggerbassbonanza":
                        //    if (spinResult is BiggerBassBonanzaSpinResult normalSpin)
                        //    {
                        //        currentSlotMachine.spinSymbolMatrix = normalSpin.reels;
                        //    }
                        //    break;

                        default:
                            if (spinResult is SpinResult normalSpin)
                            {
                                currentSlotMachine.spinSymbolMatrix = normalSpin.reels;
                            }
                            break;
                    }

                    SpinResultController.Instance.HandleSpinResponse(responseText);
                }
                else
                {
                    AddCurrentBetCoinIntoUserCoin();
                    Debug.LogError("❌ SpinResultController.Instance is null.");
                }

            }
            catch (Exception ex)
            {
                AddCurrentBetCoinIntoUserCoin();
                Debug.LogError("❌ Exception while parsing spin result: " + ex.Message);
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
                currentSlotMachine.StopSpinGettingError();
            }
        }
    }

    private void AddCurrentBetCoinIntoUserCoin()
    {
        string gameName = SceneManagement.currentGameName;

        if (currentSlotMachine.isFreeGame)
        {
            Debug.Log("Network Error: Free Spin Reverted");
            return;
        }

        /* switch (gameName)
         {
             case "cleopatra":
                 if (currentSlotMachine.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "crazy7":
                 if (CrazySevenSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "monkeymadness":
                 break;
             case "atomicmeltdown":
                 if (AtomicMeltdownSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "tentimeswins":
                 break;
             case "quickhitvolcano":
                 if (QuickHitVolcanoSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "fruitslots":
                 if (FruitSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "thegreenmachinedeluxe":
                 if (TheGreenMachineDeluxeSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "piratesofthecaribbean":
                 if (PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "fruitparadise":
                 if (FruitParadiseSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "doublejackpotbullseye":
                 if (DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             case "saharariches":
                 if (SaharaRichesSlotMachine.Instance.isFreeGame)
                 {
                     Debug.Log("Network Error: Free Spin Reverted");
                     return;
                 }
                 break;
             default:
                 break;
         }*/

        var userManager = UserManager.Instance;
        float coin = userManager.currentBetAmount + userManager.Coins;
        GameBetServices.Instance.UpdateCoins(coin);

        Debug.Log("Network Error: Bet Amount Returned - " + UserManager.Instance.currentBetAmount);
    }

    private SpinResult ParseResponseNormal(string responseText)
    {
        return JsonConvert.DeserializeObject<SpinResult>(responseText);
    }

    private ZombieParadiseSpinResult ParseResponseZombieParadise(string responseText)
    {
        return JsonConvert.DeserializeObject<ZombieParadiseSpinResult>(responseText);
    }
}
