using UnityEngine;
using System.Collections.Generic;
using Facebook.Unity;
public class FacebookInit : MonoBehaviour
{
    public static bool IsReady { get; private set; }
    public static event System.Action OnFacebookReady;

    private bool isInitialized = false;

    private void Awake()
    {
        // Don't destroy on load so Facebook stays initialized
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeFacebook();

    }

    private void InitializeFacebook()
    {
        if (isInitialized) return;

        if (!FB.IsInitialized)
        {
            Debug.Log("[Facebook] Initializing SDK...");
            FB.Init(OnInitComplete, OnHideUnity);
        }
        else
        {
            OnInitComplete();
        }
    }

    private void OnInitComplete()
    {
        if (FB.IsInitialized)
        {
            isInitialized = true;
            IsReady = true;

            Debug.Log("[Facebook] SDK initialized successfully!");

            // Activate app to track sessions
            FB.ActivateApp();
            FB.LogAppEvent("facebook_test_event_ACTIVATE_APP");
            // Enable auto-logging
            FB.Mobile.SetAutoLogAppEventsEnabled(true);
            FB.Mobile.SetAdvertiserIDCollectionEnabled(true);

            // Notify any listeners
            OnFacebookReady?.Invoke();
        }
        else
        {
            Debug.LogError("[Facebook] SDK initialization failed!");
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        // Handle pause/resume if needed
        if (!isGameShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    // ============================================================
    // HELPER METHODS FOR LOGGING EVENTS
    // ============================================================

    public static void LogEvent(string eventName)
    {
        if (!IsReady)
        {
            Debug.LogWarning($"[Facebook] Event '{eventName}' skipped - SDK not ready.");
            return;
        }

        FB.LogAppEvent(eventName);
        Debug.Log($"[Facebook] Event logged: {eventName}");
    }

    public static void LogEvent(string eventName, float valueToSum)
    {
        if (!IsReady)
        {
            Debug.LogWarning($"[Facebook] Event '{eventName}' skipped - SDK not ready.");
            return;
        }

        FB.LogAppEvent(eventName, valueToSum);
        Debug.Log($"[Facebook] Event logged: {eventName} (value: {valueToSum})");
    }

    public static void LogEvent(string eventName, float? valueToSum, System.Collections.Generic.Dictionary<string, object> parameters)
    {
        if (!IsReady)
        {
            Debug.LogWarning($"[Facebook] Event '{eventName}' skipped - SDK not ready.");
            return;
        }

        FB.LogAppEvent(eventName, valueToSum, parameters);
        Debug.Log($"[Facebook] Event logged: {eventName}");
    }
}