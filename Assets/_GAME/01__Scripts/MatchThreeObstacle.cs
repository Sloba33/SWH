using System.Collections;
using System.Collections.Generic;
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

    private int consecutiveCount = 1;

    private void CheckListForMatches(List<Obstacle> obstacleList)
    {
        if (obstacleList == null || obstacleList.Count < 3)
            return;

        float offset;
        Vector3 groundPosition;
        if (groundObject == null)
        {
            groundPosition = Obstacle.tile.transform.position;
            offset = 0.556f;
        }
        else
        {
            groundPosition = groundObject.transform.position;
            offset = 0.05f;
        }

        int matchStart = 0;
        Obstacle currentMatchTarget = obstacleList[0];
        consecutiveCount = 1;

        for (int i = 1; i < obstacleList.Count; i++)
        {
            float distanceToGround = Vector3.Distance(Obstacle.transform.position, groundPosition);

            if (IsMatchingObstacle(currentMatchTarget, obstacleList[i]) && distanceToGround < (transform.position.y + offset))
            {
                consecutiveCount++;
            }
            else
            {
                if (consecutiveCount >= 3)
                {
                    // Handle match group from matchStart to i - 1
                    if (CheckAndHandleJackInTheBox(obstacleList.GetRange(matchStart, consecutiveCount)))
                        return;

                    DestroyConsecutiveObstacles(obstacleList, matchStart, consecutiveCount);
                    return; // Or remove this if you want to continue scanning for more matches
                }

                // Reset
                consecutiveCount = 1;
                matchStart = i;
                currentMatchTarget = obstacleList[i];
            }
        }

        // Final match check at end of list
        if (consecutiveCount >= 3)
        {
            if (CheckAndHandleJackInTheBox(obstacleList.GetRange(matchStart, consecutiveCount)))
                return;

            DestroyConsecutiveObstacles(obstacleList, matchStart, consecutiveCount);
        }
    }


    /// <summary>
    /// Checks if two obstacles match based on Type, Color, and Modifier (especially Universal).
    /// </summary>
    /// <param name="mainObstacle">The reference obstacle for comparison.</param>
    /// <param name="compareObstacle">The obstacle to compare against the reference.</param>
    /// <returns>True if they match, false otherwise.</returns>
    private bool IsMatchingObstacle(Obstacle mainObstacle, Obstacle compareObstacle)
    {
        // A null obstacle cannot match
        if (mainObstacle == null || compareObstacle == null) return false;

        // If either obstacle has a Universal modifier, it matches anything
        if (mainObstacle.obstacleModifier == ObstacleModifier.Universal || compareObstacle.obstacleModifier == ObstacleModifier.Universal)
        {
            return true;
        }

        // If either obstacle has a Universal type, it matches the other's type (and implicitly color for Universal type)
        // This part might need careful consideration if Universal type should *also* imply Universal modifier
        // For now, it means if one is universal type, it matches the other type, then we check color.
        if (mainObstacle.obstacleType == ObstacleType.Universal || compareObstacle.obstacleType == ObstacleType.Universal)
        {
            // If one is Universal type, and the other is a specific type, it still counts as a type match.
            // Then we must also check for color match.
            return (mainObstacle.obstacleColor == compareObstacle.obstacleColor ||
                    mainObstacle.obstacleColor == ObstacleColor.Universal ||
                    compareObstacle.obstacleColor == ObstacleColor.Universal);
        }

        // If neither is Universal in type or modifier, then match based on exact Type, Color, and Modifier
        // This assumes non-Universal modifiers must also match exactly if they exist.
        return (mainObstacle.obstacleType == compareObstacle.obstacleType &&
                mainObstacle.obstacleColor == compareObstacle.obstacleColor &&
                mainObstacle.obstacleModifier == compareObstacle.obstacleModifier);
    }

    private bool CheckAndHandleJackInTheBox(List<Obstacle> obstacleList)
    {
        Debug.Log("Checking for JackInTheBox in obstacle list of size: " + obstacleList.Count);
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
    private bool IsCenteredOnTile()
    {
        if (Obstacle == null || Obstacle.tile == null)
            return false;

        Vector3 obstaclePos = transform.position;
        Vector3 tileCenter = Obstacle.tile.transform.position;

        // Ignore Y-axis for distance check
        Vector2 obstacleXZ = new Vector2(obstaclePos.x, obstaclePos.z);
        Vector2 tileCenterXZ = new Vector2(tileCenter.x, tileCenter.z);

        float distanceXZ = Vector2.Distance(obstacleXZ, tileCenterXZ);

        return distanceXZ <= 0.2f; // Adjust threshold to taste
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
        HashSet<Obstacle> uniqueObstacles = new HashSet<Obstacle>(list1);
        uniqueObstacles.UnionWith(list2);
        return new List<Obstacle>(uniqueObstacles);
    }

    private void CastRays()
    {
        ClearObstacleLists(); // Ensure lists are clear before casting new rays
        // Add the obstacle itself to its own lists so it's always included in the checks
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

            // Use the new IsMatchingObstacle for raycast condition
            if (hitObstacle != null && IsMatchingObstacle(Obstacle, hitObstacle)) // Use the script's own Obstacle as the reference
            {
                AddToObstacleList(hitObstacle, direction);
                rayOrigin = hit.point + direction * 0.02f;
            }
            else
            {
                break; // Stop if no match or no obstacle hit
            }
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
        if (!list.Contains(hitObstacle))
        {
            list.Add(hitObstacle);
        }
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

    // List declarations for each direction
    public List<Obstacle> ObstacleListForward = new();
    public List<Obstacle> ObstacleListBackward = new();
    public List<Obstacle> ObstacleListLeft = new();
    public List<Obstacle> ObstacleListRight = new();
    public List<Obstacle> ObstacleListUp = new();
    public List<Obstacle> ObstacleListDown = new();
}
