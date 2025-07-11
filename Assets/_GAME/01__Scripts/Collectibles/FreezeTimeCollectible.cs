using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeTimeCollectible : CollectibleItem
{
    public MeshRenderer mesh;
    public SphereCollider sphereCollider;
    public GameObject objectToDestroy;
    private LevelGoal levelGoal;
    public float freezeDuration = 5f;
    bool isCollected;
    // private AudioSource audioSource;

    public override void Collect(PlayerController player)
    {
        if (!isCollected)
        {
            levelGoal = FindFirstObjectByType<LevelGoal>();
            if (levelGoal != null)
            {
                Debug.Log("Freezing time for " + freezeDuration + " seconds.");
                StartCoroutine(FreezeTime());
            }
            isCollected = true;
            mesh.enabled = false;
            sphereCollider.enabled = false;
            PlayCollectSound(objectToDestroy);
            // player.GetComponent<Player>().pc.AddConsumable(this);
        }
    }
    private IEnumerator FreezeTime()
    {
        List<Obstacle> obstaclesToFreeze = levelGoal.spawnedObstacles;
        foreach (Obstacle obstacle in obstaclesToFreeze)
        {
            if (obstacle != null)
            {

                if (!obstacle.grounded)
                {

                    obstacle.FreezeFall(false);
                    Debug.Log("Freezing obstacle: " + obstacle.name);
                }
                

            }
        }
        yield return new WaitForSeconds(freezeDuration);
        foreach (Obstacle obstacle in obstaclesToFreeze)
        {
            if (obstacle != null)
            {
                if (!obstacle.grounded)
                {
                    obstacle.FreezeFall(true);
                    Debug.Log("Unfreezing obstacle: " + obstacle.name);
                }

            }
        }
    }
}
