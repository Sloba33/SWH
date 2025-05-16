using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Obstacles;
public class GoalSetter : MonoBehaviour
{
    public bool AIGoal;
    public List<GoalSlot> playerGoals = new List<GoalSlot>();
    public List<GoalSlot> AIGoals = new List<GoalSlot>();
    public GoalSlot goalSlot;
    public LevelGoal levelGoal;
    public GameObject fillBar;
    private Image fillImage;
    private const float FillThreshold = 0.99f;

    private void Start()
    {
        levelGoal = FindObjectOfType<LevelGoal>();

        SetGoals();
        if (fillBar != null) fillImage = playerBar.transform.GetChild(0).GetComponent<Image>();
    }

    public List<Obstacle> obstacleTypes = new List<Obstacle>();
    public List<int> counts = new List<int>();
    public float playerFragment;
    public GameObject playerBar;

    public void SetGoals()
    {
        // Dictionary to store obstacle type counts along with obstacle references
        Dictionary<ObstacleType, List<Obstacle>> obstacleTypeCounts =
            new Dictionary<ObstacleType, List<Obstacle>>();
        // if (levelGoal.obstaclesToDestroy.Count ==0 || obstacleTypeCounts.Count == 0) return;
        if (GameManager.Instance.Recording)
            return;
        // Iterate through the list of obstacles
        if (!AIGoal)
            foreach (var obstacle in levelGoal.ObstaclesToDestroy_Player)
            {
                // Check if the obstacle type is already in the dictionary
                if (obstacleTypeCounts.ContainsKey(obstacle.obstacleType))
                {
                    // Add the obstacle to the list for this obstacle type
                    obstacleTypeCounts[obstacle.obstacleType].Add(obstacle);
                }
                else
                {
                    // Create a new list for this obstacle type and add the obstacle to it
                    obstacleTypeCounts[obstacle.obstacleType] = new List<Obstacle>() { obstacle };
                }
            }
        else
        {
            Debug.Log(" Obstacle type count ???? : " + obstacleTypeCounts.Count);
            foreach (var obstacle in levelGoal.ObstaclesToDestroy_AI)
            {
                // Check if the obstacle type is already in the dictionary
                if (obstacleTypeCounts.ContainsKey(obstacle.obstacleType))
                {
                    // Add the obstacle to the list for this obstacle type
                    obstacleTypeCounts[obstacle.obstacleType].Add(obstacle);
                }
                else
                {
                    // Create a new list for this obstacle type and add the obstacle to it
                    obstacleTypeCounts[obstacle.obstacleType] = new List<Obstacle>() { obstacle };
                }
            }
        }

        // Clear the obstacleTypes list before populating it
        obstacleTypes.Clear();
        int cnt = 0;
        // Output the obstacle types and their counts for debugging or further processing
        foreach (var kvp in obstacleTypeCounts)
        {
            Debug.Log("Obstacle Type: " + kvp.Key + ", Count: " + kvp.Value.Count);
            cnt = kvp.Value.Count;

            // Add one instance of the obstacle to the obstacleTypes list
            obstacleTypes.Add(kvp.Value[0]);
            counts.Add(kvp.Value.Count);
        }

        // Output the obstacle types added to the obstacleTypes list
        if (levelGoal.DualLevel)
        {
            this.transform.GetComponent<GridLayoutGroup>().enabled = false;
            playerBar = Instantiate(fillBar, this.transform);
            // if (AIGoal)
            //     playerBar.transform.GetChild(0).GetComponent<Image>().color = Color.red;
            // else
            //     playerBar.transform.GetChild(0).GetComponent<Image>().color = Color.blue;
            if (!AIGoal)
            {

                playerFragment = 1f / levelGoal.ObstaclesToDestroy_Player.Count;
                Debug.Log(" Player fragment is : " + playerFragment);
            }
            else if (AIGoal)
            {
                playerFragment = 1f / levelGoal.ObstaclesToDestroy_AI.Count;

                Debug.Log("AI fragment : " + playerFragment);
            }
        }
        else
        {
            int i = 0;
            foreach (var obstacle in obstacleTypes)
            {
                GoalSlot gs = Instantiate(goalSlot, transform);
                gs.sprite = obstacle.obstacleSprite;
                gs.amount = counts[i];
                gs.obstacleType = obstacle.obstacleType;
                playerGoals.Add(gs);
                i++;
            }
        }
    }

    public void FillBar()
    {
        if (fillImage == null)
        {
            Debug.LogError("FillImage is not assigned!");
            return;
        }

        fillImage.fillAmount += playerFragment;

        // Check fill amount using Mathf.Approximately for better precision handling
        if (fillImage.fillAmount >= FillThreshold)
        {
            if (!AIGoal)
            {
                StartCoroutine(levelGoal.WinLevel(1f));
                Debug.Log("Winning");
            }
            else
            {
                StartCoroutine(levelGoal.LoseLevel());
                Debug.Log("Losing");
            }
        }

        Debug.Log($"Filling : {playerBar} to : {fillImage.fillAmount}");
    }
}
