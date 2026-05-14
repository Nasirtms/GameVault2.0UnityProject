using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class SlotSpinService : MonoBehaviour
{
    public static SlotSpinService Instance { get; private set; }
    public string GameScenName = "";

    public BaseSlotMachine currentSlotMachine;
    private string currentRequestId;
    //public bool isCoinUpdaterOrNot = false;

    public bool sendCorrectAPI;
    public bool is2FreSpin;
    public bool is3FreSpin;
    public bool is4FreSpin;
    public bool isNewSlotGame;//Any NewSlotGame Test(which don't have an api)
    public float newBalance;
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
        Debug.Log("SlotSpinService.Instance.currentSlotMachine InSpin : " + currentSlotMachine.InSpin);



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
        //Debug.Log($"🎰 Calling Slot Spin API with betAmount: {betAmount}, requestId: {currentRequestId}");

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

            case "goldgobblers":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    IsFreeSpin = GoldGobblersSlotMachine.Instance.isFreeGame,
                    red = GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted,
                    green = GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted,
                    blue = GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted
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
                break;

            case "pandafortune":
                int fs;
                string freeSpinType;
                if (PandaFortuneSlotMachine.Instance.isFreeGame && !PandaFortuneSlotMachine.Instance.firstFreeSpin)
                {
                    fs = PandaFortuneSlotMachine.Instance.frozenIndexThisSpin;
                }
                else if (PandaFortuneSlotMachine.Instance.isFreeGame && PandaFortuneSlotMachine.Instance.firstFreeSpin)
                {
                    fs = 5;
                }
                else
                {
                    fs = -1;
                }
                if (PandaFortuneSlotMachine.Instance.isFreeGame)
                {
                    if (PandaFortuneSlotMachine.Instance.isFreeGameTwo) freeSpinType = "second";
                    else freeSpinType = "first";
                }
                else
                {
                    freeSpinType = "";
                }
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = PandaFortuneSlotMachine.Instance.isFreeGame,
                    freeSpinCurrentSpin = fs,
                    freeSpinType = freeSpinType,
                    frozenColumns = PandaFortuneSlotMachine.Instance.frozenColumns.ToArray(),
                    isfreespintwo = PandaFortuneSlotMachine.Instance.isFreeGameTwo
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
                List<int> lockedReels = new List<int>();
                if (SuperBombSlotMachine.Instance.lockedReels[1])
                {
                    lockedReels.Add(2);
                }
                if (SuperBombSlotMachine.Instance.lockedReels[2])
                {
                    lockedReels.Add(3);
                }
                if (SuperBombSlotMachine.Instance.lockedReels[3])
                {
                    lockedReels.Add(4);
                }
                if (!SuperBombSlotMachine.Instance.lockedReels[3] && !SuperBombSlotMachine.Instance.lockedReels[2] && !SuperBombSlotMachine.Instance.lockedReels[1])
                {
                    lockedReels.Clear();
                }
                requestData = new
                {
                    userId = UserManager.Instance.UserId,
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    gameId = SceneManagement.currentGameID,
                    IsFreeSpin = SuperBombSlotMachine.Instance.isFreeGame,
                    LockedReels = lockedReels.ToArray()
                };
                break;

            case "imperialdiamond":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                };
                break;

            case "cashmachine":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = CashMachineSlotMachine.Instance.isFreeGame,
                    isHighStake = !CashMachineSlotMachine.Instance.isHighStake,
                    LockedReels = CashMachineSlotMachine.Instance.LockedReels.ToArray()

                };
                break;

            case "comeoncash":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = ComeOnCashSlotMachine.Instance.isFreeGame,
                    isHighStake = ComeOnCashSlotMachine.Instance.isHighStake,
                    LockedReels = ComeOnCashSlotMachine.Instance.LockedReels.ToArray(),
                    BonusGame = ComeOnCashSlotMachine.Instance.isBonusGame,
                    IsTakeOffer = ComeOnCashSlotMachine.Instance.isTakeOffer
                };
                break;

            case "goldrushgus":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = GoldRushGusSlotMachine.Instance.isFreeGame,
                    freeSpinMultiplier = GoldRushGusSlotMachine.Instance.freeSpinMultiplier,
                    respinReels = GoldRushGusSlotMachine.Instance.reSpinReels,
                };
                break;

             case "cashvault":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = CashVaultSlotMachine.Instance.isFreeGame,
                };
                break;

            case "stickypiggy":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = StickyPiggySlotMachine.Instance.isFreeGame,
                    //stickyPiggyWilds = new List<string>()
                };
                break;
            case "irishpotluck":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = IrishPotLuckSlotMachine.Instance.isFreeGame,
                };
                break;
            case "wildxreel":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                };
                break;
            case "wildxtrio":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                };
                break;
            case "redhottripple":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = RedHotTrippleSlotMachine.Instance.isFreeGame,
                };
                break;

            case "stinkinrich":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    betAmount = betAmount,
                    requestId = currentRequestId,
                    IsFreeSpin = StinkinRichSlotMachine.Instance.isFreeGame
                };
                break;
            case "goldenwheel":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = GoldenWheelSlotMachine.Instance.isFreeGame,
                    isHighStake = GoldenWheelSlotMachine.Instance.isHighStake
                };
                break;
            case "lifeofluxury":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = LifeOfLuxurySlotMachine.Instance.isFreeGame,
                    freeSpinState = new FreeSpinState
                    {
                        remainingSpins = LifeOfLuxurySlotMachine.Instance.remainingFreeSpins,
                        lineMultiplier = LifeOfLuxurySlotMachine.Instance.freeSpinLineMultiplier
                    }
                };
                break;
            case "invadersplanetmoolah":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = InvadersPlanetMoolahSlotMachine.Instance.isFreeGame,
                };
                break;
            case "wildbuffalo":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = WildBuffaloSlotMachine.Instance.isFreeGame,
                };
                break;
            case "richlittlepiggies":
                requestData = new
                {
                    gameId = SceneManagement.currentGameID,
                    requestId = currentRequestId,
                    betAmount = betAmount,
                    IsFreeSpin = RichLittlePiggiesSlotMachine.Instance.isFreeGame,
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

        //Debug.Log("Game Name: " + ApiEndpoints.slotGameName);
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

        if (isNewSlotGame)
        {
            spinApiUrl = ApiEndpoints.newGameTest;
        }
        using (UnityWebRequest www = new UnityWebRequest(spinApiUrl, "POST"))
        {
            //Debug.Log("📤 Starting Slot Spin API Call...");
            Debug.Log("📡 Endpoint: " + ApiEndpoints.slotGameSpin);
            //Debug.Log("🔑 Current AuthToken: " + ApiEndpoints.AuthToken);
            Debug.Log("📦 Nasir_ Request Body: " + jsonData);
            //Debug.Log("🎯 Bet Amount: " + betAmount);

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
            newBalance = parsed.newBalance;
            //Debug.Log("✅ Response Received 1 : " + parsed);
            //Debug.Log("✅ New Balance : " + parsed.newBalance);
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
                BaseSpinResult spinResult;
                switch (sceneName.ToLowerInvariant())
                {
                    case "zombieparadise":
                        spinResult = ParseResponseZombieParadise(responseText);
                        break;

                    case "goldgobblers":
                        spinResult = ParseResponseGoldGobblers(responseText);
                        break;

                    case "cashvault":
                        spinResult = ParseResponseCashVault(responseText);
                        break;

                    default:
                        spinResult = ParseResponseNormal(responseText);
                        break;
                }
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
                            break;
                        case "cashvault":
                            break;

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
    [System.Serializable]
    public class FreeSpinState
    {
        public int remainingSpins;
        public int lineMultiplier;
    }
    public void AddCurrentBetCoinIntoUserCoin()
    {
        string gameName = SceneManagement.currentGameName;

        if (!gameName.Contains("headsntails"))
        {
            if (currentSlotMachine.isFreeGame)
            {
                Debug.Log("Network Error: Free Spin Reverted");
                return;
            }
        }

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
    private GoldGobblersSpinResult ParseResponseGoldGobblers(string responseText)
    {
        return JsonConvert.DeserializeObject<GoldGobblersSpinResult>(responseText);
    }
    private CashVaultSpinResult ParseResponseCashVault(string responseText)
    {
        return JsonConvert.DeserializeObject<CashVaultSpinResult>(responseText);
    }
}
