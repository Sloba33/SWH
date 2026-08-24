using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    // ============================================================
    // FIREBASE STATE
    // ============================================================
    public static bool IsReady { get; private set; }
    private bool firebaseReady = false;

    // ============================================================
    // EVENT QUEUE
    // ============================================================
    private Queue<Action> eventQueue = new Queue<Action>();
    private bool isProcessingQueue = false;

    // ============================================================
    // CURRENT LEVEL SESSION
    // ============================================================
    private bool levelSessionActive = false;
    private bool levelCompleted = false;
    private int currentProgressionLevel;
    private int currentChapter;
    private int currentLevel;
    private int currentCharacterID;
    private float levelStartTime;

    // ============================================================
    // UNITY
    // ============================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[Analytics] Manager Awake.");

        if (FirebaseInit.IsReady)
        {
            InitializeAnalytics();
        }
        else
        {
            FirebaseInit.OnFirebaseReady += InitializeAnalytics;
            Debug.Log("[Analytics] Waiting for FirebaseInit...");
        }
    }

    private void Update()
    {
        if (IsReady && eventQueue.Count > 0 && !isProcessingQueue)
        {
            ProcessQueue();
        }
    }

    private void OnDestroy()
    {
        if (FirebaseInit.Instance != null)
        {
            FirebaseInit.OnFirebaseReady -= InitializeAnalytics;
        }
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================
    private void InitializeAnalytics()
    {
        if (firebaseReady) return;

        firebaseReady = true;

        try
        {
            Debug.Log("[Analytics] Enabling Firebase Analytics...");
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            string playerId = PlayerPrefs.GetString("playerId", "");
            if (!string.IsNullOrEmpty(playerId))
            {
                FirebaseAnalytics.SetUserId(playerId);
                Debug.Log("[Analytics] Firebase Analytics User ID set.");
            }
            else
            {
                Debug.LogWarning("[Analytics] playerId does not exist yet.");
            }

            IsReady = true;
            Debug.Log("[Analytics] Firebase Analytics Ready.");
            Debug.Log("[Analytics] Analytics collection enabled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Analytics] Initialization failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // ============================================================
    // QUEUE PROCESSING
    // ============================================================
    private void ProcessQueue()
    {
        isProcessingQueue = true;

        while (eventQueue.Count > 0)
        {
            try
            {
                Action queuedEvent = eventQueue.Dequeue();
                queuedEvent?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Analytics] Queue processing error: {ex.Message}");
            }
        }

        isProcessingQueue = false;
    }

    private void QueueEvent(Action eventAction)
    {
        if (IsReady)
        {
            eventAction?.Invoke();
        }
        else
        {
            eventQueue.Enqueue(eventAction);
            Debug.Log("[Analytics] Event queued for later processing.");
        }
    }

    // ============================================================
    // SAFETY
    // ============================================================
    private bool CanLog()
    {
        if (!firebaseReady || !IsReady)
        {
            Debug.LogWarning("[Analytics] Event ignored because Analytics is not ready.");
            return false;
        }
        return true;
    }

    // ============================================================
    // LEVEL / CHAPTER CONVERSION
    // ============================================================
    private void GetChapterAndLevel(int progressionLevel, out int chapter, out int level)
    {
        if (progressionLevel < 50)
        {
            chapter = 1;
            level = progressionLevel + 1;
        }
        else if (progressionLevel < 100)
        {
            chapter = 2;
            level = progressionLevel - 50 + 1;
        }
        else if (progressionLevel < 200)
        {
            chapter = 3;
            level = progressionLevel - 100 + 1;
        }
        else
        {
            chapter = 4;
            level = progressionLevel - 200 + 1;
        }
    }

    // ============================================================
    // LEVEL START (WITH QUEUEING)
    // ============================================================
    public void LevelStarted()
    {
        QueueEvent(() =>
        {
            if (!CanLog()) return;

            int progressionLevel = PlayerPrefs.GetInt("Level", 0);
            int characterID = PlayerPrefs.GetInt("SelectedCharacterID", 0);

            GetChapterAndLevel(progressionLevel, out int chapter, out int level);

            if (levelSessionActive && currentProgressionLevel == progressionLevel)
            {
                Debug.LogWarning($"[Analytics] level_start already logged for progression level {progressionLevel}.");
                return;
            }

            currentProgressionLevel = progressionLevel;
            currentChapter = chapter;
            currentLevel = level;
            currentCharacterID = characterID;
            levelStartTime = Time.realtimeSinceStartup;
            levelSessionActive = true;
            levelCompleted = false;

            FirebaseAnalytics.LogEvent(
                FirebaseAnalytics.EventLevelStart,
                new Parameter("level", level),
                new Parameter("chapter", chapter),
                new Parameter("character_id", characterID)
            );

            Debug.Log($"[Analytics] level_start | Chapter: {chapter} | Level: {level} | Character: {characterID}");
        });
    }

    // ============================================================
    // LEVEL COMPLETED
    // ============================================================
    public void LevelCompleted(int stars, int trophies, float completionTime, int trophyDifference)
    {
        if (!CanLog()) return;

        if (!levelSessionActive)
        {
            Debug.LogWarning("[Analytics] level_end requested but no active level session exists.");
            return;
        }

        if (levelCompleted)
        {
            Debug.LogWarning("[Analytics] level_end already logged for this level.");
            return;
        }

        levelCompleted = true;
        float duration = Time.realtimeSinceStartup - levelStartTime;

        FirebaseAnalytics.LogEvent(
            FirebaseAnalytics.EventLevelEnd,
            new Parameter("level", currentLevel),
            new Parameter("chapter", currentChapter),
            new Parameter("character_id", currentCharacterID),
            new Parameter("success", "true"),
            new Parameter("stars", stars),
            new Parameter("trophies", trophies),
            new Parameter("trophy_difference", trophyDifference),
            new Parameter("completion_time", completionTime),
            new Parameter("session_duration", duration)
        );

        Debug.Log($"[Analytics] level_end | Chapter: {currentChapter} | Level: {currentLevel} | " +
                  $"Stars: {stars} | Time: {completionTime:F2}s | " +
                  $"Trophies: {trophies} | Character: {currentCharacterID}");

        levelSessionActive = false;
    }

    // ============================================================
    // LEVEL QUIT
    // ============================================================
    public void LevelQuit(string reason = "quit")
    {
        if (!CanLog()) return;

        if (!levelSessionActive)
        {
            Debug.LogWarning("[Analytics] level_quit requested but no active level session exists.");
            return;
        }

        if (levelCompleted)
        {
            Debug.LogWarning("[Analytics] Not logging level_quit because level was completed.");
            return;
        }

        float duration = Time.realtimeSinceStartup - levelStartTime;

        FirebaseAnalytics.LogEvent(
            "level_quit",
            new Parameter("level", currentLevel),
            new Parameter("chapter", currentChapter),
            new Parameter("character_id", currentCharacterID),
            new Parameter("reason", reason),
            new Parameter("session_duration", duration)
        );

        Debug.Log($"[Analytics] level_quit | Chapter: {currentChapter} | " +
                  $"Level: {currentLevel} | Character: {currentCharacterID} | " +
                  $"Reason: {reason}");

        levelSessionActive = false;
    }

    // ============================================================
    // CURRENCY SPENT
    // ============================================================
    public void CurrencySpent(string currency, int amount, string item, string itemID = "")
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(
            "currency_spent",
            new Parameter("currency", currency),
            new Parameter("amount", amount),
            new Parameter("item", item),
            new Parameter("item_id", itemID)
        );

        Debug.Log($"[Analytics] currency_spent | Currency: {currency} | " +
                  $"Amount: {amount} | Item: {item} | Item ID: {itemID}");
    }

    // ============================================================
    // CURRENCY EARNED
    // ============================================================
    public void CurrencyEarned(string currency, int amount, string source)
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(
            FirebaseAnalytics.EventEarnVirtualCurrency,
            new Parameter("virtual_currency_name", currency),
            new Parameter("value", amount),
            new Parameter("source", source)
        );

        Debug.Log($"[Analytics] currency_earned | Currency: {currency} | " +
                  $"Amount: {amount} | Source: {source}");
    }

    // ============================================================
    // CHARACTER SELECTED
    // ============================================================
    public void CharacterSelected(int characterID)
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(
            "character_selected",
            new Parameter("character_id", characterID)
        );

        Debug.Log($"[Analytics] character_selected | Character: {characterID}");
    }

    // ============================================================
    // CHARACTER UNLOCKED
    // ============================================================
    public void CharacterUnlocked(int characterID, string unlockMethod)
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(
            "character_unlocked",
            new Parameter("character_id", characterID),
            new Parameter("unlock_method", unlockMethod)
        );

        Debug.Log($"[Analytics] character_unlocked | Character: {characterID} | " +
                  $"Method: {unlockMethod}");
    }

    // ============================================================
    // CHARACTER UPGRADED
    // ============================================================
    public void CharacterUpgraded(int characterID, string stat, int newUpgradeLevel, string currency, int cost)
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(
            "character_upgraded",
            new Parameter("character_id", characterID),
            new Parameter("stat", stat),
            new Parameter("upgrade_level", newUpgradeLevel),
            new Parameter("currency", currency),
            new Parameter("cost", cost)
        );

        Debug.Log($"[Analytics] character_upgraded | Character: {characterID} | " +
                  $"Stat: {stat} | Level: {newUpgradeLevel} | " +
                  $"Currency: {currency} | Cost: {cost}");
    }

    // ============================================================
    // ITEM PURCHASED
    // ============================================================
    public void ItemPurchased(string itemType, string itemID, string currency, int cost)
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(
            "item_purchased",
            new Parameter("item_type", itemType),
            new Parameter("item_id", itemID),
            new Parameter("currency", currency),
            new Parameter("cost", cost)
        );

        Debug.Log($"[Analytics] item_purchased | Type: {itemType} | " +
                  $"ID: {itemID} | Currency: {currency} | Cost: {cost}");
    }

    // ============================================================
    // TUTORIAL
    // ============================================================
    public void TutorialCompleted()
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent("tutorial_completed");
        Debug.Log("[Analytics] tutorial_completed");
    }

    // ============================================================
    // GENERIC CUSTOM EVENT
    // ============================================================
    public void LogCustomEvent(string eventName)
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(eventName);
        Debug.Log($"[Analytics] custom_event | {eventName}");
    }

    public void LogCustomEvent(string eventName, params Parameter[] parameters)
    {
        if (!CanLog()) return;

        FirebaseAnalytics.LogEvent(eventName, parameters);
        Debug.Log($"[Analytics] custom_event | {eventName}");
    }
}