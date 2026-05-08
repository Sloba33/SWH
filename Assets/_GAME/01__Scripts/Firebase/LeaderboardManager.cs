using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class LeaderboardManager : MonoBehaviour
{
    private DatabaseReference db;
    private float lastUploadTime = 0;
    private float uploadCooldown = 1f;
    private bool isUploading = false;

    private void Start()
    {
        if (FirebaseInit.IsReady)
            Init();
        else
            FirebaseInit.OnFirebaseReady += Init;
    }

    private void Init()
    {
        db = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("[Leaderboard] Ready");
        UploadPlayerData();
    }

    public void UploadPlayerData()
    {
        if (db == null)
        {
            Debug.LogWarning("[Leaderboard] DB not ready");
            return;
        }

        if (isUploading)
        {
            Debug.Log("[Leaderboard] Upload already in progress");
            return;
        }

        if (Time.time - lastUploadTime < uploadCooldown)
        {
            Debug.Log("[Leaderboard] Upload throttled");
            return;
        }

        EnsurePlayerId();

        string playerId = PlayerPrefs.GetString("playerId");
        string playerName = PlayerPrefs.GetString("playerName", "Player");
        int trophies = PlayerPrefs.GetInt("Trophies", 0);

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "name", playerName },
            { "trophies", trophies },
            { "lastUpdated", System.DateTime.UtcNow.Ticks }
        };

        isUploading = true;
        lastUploadTime = Time.time;

        db.Child("leaderboard")
          .Child(playerId)
          .SetValueAsync(data)
          .ContinueWithOnMainThread(task =>
          {
              isUploading = false;
              
              if (task.IsFaulted)
              {
                  Debug.LogError($"[Leaderboard] Upload failed for {playerId}: {task.Exception}");
                  // Optional: Implement retry logic here
              }
              else
                  Debug.Log($"[Leaderboard] Player {playerName} data uploaded (Trophies: {trophies})");
          });
    }

    private void EnsurePlayerId()
    {
        if (!PlayerPrefs.HasKey("playerId"))
        {
            PlayerPrefs.SetString("playerId", System.Guid.NewGuid().ToString());
            PlayerPrefs.Save();
        }
    }

    // Optional: Call this when player earns trophies
    public void AddTrophies(int amount)
    {
        if (db == null) return;
        
        string playerId = PlayerPrefs.GetString("playerId");
        
        db.Child("leaderboard")
          .Child(playerId)
          .Child("trophies")
          .RunTransaction(trophyData =>
          {
              int currentTrophies = trophyData.Value != null ? int.Parse(trophyData.Value.ToString()) : 0;
              int newTrophies = currentTrophies + amount;
              trophyData.Value = newTrophies;
              
              // Update local PlayerPrefs
              PlayerPrefs.SetInt("Trophies", newTrophies);
              PlayerPrefs.Save();
              
              return TransactionResult.Success(trophyData);
          })
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
                  Debug.LogError($"[Leaderboard] Failed to add trophies: {task.Exception}");
              else
                  Debug.Log($"[Leaderboard] Added {amount} trophies. New total: {task.Result.Value}");
          });
    }
}