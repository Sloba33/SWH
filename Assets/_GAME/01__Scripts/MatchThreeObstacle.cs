using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obstacles;
public class MatchThreeObstacle : MonoBehaviour
{
    public LayerMask obstacleLayer;
    public float rayLength = 0.9999f; // Use a fixed initial ray length
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
        ObstacleType specialType;
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

        for (int i = 1; i < obstacleList.Count; i++)
        {
            float distanceToGround = Vector3.Distance(Obstacle.transform.position, groundPosition);
            // Debug.Log("Distance to ground " + distanceToGround);
        
            if (IsMatchingType(obstacleList[i]) && distanceToGround < (transform.position.y + offset))
            {
                Debug.Log("Distance to ground " + distanceToGround);
                consecutiveCount++;
                Debug.Log("Consecutive count" + consecutiveCount);
                if (consecutiveCount >= 3)
                {
                    if (CheckAndHandleJackInTheBox(obstacleList))
                    {
                        // JackInTheBox found, cancel match
                        return;
                    }
                    DestroyConsecutiveObstacles(obstacleList, i);
                }
            }
            else if (Obstacle.obstacleType == ObstacleType.Universal && obstacleList[0].obstacleType == ObstacleType.Universal)
            {
                specialType = obstacleList[1].obstacleType;
                if (obstacleList[i].obstacleType == specialType)
                {
                    consecutiveCount++;
                    if (consecutiveCount >= 3 && distanceToGround < (transform.position.y + offset))
                    {
                        if (CheckAndHandleJackInTheBox(obstacleList))
                        {
                            // JackInTheBox found, cancel match
                            return;
                        }
                        DestroyConsecutiveObstacles(obstacleList, i, specialType);
                    }
                }
            }
            else
            {
                consecutiveCount = 1;
            }
        }
    }

    private bool IsMatchingType(Obstacle obstacle)
    {
        return obstacle.obstacleType == Obstacle.obstacleType || obstacle.obstacleType == ObstacleType.Universal;
    }

    private bool CheckAndHandleJackInTheBox(List<Obstacle> obstacleList)
    {
        foreach (Obstacle obstacle in obstacleList)
        {
            JackInTheBox jackInTheBox = obstacle.GetComponent<JackInTheBox>();
            if (jackInTheBox != null)
            {
                // Trigger JackInTheBox functionality
                jackInTheBox.TriggerJackInTheBox();
                return true;
            }
        }
        return false;
    }

    private void DestroyConsecutiveObstacles(List<Obstacle> obstacleList, int i, ObstacleType? specialType = null)
    {
        for (int j = i - consecutiveCount + 1; j <= i; j++)
        {
            if (!obstacleList[j].queuedForDestruction)
            {
                if (specialType.HasValue && j == 0)
                {
                    obstacleList[j].destructionParticleSystem = obstacleList[1].destructionParticleSystem;
                }
                obstacleList[j].ParticleDestroy();
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
        CastRayFromObstacleCenter(Vector3.left);
        CastRayFromObstacleCenter(Vector3.right);
        CastRayFromObstacleCenter(Vector3.forward);
        CastRayFromObstacleCenter(Vector3.back);
        CastRayFromObstacleCenter(Vector3.up);
        CastRayFromObstacleCenter(Vector3.down);
    }

    private void CastRayFromObstacleCenter(Vector3 direction)
    {
        Vector3 rayOrigin = Obstacle.transform.position;
        float currentRayLength = rayLength;
        RaycastHit hit;

        while (Physics.Raycast(rayOrigin, direction, out hit, currentRayLength, obstacleLayer))
        {
            Obstacle hitObstacle = hit.collider.GetComponent<Obstacle>();

            if (hitObstacle != null && (hitObstacle.obstacleType == Obstacle.obstacleType || hitObstacle.obstacleType == ObstacleType.Universal))
            {
                AddToObstacleList(hitObstacle, direction);
                rayOrigin = hit.point + direction * 0.02f;
            }
            else
            {
                break;
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
            if (!list.Contains(Obstacle))
            {
                list.Insert(0, Obstacle);
            }
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
