using UnityEngine;
using Supabase;
using System;
using Postgrest.Models;
using Postgrest.Attributes;


[Table("custom_users")]
public class CustomUser : BaseModel
{
    [Column("user_id")]
    public int Id { get; set; }

    [Column("email")]
    public string Email { get; set; }

    [Column("password")]
    public string Password { get; set; }

    [Column("username")]
    public string Username { get; set; }

    [Column("bio")]
    public string Bio { get; set; }

    [Column("avatar_id")]
    public string AvatarId { get; set; }

    [Column("coins")]
    public float Coins { get; set; }

}


public class SupabaseConnection : MonoBehaviour
{
    private SettingsPanelController settingsPanelController;

    private Client supabase;

    private const string SUPABASE_URL = "https://pgczpubnlvtgkifffocs.supabase.co";
    private const string SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InBnY3pwdWJubHZ0Z2tpZmZmb2NzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDY0NDEzMDUsImV4cCI6MjA2MjAxNzMwNX0.oDn9G6t6HcdynT50mKirQGU84FyuvUf2_gtqM4g1zTQ";

    [Header("Managers")]
    [SerializeField] private LoginManager loginManager;
    [SerializeField] private MainMenuUIManager mainMenuUIManager;

    private async void Awake()
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        };

        supabase = await Client.InitializeAsync(SUPABASE_URL, SUPABASE_KEY, options);
        Debug.Log("Supabase client initialized");
    }

    public async void UserValidation(string email, string password)
    {
        try
        {
            CasinoUIManager.Instance.ShowErrorCanvas(0, "");
            var result = await supabase
                .From<CustomUser>()
                .Filter("email", Postgrest.Constants.Operator.Equals, email)
                //.Filter("password", Postgrest.Constants.Operator.Equals, password)
                .Get();
            CasinoUIManager.Instance.ShowErrorCanvas(2, "");

            Debug.Log(result.Content);

            if (result.Models.Count == 0)
            {
                Debug.LogError("Email not found.");
                //loginManager.ShowError("Email not found.");
                return;
            }

            var user = result.Models[0];

            if (user.Password == password)
            {
                Debug.Log("✅ Login successful!");
                Debug.Log("You have " + user.Coins + " Coins");

                // Save session

               // UserManager.Instance.SetUserData(user.Id, user.Username, user.Email, user.Coins, user.AvatarId, user.Bio);
                //MainMenuUIManager.Instance.SetUserData();
                //FindObjectOfType<SequentialPopupManager>()?.TriggerAfterLogin();

                //UserSessionManager.Instance.SetUserSession(user.Username, user.Coins);
                //Debug.Log(UserSessionManager.Instance.IsLoggedIn);

                //mainScreenManager.getUserData(user.Username, user.Coins);
                //loginManager.loginPanel.SetActive(false);
                //loginManager.mainPanel.SetActive(true);
                return;
            }
            else
            {
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Worng Password");
                Debug.LogError("❌ Incorrect password.");
                //loginManager.ShowError("Incorrect password.");
                return;
            }
        }
        catch (Exception ex)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(2,"");
            Debug.LogError("Unexpected login error: " + ex.Message);
            //loginManager.ShowError("Something went wrong. Try again later.");
        }

    }

}
