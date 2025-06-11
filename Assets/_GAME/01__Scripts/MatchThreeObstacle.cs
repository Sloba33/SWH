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
        if (hasGameStarted && Obstacle.Moving)
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
        CheckListForMatches(verticalList);
        CheckListForMatches(horizontalList);
        CheckListForMatches(updownList);
    }

    private int consecutiveCount = 1;

    private void CheckListForMatches(List<Obstacle> obstacleList)
    {
        if (obstacleList.Count == 0) return; // Prevent errors with empty lists

        ObstacleType specialType;
        // ObstacleColor specialColor; // Not strictly needed here, as Universal handles it

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

        consecutiveCount = 1;
        Obstacle currentMatchTarget = obstacleList[0]; // Start with the first obstacle in the sequence

        for (int i = 1; i < obstacleList.Count; i++)
        {
            float distanceToGround = Vector3.Distance(Obstacle.transform.position, groundPosition);

            // Check if the current obstacle in the list matches the type and color of the 'origin' or the current match chain's target.
            // This is the core logic change.
            if (IsMatchingTypeAndColor(currentMatchTarget, obstacleList[i]) && distanceToGround < (transform.position.y + offset))
            {
                // Debug.Log("Distance to ground " + distanceToGround);
                consecutiveCount++;
                // Debug.Log("Consecutive count" + consecutiveCount);
                if (consecutiveCount >= 3)
                {
                    if (CheckAndHandleJackInTheBox(obstacleList))
                    {
                        // JackInTheBox found, cancel match (or special handling)
                        return;
                    }
                    DestroyConsecutiveObstacles(obstacleList, i, consecutiveCount);
                    return; // Stop checking this list after a match is found and destroyed
                }
            }
            // Handling for cases where the origin obstacle or the comparison obstacle is Universal.
            // This assumes 'Universal' means it matches anything for its type, and implicitly, its color.
            else if (currentMatchTarget.obstacleType == ObstacleType.Universal || obstacleList[i].obstacleType == ObstacleType.Universal)
            {
                // If either is Universal, it counts as a match for the purpose of extending a chain
                // (e.g., Red-Universal-Red still counts as a match of Red).
                // If the universal is the first in the chain, it needs to 'adopt' the type/color of the next non-universal.
                // The current implementation of IsMatchingTypeAndColor handles this implicitly by comparing to a specific type/color,
                // so we just need to ensure the count continues for universal.

                // For simplicity, let's keep the existing `Universal` logic as is, but it might need
                // more specific rules if `Universal` should behave differently in chains.
                // The most robust way to handle `Universal` is usually within the `IsMatchingTypeAndColor` itself.

                // Re-evaluating the original `else if` block:
                // `Obstacle.obstacleType == ObstacleType.Universal && obstacleList[0].obstacleType == ObstacleType.Universal`
                // This original logic specifically checked if *this* obstacle (the one the script is on)
                // AND the *first* obstacle in the list are Universal. This is less flexible.
                // It's better to integrate Universal checks into IsMatchingTypeAndColor.

                // Let's modify this to use the IsMatchingTypeAndColor method more universally.
                // The current `IsMatchingTypeAndColor` handles Universal types.
                // So, if it's NOT a normal match, and it's NOT a universal, then reset.
                consecutiveCount = 1;
                currentMatchTarget = obstacleList[i]; // Start new potential sequence
            }
            else
            {
                consecutiveCount = 1;
                currentMatchTarget = obstacleList[i]; // Start new potential sequence
            }
        }
    }

    // *** MODIFIED: New IsMatchingTypeAndColor method ***
    private bool IsMatchingTypeAndColor(Obstacle mainObstacle, Obstacle compareObstacle)
    {
        // If either obstacle is Universal, it matches the other's type and color.
        if (mainObstacle.obstacleType == ObstacleType.Universal || compareObstacle.obstacleType == ObstacleType.Universal)
        {
            return true; // Universal matches anything
        }

        // Otherwise, match based on both type AND color
        return (mainObstacle.obstacleType == compareObstacle.obstacleType &&
                mainObstacle.obstacleColor == compareObstacle.obstacleColor);
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

    // *** MODIFIED: Added consecutiveCount parameter to DestroyConsecutiveObstacles ***
    private void DestroyConsecutiveObstacles(List<Obstacle> obstacleList, int i, int count, ObstacleType? specialType = null)
    {
        // The loop needs to iterate 'count' times backwards from 'i'
        for (int j = i - count + 1; j <= i; j++)
        {
            // Ensure index is valid
            if (j >= 0 && j < obstacleList.Count)
            {
                if (!obstacleList[j].queuedForDestruction)
                {
                    // The specialType logic for `destructionParticleSystem` might need re-evaluation
                    // with the new ObstacleData approach for `Universal` types.
                    // For now, retaining original logic for `specialType` but it might be redundant
                    // if `destructionParticleSystem` is now consistently on `ObstacleData`.
                    if (specialType.HasValue && j == 0) // This logic seems specific to the first element in the list
                    {
                        // This implies the special type sets the particle system for the 'origin' obstacle.
                        // With the new structure, the particle system should ideally be driven by the ObstacleData
                        // associated with each obstacle.
                        // Consider if this line is still needed or if `obstacleList[j].ParticleDestroy()`
                        // (which relies on `obstacleList[j].destructionParticleSystem`) is sufficient.
                        obstacleList[j].destructionParticleSystem = obstacleList[1].destructionParticleSystem;
                    }
                    obstacleList[j].ParticleDestroy();
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

            // *** MODIFIED: Use IsMatchingTypeAndColor for raycast condition ***
            // This is the check for whether a hit obstacle *should* be added to a directional list.
            // It should only be added if it's the same type AND color, or if it's a Universal.
            if (hitObstacle != null && IsMatchingTypeAndColor(Obstacle, hitObstacle)) // Use the script's own Obstacle as the reference
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
            // Removed the `if (!list.Contains(Obstacle))` and `list.Insert(0, Obstacle);`
            // because `Obstacle` is now added to all lists at the beginning of `CastRays()`.
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