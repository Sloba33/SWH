using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class LeaderboardManager : MonoBehaviour
{
    private DatabaseReference db;

    private void Start()
    {
        FirebaseInit.OnFirebaseReady += Init;
        if(FirebaseInit.IsReady) Init();
      
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

        EnsurePlayerId();

        string playerId = PlayerPrefs.GetString("playerId");
        string playerName = PlayerPrefs.GetString("playerName", "Player");
        int level = PlayerPrefs.GetInt("Level", 0);

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "name", playerName },
            { "level", level }
        };

        db.Child("leaderboard")
          .Child(playerId)
          .SetValueAsync(data)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
                  Debug.LogError("[Leaderboard] Upload failed");
              else
                  Debug.Log("[Leaderboard] Player data uploaded");
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
}
