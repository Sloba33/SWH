using System;
using UnityEngine;
using Firebase;
using Firebase.Extensions;

using Firebase.Database;
public class FirebaseInit : MonoBehaviour
{
    public static FirebaseInit Instance { get; private set; }

    public static bool IsReady { get; private set; } = false;

    // Optional: let other scripts know when Firebase is fully ready
    public static event Action OnFirebaseReady;
    private string playerId;

    private FirebaseApp app;
    private void EnsurePlayerId()
    {
        if (!PlayerPrefs.HasKey("playerId"))
        {
            string id = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("playerId", id);
            PlayerPrefs.Save();
        }
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePlayerId();

        // enable verbose logging to see internal Firebase errors in logcat
        Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Debug;

        Debug.Log("[FirebaseInit] Starting initialization...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            // We wrap the ENTIRE callback in a try-catch. 
            // Without this, exceptions on Android often happen silently inside the Task.
            try
            {
                var dependencyStatus = task.Result;
                Debug.Log($"[FirebaseInit] Dependency check result: {dependencyStatus}");

                if (dependencyStatus == DependencyStatus.Available)
                {
                    // 1. Initialize App R(Crucial step that often fails on Android if config is missing)
                    app = FirebaseApp.DefaultInstance;
                   
                    if (app == null)
                    {
                        Debug.LogError("[FirebaseInit] DefaultInstance is null! (Check google-services.json)");
                        return;
                    }

                    Debug.Log($"[FirebaseInit] App initialized. Name: {app.Name}");

                    // 2. Check for Database URL specifically
                    if (app.Options == null || string.IsNullOrEmpty(app.Options.DatabaseUrl?.ToString()))
                    {
                        Debug.LogError("[FirebaseInit] CRITICAL: Database URL is missing from App Options.");
                    }
                    else
                    {
                        Debug.Log($"[FirebaseInit] Database URL found: {app.Options.DatabaseUrl}");
                    }

                    // 3. Initialize Database
                    var db = FirebaseDatabase.DefaultInstance;

                    // Enable persistence if you want offline support (optional, but good for mobile)
                    db.SetPersistenceEnabled(true);

                    Debug.Log($"[FirebaseInit] DB Instance created.");

                    // 4. Run your test write immediately
                    string path = "test/mobile_" + DateTime.Now.Ticks;
                    Debug.Log($"[FirebaseInit] Attempting write to: {path}");

                    db.GetReference(path)
                      .SetValueAsync("Test from mobile")
                      .ContinueWithOnMainThread(writeTask =>
                      {
                          if (writeTask.IsFaulted)
                          {
                              Debug.LogError($"[FirebaseInit] DB Write FAILED: {writeTask.Exception?.Flatten().InnerException?.Message}");
                          }
                          else if (writeTask.IsCompleted)
                          {
                              Debug.Log("[FirebaseInit] DB Write SUCCESS!");
                          }
                      });

                    IsReady = true;
                    OnFirebaseReady?.Invoke();
                }
                else
                {
                    Debug.LogError($"[FirebaseInit] Dependencies failed: {dependencyStatus}");
                }
            }
            catch (Exception ex)
            {
                // THIS is what you are looking for. 
                // It will catch the crash that is currently happening silently.
                Debug.LogError($"[FirebaseInit] FATAL ERROR during init: {ex.Message}\nStack: {ex.StackTrace}");
            }
        });
    }

    // Helper method – call from other scripts instead of using DefaultInstance directly
    public static FirebaseApp GetAppSafe()
    {
        if (!IsReady || Instance == null || Instance.app == null)
        {
            Debug.LogWarning("[FirebaseInit] Trying to use Firebase before it's ready!");
            return null;
        }
        return Instance.app;
    }
}