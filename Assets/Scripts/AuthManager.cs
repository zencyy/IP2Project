using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic; 

public class AuthManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TMP_Text statusText;

    [Header("Settings")]
    public string gameSceneName = "World_Main"; // Change to your game scene name

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    void Start()
    {
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.GetInstance("https://fymstudio-a8928-default-rtdb.asia-southeast1.firebasedatabase.app/").RootReference;        UpdateStatus("Ready to Login");
    }

    // --- BUTTON FUNCTIONS ---

    public void OnLoginPressed()
    {
        string email = emailField.text;
        string password = passwordField.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Please enter email and password");
            return;
        }

        UpdateStatus("Logging in...");
        
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                UpdateStatus("Login Failed: " + task.Exception?.Flatten().InnerExceptions[0].Message);
                return;
            }

            // Success
            FirebaseUser newUser = task.Result.User;
            UpdateStatus($"Welcome back, {newUser.Email}!");
            Invoke("LoadGameScene", 1.5f);
        });
    }

    public void OnRegisterPressed()
    {
        string email = emailField.text;
        string password = passwordField.text;

        UpdateStatus("Account Created");

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                UpdateStatus("Registration Failed: " + task.Exception?.Flatten().InnerExceptions[0].Message);
                return;
            }

            // Authentication Successful
            FirebaseUser newUser = task.Result.User;
            
            // Now Create the Database Entry (DDA Requirement)
            CreateInitialUserData(newUser.UserId, email);
        });
    }

    // --- DATABASE LOGIC (DDA Requirement) ---
    
    void CreateInitialUserData(string userId, string email)
    {
        User data = new User(email);
        string json = JsonUtility.ToJson(data);

        dbReference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task => {
            // FIX: Check for errors first!
            if (task.IsFaulted)
            {
                Debug.LogError("Database Error: " + task.Exception.ToString());
                UpdateStatus("Login Success, but Database Failed.");
                return;
            }

            if (task.IsCanceled)
            {
                Debug.LogError("Database Write Canceled.");
                return;
            }

            if (task.IsCompleted)
            {
                UpdateStatus("Account & Database Created! Loading Game...");
                Invoke("LoadGameScene", 1.5f);
            }
        });
    }

    void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void UpdateStatus(string message)
    {
        if(statusText != null) statusText.text = message;
        Debug.Log(message);
    }
}

// --- DATA CLASSES FOR JSON ---

[System.Serializable]
public class User
{
    public string username;
    public string email;
    public string created_at;
    public UserProgress progress;
    public UserStats statistics;

    public User(string emailAddress)
    {
        this.username = "Student"; // Default name
        this.email = emailAddress;
        this.created_at = System.DateTime.UtcNow.ToString();
        this.progress = new UserProgress();
        this.statistics = new UserStats();
    }
}

[System.Serializable]
public class UserProgress
{
    public int current_level = 1;
    public int total_score = 0;
    public int sentences_completed = 0;
    public int vocabulary_collected = 0;

    public List<string> inventory = new List<string>();
    // Arrays are harder to initialize in simple JSON utility, initializing empty for now
}

[System.Serializable]
public class UserStats
{
    public float accuracy_rate = 1.0f;
    public float average_time_per_sentence = 0f;
}