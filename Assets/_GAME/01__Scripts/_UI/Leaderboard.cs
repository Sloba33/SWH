using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using NaughtyAttributes;
using Firebase.Extensions;

public class Leaderboard : MonoBehaviour
{
    public GameObject leaderboardPanel;
    private readonly List<GameObject> spawnedRanks = new();
    [SerializeField] private int maxEntries = 100;
    public Button leaderboardButton;
    public GameObject leaderboardRankPrefab;
    public Transform contentPanel;
    private DatabaseReference db;
    [SerializeField] private LeaderboardRank personalRank;
    private GameObject extraRankObject; // Track the extra rank separately
    
    void Start()
    {
        if (FirebaseInit.IsReady)
            Init();
        else
            FirebaseInit.OnFirebaseReady += Init;
    }

    private void Init()
    {
        db = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("[Leaderboard] DB ready");
        LoadTopPlayers(maxEntries);
        LoadPersonalRank();
    }
    
    // Call this BEFORE loading new data to clear the UI
    private void ClearLeaderboardUI()
    {
        // Destroy all tracked rank entries
        foreach (var go in spawnedRanks)
        {
            if (go != null)
                Destroy(go);
        }
        spawnedRanks.Clear();
        
        // Destroy the extra rank if it exists
        if (extraRankObject != null)
        {
            Destroy(extraRankObject);
            extraRankObject = null;
        }
        
        // OPTIONAL: Also destroy any children that might have been missed
        // This is a safety net
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }
    
    [NaughtyAttributes.Button("Hello")]
    public void LoadTopPlayersBtnRef()
    {
        LoadTopPlayers(maxEntries);
    }
    
    private void LoadTopPlayers(int count)
    {
        if (db == null)
        {
            Debug.LogWarning("[Leaderboard] DB not ready");
            return;
        }

        db.Child("leaderboard")
          .OrderByChild("trophies")
          .LimitToLast(maxEntries) 
          .GetValueAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("[Leaderboard] Load failed");
                  return;
              }

              // Clear UI before adding new entries
              ClearLeaderboardUI();

              var entries = new List<DataSnapshot>();

              foreach (var child in task.Result.Children)
              {
                  if (!child.HasChild("trophies") || !child.HasChild("name"))
                      continue;

                  entries.Add(child);
              }

              // Sort descending (highest trophies first)
              entries.Sort((a, b) =>
                  int.Parse(b.Child("trophies").Value.ToString())
                  .CompareTo(int.Parse(a.Child("trophies").Value.ToString()))
              );

              int finalCount = Mathf.Min(entries.Count, maxEntries);

              for (int i = 0; i < finalCount; i++)
              {
                  string playerName = entries[i].Child("name").Value.ToString();
                  int trophies = int.Parse(entries[i].Child("trophies").Value.ToString());
                  int rank = i + 1;

                  GameObject go = Instantiate(leaderboardRankPrefab, contentPanel);
                  LeaderboardRank rankUI = go.GetComponent<LeaderboardRank>();
                  rankUI.Set(playerName, trophies, rank);
                  spawnedRanks.Add(go);
              }
              
              // Instantiate and track the extra object
              extraRankObject = Instantiate(leaderboardRankPrefab, contentPanel);
              extraRankObject.GetComponent<LeaderboardRank>().SetAsExtra();

              Debug.Log($"[Leaderboard] Loaded {finalCount} entries");
          });
    }

    private void LoadPersonalRank()
    {
        if (db == null || personalRank == null)
            return;

        string myPlayerId = PlayerPrefs.GetString("playerId");

        db.Child("leaderboard")
          .OrderByChild("trophies")
          .GetValueAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("[Leaderboard] Personal rank load failed");
                  return;
              }

              var entries = new List<DataSnapshot>();

              foreach (var child in task.Result.Children)
              {
                  if (!child.HasChild("trophies") || !child.HasChild("name"))
                      continue;

                  entries.Add(child);
              }

              // Sort descending
              entries.Sort((a, b) =>
                  int.Parse(b.Child("trophies").Value.ToString())
                  .CompareTo(int.Parse(a.Child("trophies").Value.ToString()))
              );

              for (int i = 0; i < entries.Count; i++)
              {
                  if (entries[i].Key == myPlayerId)
                  {
                      string myName = entries[i].Child("name").Value.ToString();
                      int myTrophies = int.Parse(entries[i].Child("trophies").Value.ToString());
                      int myRank = i + 1;

                      personalRank.Set(myName, myTrophies, myRank);
                      Debug.Log($"[Leaderboard] Personal rank: {myRank}");
                      return;
                  }
              }

              // If not found
              personalRank.Set("Unranked", 0, 0);
          });
    }
    
    // Optional: Call this when closing the leaderboard panel
    public void CloseLeaderboard()
    {
        ClearLeaderboardUI();
        leaderboardPanel.SetActive(false);
    }
}