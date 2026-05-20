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
    [Tooltip("The Scroll View's Viewport RectTransform. If left empty, contentPanel's parent is used.")]
    [SerializeField] private RectTransform scrollViewport;

    // The local player's own (hidden) row inside the list. The floating personalRank element
    // tracks this row's position, clamped to the viewport, so it reads as part of the list.
    private RectTransform playerRowPlaceholder;
    private RectTransform _cachedViewport;
    // Clips list content out of the band the float occupies while it is pinned to an edge,
    // so semi-transparent rows don't show through the (also semi-transparent) float.
    private RectMask2D _viewportMask;
    private readonly Vector3[] _viewportCorners = new Vector3[4];
    private readonly Vector3[] _floatCorners = new Vector3[4];
    private readonly Vector3[] _placeholderCorners = new Vector3[4];

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
        playerRowPlaceholder = null;

        // Drop any edge-clip so a stale band doesn't flash before LateUpdate recomputes.
        if (_viewportMask != null)
        {
            Vector4 padding = _viewportMask.padding;
            padding.y = 0f;
            padding.w = 0f;
            _viewportMask.padding = padding;
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

          string myPlayerId = PlayerPrefs.GetString("playerId");

          for (int i = 0; i < finalCount; i++)
          {
              string playerName = entries[i].Child("name").Value.ToString();
              int trophies = int.Parse(entries[i].Child("trophies").Value.ToString());
              int rank = i + 1;

              GameObject go = Instantiate(leaderboardRankPrefab, contentPanel);
              LeaderboardRank rankUI = go.GetComponent<LeaderboardRank>();
              rankUI.Set(playerName, trophies, rank);
              spawnedRanks.Add(go);

              // The local player's own row stays in the list and reserves layout space, but is
              // rendered invisible. The floating personalRank element is drawn in its place and
              // clamps to the viewport edges (see LateUpdate), so it never scrolls off-screen.
              if (!string.IsNullOrEmpty(myPlayerId) && entries[i].Key == myPlayerId)
              {
                  rankUI.SetHidden(true);
                  playerRowPlaceholder = go.GetComponent<RectTransform>();
              }
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

    // Keeps the floating personalRank aligned with the player's (hidden) row in the list, but
    // clamped so it never leaves the viewport. While the player's row is fully scrolled into
    // view the clamp is inactive and the float sits exactly on top of the hidden row, so it
    // reads as a normal list entry. When the row scrolls past an edge, the float sticks to
    // that edge. The transition is continuous because the clamp engages exactly as the row's
    // edge crosses the viewport's edge.
    //
    // While the float is pinned to an edge, the list content is clipped out of the band the
    // float covers (via RectMask2D padding) so semi-transparent rows don't show through it.
    private void LateUpdate()
    {
        if (personalRank == null)
            return;

        RectTransform viewport = ResolveViewport();
        if (viewport == null)
            return;

        var floatRt = (RectTransform)personalRank.transform;

        viewport.GetWorldCorners(_viewportCorners);
        float viewBottom = _viewportCorners[0].y;
        float viewTop = _viewportCorners[1].y;

        floatRt.GetWorldCorners(_floatCorners);
        float floatHeight = _floatCorners[1].y - _floatCorners[0].y;
        float floatHalfHeight = floatHeight * 0.5f;
        float floatCenterY = (_floatCorners[0].y + _floatCorners[1].y) * 0.5f;

        // Range the float's center may occupy so the float stays fully inside the viewport.
        float minCenterY = viewBottom + floatHalfHeight;
        float maxCenterY = viewTop - floatHalfHeight;

        float targetCenterY;
        // World-space height of the band to clip from each edge of the list.
        float clipBottomWorld = 0f;
        float clipTopWorld = 0f;

        if (playerRowPlaceholder != null)
        {
            playerRowPlaceholder.GetWorldCorners(_placeholderCorners);
            float rowCenterY = (_placeholderCorners[0].y + _placeholderCorners[1].y) * 0.5f;
            targetCenterY = Mathf.Clamp(rowCenterY, minCenterY, maxCenterY);

            // How far the float is pinned past its row equals how much list content has slid
            // under it. Capped at the float's own height.
            float offset = targetCenterY - rowCenterY;
            if(offset > 0f)
                clipBottomWorld = Mathf.Min(offset * 2f, floatHeight);
            else if(offset < 0f)
                clipTopWorld = Mathf.Min(-offset * 2f, floatHeight);
        }
        else
        {
            // Player isn't in the displayed list — keep the rank pinned to the bottom edge,
            // fully detached from the list, so clip the whole band it covers.
            targetCenterY = minCenterY;
            clipBottomWorld = floatHeight;
        }

        float deltaY = targetCenterY - floatCenterY;
        if (Mathf.Abs(deltaY) > 0.001f)
            floatRt.position += new Vector3(0f, deltaY, 0f);

        ApplyViewportClip(viewport, clipBottomWorld, clipTopWorld);
    }

    private void ApplyViewportClip(RectTransform viewport, float clipBottomWorld, float clipTopWorld)
    {
        RectMask2D mask = EnsureViewportMask(viewport);
        if (mask == null)
            return;

        float scaleY = viewport.lossyScale.y;
        if (scaleY <= 0f)
            return;

        // RectMask2D.padding is (left, bottom, right, top) in the mask's local rect units;
        // converting world-space band heights keeps it correct under any canvas scale.
        Vector4 padding = mask.padding;
        padding.y = clipBottomWorld / scaleY;
        padding.w = clipTopWorld / scaleY;
        mask.padding = padding;
    }

    private RectMask2D EnsureViewportMask(RectTransform viewport)
    {
        if (_viewportMask != null)
            return _viewportMask;
        if (!viewport.TryGetComponent(out _viewportMask))
            _viewportMask = viewport.gameObject.AddComponent<RectMask2D>();
        return _viewportMask;
    }

    private RectTransform ResolveViewport()
    {
        if (scrollViewport != null)
            return scrollViewport;
        if (_cachedViewport == null && contentPanel != null)
            _cachedViewport = contentPanel.parent as RectTransform;
        return _cachedViewport;
    }

    public void CloseLeaderboard()
    {
        ClearLeaderboardUI();
        leaderboardPanel.SetActive(false);
    }
}