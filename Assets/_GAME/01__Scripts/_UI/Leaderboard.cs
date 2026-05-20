using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using NaughtyAttributes;
using Firebase.Extensions;
using System.Threading.Tasks;

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

    private void ClearLeaderboardUI()
    {
        foreach (var go in spawnedRanks)
        {
            if (go != null)
                Destroy(go);
        }
        spawnedRanks.Clear();

        if (extraRankObject != null)
        {
            Destroy(extraRankObject);
            extraRankObject = null;
        }

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

    // TEMPORARY: Remove ordering and limiting to test
    db.Child("leaderboard")
      .GetValueAsync()  // Just get everything
      .ContinueWithOnMainThread(task =>
      {
          if (task.IsFaulted)
          {
              Debug.LogError($"[Leaderboard] Load failed: {task.Exception}");
              return;
          }

          DataSnapshot snapshot = task.Result;
          
          if (!snapshot.Exists)  // This should now be false
          {
              Debug.LogWarning("[Leaderboard] No data found");
              return;
          }

          Debug.Log($"[Leaderboard] Found {snapshot.ChildrenCount} entries");
          
          ClearLeaderboardUI();

          var entries = new List<DataSnapshot>();

          foreach (var child in snapshot.Children)
          {
              if (!child.HasChild("trophies") || !child.HasChild("name"))
                  continue;
              entries.Add(child);
          }

          // Sort locally instead of using OrderByChild
          entries.Sort((a, b) =>
          {
              int aVal = int.Parse(a.Child("trophies").Value.ToString());
              int bVal = int.Parse(b.Child("trophies").Value.ToString());
              return bVal.CompareTo(aVal);
          });

          int finalCount = Mathf.Min(entries.Count, maxEntries);
          Debug.Log($"[Leaderboard] Displaying top {finalCount} players");

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
          
          if (entries.Count > 0)
          {
              extraRankObject = Instantiate(leaderboardRankPrefab, contentPanel);
              extraRankObject.GetComponent<LeaderboardRank>().SetAsExtra();
          }
      });
}
    [Button("Debug All Leaderboard Data")]

    public void DebugAllLeaderboardData()
    {
        if (db == null)
        {
            Debug.LogError("[Leaderboard] DB not ready");
            return;
        }

        db.Child("leaderboard").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[Leaderboard] Failed to get data: {task.Exception}");
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (!snapshot.Exists)
            {
                Debug.LogWarning("[Leaderboard] No data found in leaderboard node");
                return;
            }

            Debug.Log($"=== LEADERBOARD DATA ({snapshot.ChildrenCount} entries) ===");

            foreach (var child in snapshot.Children)
            {
                Debug.Log($"Player ID: {child.Key}");

                // Check for trophies
                if (child.HasChild("trophies"))
                {
                    Debug.Log($"  - trophies: {child.Child("trophies").Value} (Type: {child.Child("trophies").Value.GetType()})");
                }
                else
                {
                    Debug.LogWarning($"  - trophies: MISSING!");
                }

                // Check for name
                if (child.HasChild("name"))
                {
                    Debug.Log($"  - name: {child.Child("name").Value}");
                }
                else
                {
                    Debug.LogWarning($"  - name: MISSING!");
                }
            }
            Debug.Log("=====================================");
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
                  Debug.LogError($"[Leaderboard] Personal rank load failed: {task.Exception}");
                  return;
              }

              DataSnapshot snapshot = task.Result;

              if (!snapshot.Exists)
              {
                  Debug.LogWarning("[Leaderboard] No data found for personal rank");
                  personalRank.Set("Unranked", 0, 0);
                  return;
              }

              var entries = new List<DataSnapshot>();

              foreach (var child in snapshot.Children)
              {
                  if (!child.HasChild("trophies") || !child.HasChild("name"))
                      continue;

                  entries.Add(child);
              }

              // Sort descending
              entries.Sort((a, b) =>
              {
                  int aVal = int.Parse(a.Child("trophies").Value.ToString());
                  int bVal = int.Parse(b.Child("trophies").Value.ToString());
                  return bVal.CompareTo(aVal);
              });

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

    public void CloseLeaderboard()
    {
        ClearLeaderboardUI();
        leaderboardPanel.SetActive(false);
    }
}