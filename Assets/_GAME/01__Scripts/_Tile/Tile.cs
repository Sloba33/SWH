using System.Collections.Generic;
using UnityEngine;
using Obstacles;

public class Tile : MonoBehaviour
{
    public List<Tile> neighbourList = new();
    public bool hasObject;

    public float speed;
    // default speed is 0.003f
    public float y;
    public bool isObstaclePositioned, isStartingObstacle;
    public Dictionary<Obstacle, float> obstacleDict = new Dictionary<Obstacle, float>();
   
    public ParticleSystem ps;
    private void Start()
    {
        if (hasObject)
        {
            isStartingObstacle = true;
        }
    }
    public void ForceRepositionObstacle(Obstacle obstacle)
    {
        Vector3 targetPosition;
        targetPosition = new Vector3(transform.position.x, obstacle.transform.position.y, transform.position.z);
        obstacle.transform.position = targetPosition;
        obstacle.isPositioned = true;
    }
    public void RepositionObstacle(Obstacle obstacle)
    {
        if (obstacle == null) return;
        if (obstacle.beingSucked) { Debug.Log("Sucking"); return; }
        bool beingMoved = obstacle.isBeingPushed || obstacle.isBeingPulled;
        if (!obstacle.isPositioned && !beingMoved)
        {
            Vector3 targetPosition;
            targetPosition = new Vector3(transform.position.x, obstacle.transform.position.y, transform.position.z);
            obstacle.transform.position = Vector3.MoveTowards(obstacle.transform.position, targetPosition, speed * 7.5f);
            Vector3 newTargetPos = new Vector3(targetPosition.x, obstacle.transform.position.y, transform.position.z);
            if (Vector3.Distance(obstacle.transform.position, newTargetPos) < 0.001f && !obstacle.isHeightPositioned)
            {
                obstacle.transform.position = targetPosition;
                obstacle.isPositioned = true;
                // obstacle.GetComponent<MatchThreeObstacle>().ClearObstacleLists();
            }
        }
        else if (obstacle.obstacleUp && !obstacle.wasRecentlyPulled)
        {
            Vector3 targetPosition;
            targetPosition = new Vector3(transform.position.x, obstacle.transform.position.y, transform.position.z);
            if (obstacle.isBeingPushed)
            {
                Debug.Log("beingpulled so its slower");
                obstacle.transform.position = Vector3.MoveTowards(obstacle.transform.position, targetPosition, speed * 1.5f);
            }
            else
            {

                obstacle.transform.position = Vector3.MoveTowards(obstacle.transform.position, targetPosition, speed * 3);
                Debug.Log("beingpulled so its slower");
            }

            Vector3 newTargetPos = new Vector3(targetPosition.x, obstacle.transform.position.y, transform.position.z);
            if (Vector3.Distance(obstacle.transform.position, newTargetPos) < 0.001f && !obstacle.isHeightPositioned)
            {
                obstacle.transform.position = targetPosition;
                obstacle.isPositioned = true;
                if (obstacle.obstacleType == ObstacleType.Galaxy || obstacle.obstacleType == ObstacleType.Cardboard)
                {
                    obstacle.SphereFlags();
                    if (obstacle._obstacleForward[0] != null) obstacle._obstacleForward[0].GetComponent<Obstacle>().SphereFlags();
                    if (obstacle._obstacleBack[0] != null) obstacle._obstacleBack[0].GetComponent<Obstacle>().SphereFlags();
                    if (obstacle._obstacleLeft[0] != null) obstacle._obstacleLeft[0].GetComponent<Obstacle>().SphereFlags();
                    if (obstacle._obstacleRight[0] != null) obstacle._obstacleRight[0].GetComponent<Obstacle>().SphereFlags();
                    obstacle.CheckThreeOfAKind();
                }
            }
        }
        if (obstacle.obstacleType == ObstacleType.Galaxy || obstacle.obstacleType == ObstacleType.Cardboard)
        {
            obstacle.SphereFlags();
            obstacle.CheckThreeOfAKind();
        }

    }
    public void PositionHeight(Obstacle obstacle)
    {

        if (!GameManager.Instance.start)
        {
            if (!obstacle.isHeightPositioned)
            {
                if (obstacle.FallTimer <= 0)
                    obstacle.FallTimer = 1f;
                Vector3 targetPosition;
                if (obstacleDict.Count == 1 && obstacleDict.ContainsKey(obstacle))
                {

                    y = 0f;
                    targetPosition = new Vector3(transform.position.x, y, transform.position.z);
                }
                else
                {
                    targetPosition = new Vector3(transform.position.x, obstacleDict.Count - 1, transform.position.z);

                }

                obstacle.transform.position = Vector3.MoveTowards(obstacle.transform.position, targetPosition, speed * 15);
                float diff = obstacle.transform.position.y - y;

                if ((diff) < 0.001f && obstacle.isPositioned && !obstacle.isHeightPositioned)
                {


                    obstacle.GetComponent<Obstacle>().isHeightPositioned = true;
                    obstacle.transform.position = targetPosition;
                }
            }
        }
        obstacle.hasFallStarted = false;


    }
    [NaughtyAttributes.Button]
    public void PrintDict()
    {
        foreach (KeyValuePair<Obstacle, float> ObstacleInDictionary in obstacleDict)
        {
            //Now you can access the key and value both separately from this attachStat as:
            Debug.Log("Obstacle " + ObstacleInDictionary.Key + "Height :" + ObstacleInDictionary.Value);
        }
    }

}

