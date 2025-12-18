using UnityEngine;
using UnityEngine.UI;
public class LockManager : MonoBehaviour
{
    public static LockManager Instance { get; private set; }
    public static bool IsLockModeEnabled { get; private set; } = false;
    private static Fish currentLockedFish;
 
    public static void ToggleLockMode()
    {
        IsLockModeEnabled = !IsLockModeEnabled;
        if (!IsLockModeEnabled)
        {
            ClearLockedFish();
        }
    }

    public static void SetLockedFish(Fish newFish)
    {
        // Already locked → unlock it first
        if (currentLockedFish != null && currentLockedFish != newFish)
            currentLockedFish.SetLocked(false);
        currentLockedFish = newFish;
        if (currentLockedFish != null)
            currentLockedFish.SetLocked(true);
    }
    public static void ClearLockedFish()
    {
        //Debug.Log("clearing locked fish");
        if (currentLockedFish != null)
        {
            //Debug.Log("clearing locked fish1");
            currentLockedFish.SetLocked(false);
            currentLockedFish = null;
        }
    }
    public static Fish GetLockedFish() => currentLockedFish;
}