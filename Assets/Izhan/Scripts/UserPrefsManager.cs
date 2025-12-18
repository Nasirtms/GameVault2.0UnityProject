using Supabase.Gotrue;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UserPrefsManager
{
    private const string EmailKey = "UserEmail";
    private const string PasswordKey = "UserPassword";

    // Save email and password
    public static void SaveCredentials(string email, string password)
    {
        PlayerPrefs.SetString(EmailKey, email);
        PlayerPrefs.SetString(PasswordKey, password);
        PlayerPrefs.Save();
    }

    public static void SaveCredentials(string password)
    {
        PlayerPrefs.SetString(PasswordKey, password);
    }

    // Load email
    public static string LoadEmail()
    {
        return PlayerPrefs.GetString(EmailKey, string.Empty);
    }

    // Load password
    public static string LoadPassword()
    {
        return PlayerPrefs.GetString(PasswordKey, string.Empty);
    }

    // Clear saved credentials
    public static void ClearCredentials()
    {
        PlayerPrefs.DeleteKey(EmailKey);
        PlayerPrefs.DeleteKey(PasswordKey);
    }

    // Check if credentials exist
    public static bool HasSavedCredentials()
    {
        return PlayerPrefs.HasKey(EmailKey) && PlayerPrefs.HasKey(PasswordKey);
    }
}
