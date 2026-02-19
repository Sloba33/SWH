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
          .OrderByChild("level")
          .LimitToLast(maxEntries) // important optimization
          .GetValueAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
              {
                  Debug.LogError("[Leaderboard] Load failed");
                  return;
              }

              // Clear previous entries
              foreach (var go in spawnedRanks)
                  Destroy(go);

              spawnedRanks.Clear();

              var entries = new List<DataSnapshot>();

              foreach (var child in task.Result.Children)
              {
                  if (!child.HasChild("level") || !child.HasChild("name"))
                      continue;

                  entries.Add(child);
              }

              // Sort descending (highest level first)
              entries.Sort((a, b) =>
                  int.Parse(b.Child("level").Value.ToString())
                  .CompareTo(int.Parse(a.Child("level").Value.ToString()))
              );

              int finalCount = Mathf.Min(entries.Count, maxEntries);

              for (int i = 0; i < finalCount; i++)
              {
                  string playerName = entries[i].Child("name").Value.ToString();
                  int level = int.Parse(entries[i].Child("level").Value.ToString());
                  int rank = i + 1;

                  GameObject go = Instantiate(leaderboardRankPrefab, contentPanel);
                  LeaderboardRank rankUI = go.GetComponent<LeaderboardRank>();

                  rankUI.Set(playerName, level, rank);

                  spawnedRanks.Add(go);
              }
              //instantiate extra object
              GameObject extraRank = Instantiate(leaderboardRankPrefab, contentPanel);
              extraRank.GetComponent<LeaderboardRank>().SetAsExtra();

              Debug.Log($"[Leaderboard] Loaded {finalCount} entries");
          });
    }

    private void LoadPersonalRank()
    {
        if (db == null || personalRank == null)
            return;

        string myPlayerId = PlayerPrefs.GetString("playerId");

        db.Child("leaderboard")
          .OrderByChild("level")
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
                  if (!child.HasChild("level") || !child.HasChild("name"))
                      continue;

                  entries.Add(child);
              }

              // Sort descending
              entries.Sort((a, b) =>
                  int.Parse(b.Child("level").Value.ToString())
                  .CompareTo(int.Parse(a.Child("level").Value.ToString()))
              );

              for (int i = 0; i < entries.Count; i++)
              {
                  if (entries[i].Key == myPlayerId)
                  {
                      string myName = entries[i].Child("name").Value.ToString();
                      int myLevel = int.Parse(entries[i].Child("level").Value.ToString());
                      int myRank = i + 1;

                      personalRank.Set(myName, myLevel, myRank);

                      Debug.Log($"[Leaderboard] Personal rank: {myRank}");
                      return;
                  }
              }

              // If not found
              personalRank.Set("Unranked", 0, 0);
          });
    }

}
