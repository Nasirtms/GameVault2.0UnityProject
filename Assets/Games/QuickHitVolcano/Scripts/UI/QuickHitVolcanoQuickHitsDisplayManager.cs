using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

#region Support Classes

[System.Serializable]
public class QuickHitData
{
    public int value;
    public Sprite icon;
    public float hitMultiplier;
}

[System.Serializable]
public class QuickHitPair
{
    public QuickHitData left;
    public QuickHitData right;
}

#endregion

public class QuickHitVolcanoQuickHitsDisplayManager : MonoBehaviour
{
    #region Variables

    [Header("UI References")]
    [SerializeField] private Image leftIcon;
    [SerializeField] private TextMeshProUGUI leftAmount;

    [SerializeField] private Image rightIcon;
    [SerializeField] private TextMeshProUGUI rightAmount;

    [Header("Quick Hit Pairs")]
    [SerializeField] private List<QuickHitPair> quickHitPairs;

    private QuickHitVolcanoBetController quickHitVolcanoBetController;

    private QuickHitPair selectedPair;

    #endregion

    #region Unity Methods

    private void Start()
    {
        quickHitVolcanoBetController = GetComponent<QuickHitVolcanoBetController>();
        QuickHitVolcanoBetController.OnBetValueChanged += HandleBetChanged;
        SelectRandomPair();
        UpdateDisplay();
    }

    #endregion

    #region Quick Hit

    private void SelectRandomPair()
    {
        if (quickHitPairs == null || quickHitPairs.Count == 0) return;
        selectedPair = quickHitPairs[Random.Range(0, quickHitPairs.Count)];
    }

    private void UpdateDisplay()
    {
        if (selectedPair == null || quickHitVolcanoBetController == null) return;

        float betValue = quickHitVolcanoBetController.GetCurrentBet();

        // LEFT
        leftIcon.sprite = selectedPair.left.icon;
        float leftAmountValue = selectedPair.left.hitMultiplier * betValue;
        leftAmount.text = leftAmountValue.ToString("N2");

        // RIGHT
        rightIcon.sprite = selectedPair.right.icon;
        float rightAmountValue = selectedPair.right.hitMultiplier * betValue;
        rightAmount.text = rightAmountValue.ToString("N2");
    }

    private void HandleBetChanged()
    {
        UpdateDisplay();
    }

    #endregion
}