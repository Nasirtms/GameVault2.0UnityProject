#if UNITY_EDITOR
#define ENABLE_DEBUG_LOGGING
#endif

using UnityEngine;

public class Slot : MonoBehaviour
{
    [Header("Slot Configuration")]
    [SerializeField] private SlotMachineSettings settings; // Optional - will auto-get from SlotMachine.Instance if null

    [Header("References")]
    [SerializeField] private SpriteRenderer symbolRenderer;

    // Cached settings values for performance
    private float symbolScaleX;
    private float symbolScaleY;

    // Current symbol data
    private Sprite currentSymbol;
    private int symbolIndex = -1;

    // Properties
    public Sprite CurrentSymbol => currentSymbol;
    public int SymbolIndex => symbolIndex;
    public bool HasSymbol => currentSymbol != null;
    public SpriteRenderer SymbolRenderer => symbolRenderer;

    private void Awake()
    {
        symbolRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ValidateSetup();
        CacheSettings();
    }

    /// <summary>
    /// Validates the slot setup
    /// </summary>
    private void ValidateSetup()
    {
        // Auto-get settings from SlotMachine.Instance if not assigned
        if (settings == null)
        {
            if (SlotMachine.Instance != null && SlotMachine.Instance.settings != null)
            {
                settings = SlotMachine.Instance.settings;
                //#if UNITY_EDITOR
                //                Debug.Log($"Slot {gameObject.name}: Auto-assigned settings from SlotMachine.Instance");
                //#endif
            }
            else
            {
                //#if UNITY_EDITOR
                //                Debug.LogError($"Slot {gameObject.name}: No SlotMachineSettings assigned and SlotMachine.Instance not available!");
                //#endif
                return;
            }
        }

        if (symbolRenderer == null)
        {
            //#if UNITY_EDITOR
            //            Debug.LogError($"Slot {gameObject.name}: No SpriteRenderer found! Make sure this GameObject has a SpriteRenderer component.");
            //#endif
            return;
        }

        //#if UNITY_EDITOR
        //        Debug.Log($"Slot {gameObject.name}: Setup validated successfully");
        //#endif
    }

    /// <summary>
    /// Caches all settings values for performance optimization
    /// </summary>
    private void CacheSettings()
    {
        if (settings == null) return;

        symbolScaleX = settings.slotSettings.SymbolScaleX;
        symbolScaleY = settings.slotSettings.SymbolScaleY;
    }

    /// <summary>
    /// Sets the symbol for this slot
    /// </summary>
    /// <param name="symbol">The sprite to display</param>
    /// <param name="index">The index of the symbol in the symbol list</param>
    public void SetSymbol(Sprite symbol, int index = -1)
    {
        // Always ensure we have a SpriteRenderer component
        if (symbolRenderer == null)
        {
            symbolRenderer = GetComponent<SpriteRenderer>();
        }

        if (symbolRenderer == null)
        {
#if UNITY_EDITOR
            Debug.LogError($"Slot {gameObject.name}: Cannot set symbol - no SpriteRenderer assigned!");
#endif
            return;
        }

        currentSymbol = symbol;
        symbolIndex = index;
        symbolRenderer.sprite = symbol;

        // Apply visual settings
        ApplyVisualSettings();

        //#if UNITY_EDITOR
        //        Debug.Log($"Slot {gameObject.name}: Symbol set to '{symbol?.name ?? "null"}' at index {index}");
        //#endif
    }

    /// <summary>
    /// Applies visual settings to this slot
    /// </summary>
    public void ApplyVisualSettings()
    {
        if (symbolRenderer == null) return;

        // Apply scale without affecting position
        Vector3 scale = new Vector3(symbolScaleX, symbolScaleY, 1f);
        symbolRenderer.transform.localScale = scale;
    }

    /// <summary>
    /// Sets the symbol scale for this slot
    /// </summary>
    /// <param name="scaleX">X scale value</param>
    /// <param name="scaleY">Y scale value</param>
    public void SetSymbolScale(float scaleX, float scaleY)
    {
        // Update local cached values
        symbolScaleX = scaleX;
        symbolScaleY = scaleY;

        ApplyVisualSettings();
    }


}
