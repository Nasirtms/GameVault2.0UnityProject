using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Bridge between your existing FishManager and the backend API
/// Handles all backend communication automatically
/// </summary>
public class MermaidBackendBridge : MonoBehaviour
{
    [Header("References")]
    public FishManager fishManager; // Drag your FishManager here
    
    [Header("Game Settings")]
    public float betAmountPerShot = 1f;
    public int refreshWeightsEveryXFish = 20;
    
    [Header("Debug")]
    public bool showLogs = true;
    
    // Internal data
    private Dictionary<string, FishProfile> fishProfiles = new Dictionary<string, FishProfile>();
    private int totalWeight = 0;
    private int fishCaughtCount = 0;
    private bool profilesLoaded = false;
    private float currentBalance = 0f;
    private float globalDamageMultiplier = 1.0f; // Backend-controlled damage scaling
    private float lastKnownBalance = 0f; // Track balance changes for optimization
    public static MermaidBackendBridge instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
     
        LoadBackendProfiles(40);
        LoadInitialBalance();
    }
    
    public void LoadInitialBalance()
    {
        // Don't force balance from backend - let Unity manage player's actual balance
        // Backend only tracks for RTP calculation purposes
        if (showLogs)
            Debug.Log($"💰 Backend initialized (Unity manages player balance)");
    }

    /// <summary>
    /// Load fish spawn weights and properties from backend
    /// </summary>
    public void LoadBackendProfiles(int numberToSpawn, float betAmount = 0, float winAmount = 0, bool isBonus = false, List<string> bonusFishesName = null)
    {
     
        if (MermaidAPIManager.Instance == null)
        {
            Debug.LogError("❌ MermaidAPIManager not found! Make sure it exists in the scene.");
            return;
        }
        Debug.Log("Number To Spawn " + numberToSpawn + " Total Bet Amount " + betAmount + " total Win Amount " + winAmount + " coins " + Manager.Instance.balance);
        profilesLoaded = false;
        StartCoroutine(MermaidAPIManager.Instance.GetSpawnProfile(numberToSpawn, betAmount, winAmount ,
            onSuccess: (profiles,rtb) =>
            {
                fishProfiles.Clear();
                totalWeight = 0;
                Debug.Log("CurrentRtb from backend " + rtb);
                Manager.targetRTBFromBackend = rtb;
                
                foreach (var profile in profiles)
                {
                    fishProfiles[profile.fishName] = profile;
                    totalWeight += profile.spawnWeight;
                    
                    // Store global damage multiplier from first profile (same for all fish)
                    if (globalDamageMultiplier == 1.0f)
                    {
                        globalDamageMultiplier = profile.damageMultiplier;
                    }
                }
                
                profilesLoaded = true;

                List<string> uniqueNames = profiles
                .Where(f => !string.IsNullOrWhiteSpace(f.fishName))
                .Select(f => f.fishName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

                Debug.Log("====Total Unique Profiles==== " + uniqueNames.Count);

                if (showLogs)
                {
                    Debug.Log($"✅ Loaded {profiles.Length} fish profiles. Total weight: {totalWeight}");
                    Debug.Log($"⚔️ <color=yellow>DAMAGE MULTIPLIER: {globalDamageMultiplier:F2}x</color> (RTP-based)");
                    Debug.Log($"🎯 <color=green>BACKEND READY! Fish spawning will now use RTP-based weights.</color>");
                    
                    // Show backend fish profiles for debugging
                    Debug.Log($"🐟 <color=cyan>BACKEND FISH PROFILES:</color>");
                    for (int i = 0; i < Mathf.Min(profiles.Length, 10); i++) // Show first 10
                    {
                        var profile = profiles[i];
                        Debug.Log($"   {i + 1}. {profile.fishName} (Weight: {profile.spawnWeight}, Health: {profile.adjustedHealth}, Prize: {profile.adjustedPrize})");
                    }
                    if (profiles.Length > 10)
                    {
                        Debug.Log($"   ... and {profiles.Length - 10} more fish profiles");
                    }
                }
            },
            onError: (error) =>
            {
                Debug.LogError($"❌ Failed to load fish profiles: {error}");
                profilesLoaded = false;
            }, isBonus, 
               bonusFishesName
        ));
    }
    
    /// <summary>
    /// Get a random fish name based on backend spawn weights
    /// FishManager calls this when spawning fish
    /// </summary>
    public string GetWeightedRandomFishName()
    {
        if (!profilesLoaded || fishProfiles.Count == 0)
        {
            if (showLogs)
                Debug.LogWarning("⚠️ Backend profiles not loaded, using fallback");
            return "fish-1"; // Fallback
        }
        
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (var profile in fishProfiles.Values)
        {
            currentWeight += profile.spawnWeight;
            if (randomValue < currentWeight)
            {
                return profile.fishName;
            }
        }
        
        // Fallback
        return "fish-1";
    }
    
    /// <summary>
    /// Get fish health and prize from backend
    /// FishManager calls this when spawning fish
    /// </summary>
    public void GetFishProperties(string fishName, out int health, out float prize)
    {
        if (profilesLoaded && fishProfiles.ContainsKey(fishName))
        {
            FishProfile profile = fishProfiles[fishName];
            health = profile.adjustedHealth;
            prize = profile.adjustedPrize;
            
            if (showLogs)
                Debug.Log($"🐟 <color=green>Backend properties for {fishName}:</color> Health={health}, Prize={prize}");
        }
        else
        {
            // Fallback defaults
            health = 1;
            prize = 10f;
            
            if (showLogs)
                Debug.LogWarning($"⚠️ <color=orange>No backend data for {fishName}, using defaults</color>");
        }
    }
    
    /// <summary>
    /// Notify backend when fish is caught
    /// Call this when fish dies
    /// </summary>
    public void OnFishCaught(string fishName, float prizeAmount)
    {
        fishCaughtCount++;
        
        if (showLogs)
            Debug.Log($"🎣 Fish #{fishCaughtCount} caught: {fishName}, Prize: {prizeAmount}");
        
        if (MermaidAPIManager.Instance == null)
        {
            Debug.LogError("❌ MermaidAPIManager not found!");
            return;
        }
        
        // Get actual bet amount from Manager
        float actualBet = Manager.Instance != null ? Manager.Instance.betOptions[Manager.Instance.betIndex] : betAmountPerShot;
        
        StartCoroutine(MermaidAPIManager.Instance.CatchFish(
            fishName: fishName,
            betAmount: actualBet,
            payout: prizeAmount,
            win: true,
            onSuccess: (response) =>
            {
                currentBalance = response.newBalance;
                
                // ✅ Backend tracks balance for RTP calculation only
                // Unity handles local balance - no forced sync
                if (showLogs)
                {
                    Debug.Log($"📊 Backend tracked balance: {response.newBalance:F2} (for RTP calculation only)");
                    Debug.Log($"💰 Unity local balance: {Manager.Instance?.balance:F2} (player sees this)");
                }
                
                if (showLogs)
                {
                    Debug.Log($"✅ New Balance: {response.newBalance:F2}");
                    Debug.Log($"📊 RTP: {response.currentRtp:P2}");
                    Debug.Log($"💰 Balance Change: {response.balanceChange:+0.00;-0.00}");
                }
                
            },
            onError: (error) =>
            {
                Debug.LogError($"❌ Failed to process catch: {error}");
            }
        ));
        
        // Auto-refresh spawn weights every X fish
        //if (fishCaughtCount % refreshWeightsEveryXFish == 0)
        //{
        //    if (showLogs)
        //        Debug.Log($"🔄 Refreshing spawn weights after {fishCaughtCount} catches");
        //    LoadBackendProfiles();
        //}
    }
    
   
    /// <summary>
    /// Manually refresh spawn weights from backend
    /// </summary>
    //public void RefreshSpawnWeights()
    //{
    //    if (showLogs)
    //        Debug.Log("🔄 Manually refreshing spawn weights...");
    //    LoadBackendProfiles();
    //}
    
    /// <summary>
    /// Check if backend profiles are loaded
    /// </summary>
    public bool IsReady()
    {
        return profilesLoaded;
    }
    
    /// <summary>
    /// Get current balance
    /// </summary>
    public float GetCurrentBalance()
    {
        return currentBalance;
    }
    
    /// <summary>
    /// Get backend-controlled damage multiplier for bullets
    /// Returns 1.0 if backend not ready (normal damage)
    /// </summary>
    public float GetDamageMultiplier()
    {
        return globalDamageMultiplier;
    }
}