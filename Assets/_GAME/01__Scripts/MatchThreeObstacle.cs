using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchThreeObstacle : MonoBehaviour
{
    public LayerMask obstacleLayer;
    public float rayLength = 0.9999f;
    public Obstacle Obstacle;
    public Tile currentTile;
    public bool hasGameStarted;
    public bool isDestructible;

    private List<Obstacle> obstaclesToProcessForSpawns = new();

    private IEnumerator Start()
    {
        Obstacle = GetComponent<Obstacle>();
        yield return new WaitForSeconds(1.5f);
        hasGameStarted = true;
    }

    public GameObject groundObject, previousGroundObject;

    void FixedUpdate()
    {
        if (!isDestructible) return;
        if (Obstacle != null && Obstacle.isFalling) ClearObstacleLists();
        if (hasGameStarted && Obstacle.Moving && IsCenteredOnTile())
        {
            if (Obstacle.isFalling) return;
            if (currentTile != Obstacle.tile)
            {
                ClearObstacleLists();
                currentTile = Obstacle.tile;
            }

            CastRays();
            FillVerticalHorizontalLists();
            CheckForMatches();
        }
    }

    public void RunMatchOnce()
    {
        CastRays();
        FillVerticalHorizontalLists();
        CheckForMatches();
    }

    public List<Obstacle> verticalList = new();
    public List<Obstacle> horizontalList = new();
    public List<Obstacle> updownList = new();

    private void CheckForMatches()
    {
        obstaclesToProcessForSpawns.Clear();
        CheckListForMatches(verticalList);
        CheckListForMatches(horizontalList);
        CheckListForMatches(updownList);

        if (obstaclesToProcessForSpawns.Count > 0)
        {
            if (GameManager.Instance != null && GameManager.Instance.levelGoal != null)
            {
                foreach (var obstacle in obstaclesToProcessForSpawns)
                {
                    GameManager.Instance.levelGoal.QueueObstacleForSpawnProcessing(obstacle);
                }
            }
            ClearObstacleLists();
        }
    }

    // 🔥 NEW universal-aware matching system
    private void CheckListForMatches(List<Obstacle> obstacleList)
    {
        if (obstacleList == null || obstacleList.Count < 3)
            return;

        var concreteColors = GetConcreteColorsInList(obstacleList);
        if (concreteColors.Count == 0)
            return;

        HashSet<int> indicesToDestroy = new();

        // For each distinct color found, find runs that include universals
        foreach (var color in concreteColors)
        {
            int i = 0;
            while (i < obstacleList.Count)
            {
                while (i < obstacleList.Count && !IsColorCompatibleForGrouping(obstacleList[i], color))
                    i++;
                if (i >= obstacleList.Count) break;

                int runStart = i;
                int runEnd = i;
                int runCount = 0;

                while (runEnd < obstacleList.Count && IsColorCompatibleForGrouping(obstacleList[runEnd], color))
                {
                    runCount++;
                    runEnd++;
                }

                if (runCount >= 3)
                {
                    for (int k = runStart; k < runEnd; k++)
                        indicesToDestroy.Add(k);
                }

                i = runEnd;
            }
        }

        if (indicesToDestroy.Count == 0)
            return;

        var obstaclesMarked = indicesToDestroy.OrderBy(x => x)
            .Select(idx => obstacleList[idx])
            .Where(o => o != null)
            .ToList();

        // Check for JackInTheBox in the union-set
        if (CheckAndHandleJackInTheBox(obstaclesMarked))
            return;

        // Group consecutive indices into batches
        var consecutiveGroups = GroupConsecutiveIndices(indicesToDestroy.OrderBy(x => x).ToList());

        foreach (var grp in consecutiveGroups)
        {
            int startIndex = grp.Item1;
            int count = grp.Item2;
            if (startIndex >= 0 && startIndex + count <= obstacleList.Count)
            {
                DestroyConsecutiveObstacles(obstacleList, startIndex, count);
            }
        }
    }

    private bool IsColorCompatibleForGrouping(Obstacle obstacle, ObstacleColor targetColor)
    {
        if (obstacle == null) return false;
        return obstacle.obstacleColor == targetColor || obstacle.obstacleColor == ObstacleColor.Universal;
    }

    private List<ObstacleColor> GetConcreteColorsInList(List<Obstacle> obstacleList)
    {
        HashSet<ObstacleColor> set = new();
        foreach (var o in obstacleList)
        {
            if (o == null) continue;
            if (o.obstacleColor != ObstacleColor.Universal)
                set.Add(o.obstacleColor);
        }
        return set.ToList();
    }

    private List<System.Tuple<int, int>> GroupConsecutiveIndices(List<int> sortedIndices)
    {
        List<System.Tuple<int, int>> groups = new();
        if (sortedIndices == null || sortedIndices.Count == 0)
            return groups;

        int runStart = sortedIndices[0];
        int runPrev = sortedIndices[0];
        int runCount = 1;

        for (int i = 1; i < sortedIndices.Count; i++)
        {
            int idx = sortedIndices[i];
            if (idx == runPrev + 1)
            {
                runPrev = idx;
                runCount++;
            }
            else
            {
                groups.Add(System.Tuple.Create(runStart, runCount));
                runStart = idx;
                runPrev = idx;
                runCount = 1;
            }
        }
        groups.Add(System.Tuple.Create(runStart, runCount));
        return groups;
    }

    private bool CheckAndHandleJackInTheBox(List<Obstacle> obstacleList)
    {
        foreach (Obstacle obstacle in obstacleList)
        {
            JackInTheBox jackInTheBox = obstacle.GetComponent<JackInTheBox>();
            if (jackInTheBox != null)
            {
                jackInTheBox.TriggerJackInTheBox();
                return true;
            }
        }
        return false;
    }

    private bool IsMatchingObstacle(Obstacle mainObstacle, Obstacle compareObstacle)
    {
        if (mainObstacle == null || compareObstacle == null) return false;

        if (mainObstacle.obstacleModifier == ObstacleModifier.Universal ||
            compareObstacle.obstacleModifier == ObstacleModifier.Universal)
            return true;

        if (mainObstacle.obstacleType == ObstacleType.Universal ||
            compareObstacle.obstacleType == ObstacleType.Universal)
        {
            return (mainObstacle.obstacleColor == compareObstacle.obstacleColor ||
                    mainObstacle.obstacleColor == ObstacleColor.Universal ||
                    compareObstacle.obstacleColor == ObstacleColor.Universal);
        }

        return (mainObstacle.obstacleType == compareObstacle.obstacleType &&
                mainObstacle.obstacleColor == compareObstacle.obstacleColor &&
                mainObstacle.obstacleModifier == compareObstacle.obstacleModifier);
    }

    private bool IsCenteredOnTile()
    {
        if (Obstacle == null || Obstacle.tile == null)
            return false;

        Vector3 obstaclePos = transform.position;
        Vector3 tileCenter = Obstacle.tile.transform.position;

        Vector2 obstacleXZ = new(obstaclePos.x, obstaclePos.z);
        Vector2 tileCenterXZ = new(tileCenter.x, tileCenter.z);
        return Vector2.Distance(obstacleXZ, tileCenterXZ) <= 0.2f;
    }

    private void DestroyConsecutiveObstacles(List<Obstacle> obstacleList, int startIndex, int count)
    {
        for (int j = startIndex; j < startIndex + count; j++)
        {
            if (j >= 0 && j < obstacleList.Count)
            {
                Obstacle obstacleToDestroy = obstacleList[j];
                if (obstacleToDestroy != null && !obstacleToDestroy.queuedForDestruction)
                {
                    obstaclesToProcessForSpawns.Add(obstacleToDestroy);
                    obstacleToDestroy.ParticleDestroy(Obstacle.ObstacleDestructionSource.MatchThree);
                    GameManager.Instance?.levelGoal?.RemoveObstacle(obstacleToDestroy);
                }
            }
        }
    }

    private void FillVerticalHorizontalLists()
    {
        verticalList = CombineObstacleLists(ObstacleListForward, ObstacleListBackward);
        horizontalList = CombineObstacleLists(ObstacleListLeft, ObstacleListRight);
        updownList = CombineObstacleLists(ObstacleListUp, ObstacleListDown);
    }

    private List<Obstacle> CombineObstacleLists(List<Obstacle> list1, List<Obstacle> list2)
    {
        HashSet<Obstacle> uniqueObstacles = new(list1);
        uniqueObstacles.UnionWith(list2);
        return new List<Obstacle>(uniqueObstacles);
    }

    private void CastRays()
    {
        ClearObstacleLists();
        AddObstacleToAllLists(Obstacle);

        CastRayFromObstacleCenter(Vector3.left);
        CastRayFromObstacleCenter(Vector3.right);
        CastRayFromObstacleCenter(Vector3.forward);
        CastRayFromObstacleCenter(Vector3.back);
        CastRayFromObstacleCenter(Vector3.up);
        CastRayFromObstacleCenter(Vector3.down);
    }

    private void AddObstacleToAllLists(Obstacle obstacle)
    {
        ObstacleListForward.Add(obstacle);
        ObstacleListBackward.Add(obstacle);
        ObstacleListLeft.Add(obstacle);
        ObstacleListRight.Add(obstacle);
        ObstacleListUp.Add(obstacle);
        ObstacleListDown.Add(obstacle);
    }

    private void CastRayFromObstacleCenter(Vector3 direction)
    {
        Vector3 rayOrigin = Obstacle.transform.position;
        float currentRayLength = rayLength;
        RaycastHit hit;

        while (Physics.Raycast(rayOrigin, direction, out hit, currentRayLength, obstacleLayer))
        {
            Obstacle hitObstacle = hit.collider.GetComponent<Obstacle>();
            if (hitObstacle != null && IsMatchingObstacle(Obstacle, hitObstacle))
            {
                AddToObstacleList(hitObstacle, direction);
                rayOrigin = hit.point + direction * 0.02f;
            }
            else break;
        }
    }

    private void AddToObstacleList(Obstacle hitObstacle, Vector3 direction)
    {
        if (direction == Vector3.left) AddObstacleToList(ObstacleListLeft, hitObstacle);
        else if (direction == Vector3.right) AddObstacleToList(ObstacleListRight, hitObstacle);
        else if (direction == Vector3.forward) AddObstacleToList(ObstacleListForward, hitObstacle);
        else if (direction == Vector3.back) AddObstacleToList(ObstacleListBackward, hitObstacle);
        else if (direction == Vector3.up) AddObstacleToList(ObstacleListUp, hitObstacle);
        else if (direction == Vector3.down) AddObstacleToList(ObstacleListDown, hitObstacle);
    }

    private void AddObstacleToList(List<Obstacle> list, Obstacle hitObstacle)
    {
        if (!list.Contains(hitObstacle)) list.Add(hitObstacle);
    }

    public void ClearObstacleLists()
    {
        ObstacleListLeft.Clear();
        ObstacleListRight.Clear();
        ObstacleListForward.Clear();
        ObstacleListBackward.Clear();
        ObstacleListUp.Clear();
        ObstacleListDown.Clear();
    }

    public List<Obstacle> ObstacleListForward = new();
    public List<Obstacle> ObstacleListBackward = new();
    public List<Obstacle> ObstacleListLeft = new();
    public List<Obstacle> ObstacleListRight = new();
    public List<Obstacle> ObstacleListUp = new();
    public List<Obstacle> ObstacleListDown = new();
}
