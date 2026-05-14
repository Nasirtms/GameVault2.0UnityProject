using UnityEngine;
using Newtonsoft.Json;
using System;

public class SpinResultController : MonoBehaviour
{
    public static SpinResultController Instance;

    public event Action<BaseSpinResult> OnSpinResultReceived;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void HandleSpinResponse(string json)
    {
        BaseSpinResult result = null;

        switch (SceneManagement.currentGameName)
        {
            case "zombieparadise":
                result = JsonConvert.DeserializeObject<ZombieParadiseSpinResult>(json);
                break;

            case "goldgobblers":
                result = JsonConvert.DeserializeObject<GoldGobblersSpinResult>(json);
                break;

            case "biggerbassbonanza":
                result = JsonConvert.DeserializeObject<BiggerBassBonanzaSpinResult>(json);
                break;

            case "cashvault":
                result = JsonConvert.DeserializeObject<CashVaultSpinResult>(json);
                break;

            default:
                result = JsonConvert.DeserializeObject<SpinResult>(json);
                break;
        }

        if (result != null && result.success)
        {
            //Debug.Log("SlotSpinService.Instance.currentSlotMachine  : " + SlotSpinService.Instance.currentSlotMachine);
            //Debug.Log("SlotSpinService.Instance.currentSlotMachine InSpin : " + SlotSpinService.Instance.currentSlotMachine.InSpin);
            //Debug.Log("SlotSpinService.Instance.currentSlotMachine isStopButtonPressed : " + SlotSpinService.Instance.currentSlotMachine.isStopBtnPressed);

            if (SlotSpinService.Instance.currentSlotMachine.InSpin)
            {
                if (!SlotSpinService.Instance.currentSlotMachine.isStopBtnPressed)
                {
                    //Debug.Log("SlotSpinService.Instance.currentSlotMachine " + SlotSpinService.Instance.currentSlotMachine);
                    OnSpinResultReceived?.Invoke(result);
                }
            }
            Debug.Log("TotalWin : " + result.totalWin);
            //switch (SceneManagement.currentGameName)
            //{
            //    case "crazy7":
            //        if (CrazySevenSlotMachine.Instance.InSpin)
            //        {
            //            if (CrazySevenSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "cleopatra":
            //        if (CleopatraSlotMachine.Instance.InSpin)
            //        {
            //            if (CleopatraSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "monkeymadness":
            //        if (MonkeyMadnessSlotMachine.Instance.InSpin)
            //        {
            //            if (MonkeyMadnessSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "atomicmeltdown":
            //        if (AtomicMeltdownSlotMachine.Instance.InSpin)
            //        {
            //            if (AtomicMeltdownSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;

            //    case "doublejackpotbullseye":
            //        if (DoubleJackpotBullseyeSlotMachine.Instance.InSpin)
            //        {
            //            if (DoubleJackpotBullseyeSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "thegreenmachinedeluxe":
            //        if (TheGreenMachineDeluxeSlotMachine.Instance.InSpin)
            //        {
            //            if (TheGreenMachineDeluxeSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "tentimeswins":
            //        if (TenTimesWinsSlotMachine.Instance.InSpin)
            //        {
            //            if (TenTimesWinsSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "piratesofthecaribbean":
            //        if (PiratesOfTheCaribbeanSlotMachine.Instance.InSpin)
            //        {
            //            if (PiratesOfTheCaribbeanSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //            break;
            //    case "fruitmary":
            //        if (FruitMarySlotMachine.Instance.InSpin)
            //        {
            //            if (FruitMarySlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;

            //    case "zombieparadise":
            //        if (ZombieParadiseSlotMachine.Instance.InSpin)
            //        {
            //            if (ZombieParadiseSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;

            //    case "fruitslots":
            //        if (FruitSlotMachine.Instance.InSpin)
            //        {
            //            if (FruitSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "fruitparadise":
            //        if (FruitParadiseSlotMachine.Instance.InSpin)
            //        {
            //            if (FruitParadiseSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;

            //    case "starburstslots":
            //        if (StarBurstSlotsSlotMachine.Instance.InSpin)
            //        {
            //            if (StarBurstSlotsSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "quickhitvolcano":
            //        if (QuickHitVolcanoSlotMachine.Instance.InSpin)
            //        {

            //            if (QuickHitVolcanoSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }

            //        }
            //        break;

            //    case "biggerbassbonanza":
            //        if (BiggerBassBonanzaSlotMachine.Instance.inSpin)
            //        {
            //            if (BiggerBassBonanzaSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "vegas7":
            //        if (VegasSevenSlotMachine.Instance.InSpin)
            //        {
            //            if (VegasSevenSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "saharariches":
            //        if (SaharaRichesSlotMachine.Instance.InSpin)
            //        {
            //            if (SaharaRichesSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "wheeloffortune":
            //        if (WheelOfFortuneSlotMachine.Instance.InSpin)
            //        {
            //            if (WheelOfFortuneSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "dayofdead":
            //        if (DayOfDeadSlotMachine.Instance.InSpin)
            //        {
            //            if (DayOfDeadSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "pandafortune":
            //        if (PandaFortuneSlotMachine.Instance.InSpin)
            //        {
            //            if (PandaFortuneSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "goldendragon":
            //        if (GoldenDragonSlotMachine.Instance.InSpin)
            //        {
            //            if (GoldenDragonSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "flamecombo":
            //        if (FlameComboSlotMachine.Instance.InSpin)
            //        {
            //            if (FlameComboSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;

            //    case "goldgobblers":
            //        if (GoldGobblersSlotMachine.Instance.InSpin)
            //        {
            //            if (GoldGobblersSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;

            //    case "superbomb":
            //        if (SuperBombSlotMachine.Instance.InSpin)
            //        {
            //            if (SuperBombSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "imperialdiamond":
            //        if (ImperialDiamondSlotMachine.Instance.InSpin)
            //        {
            //            if (ImperialDiamondSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "cashmachine":
            //        if (CashMachineSlotMachine.Instance.InSpin)
            //        {
            //            if (CashMachineSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "comeoncash":
            //        if (ComeOnCashSlotMachine.Instance.InSpin)
            //        {
            //            if (ComeOnCashSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "goldrushgus":
            //        if (GoldRushGusSlotMachine.Instance.InSpin)
            //        {
            //            if (GoldRushGusSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "cashvault":
            //        if (CashVaultSlotMachine.Instance.InSpin)
            //        {
            //            if (CashVaultSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "stickypiggy":
            //        if (StickyPiggySlotMachine.Instance.InSpin)
            //        {
            //            if (StickyPiggySlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "irishpotluck":
            //        if (IrishPotLuckSlotMachine.Instance.InSpin)
            //        {
            //            if (IrishPotLuckSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "wildxreel":
            //        if (WildXReelSlotMachine.Instance.InSpin)
            //        {
            //            if (WildXReelSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "wildxtrio":
            //        if (WildXTrioSlotMachine.Instance.InSpin)
            //        {
            //            if (WildXTrioSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    case "redhottripple":
            //        if (RedHotTrippleSlotMachine.Instance.InSpin)
            //        {
            //            if (RedHotTrippleSlotMachine.Instance.isStopBtnPressed == false)
            //            {
            //                OnSpinResultReceived?.Invoke(result);
            //            }
            //        }
            //        break;
            //    default:
            //        break;
            //}
        }
        else
        {
            Debug.Log("Error :  SpinResult parse error or unsuccessful.");
        }
    }
}