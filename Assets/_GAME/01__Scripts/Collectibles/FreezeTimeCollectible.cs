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
                StartCoroutine(levelGoal.FreezeTime(freezeDuration));
            }
            isCollected = true;
            mesh.enabled = false;
            sphereCollider.enabled = false;
            PlayCollectSound(objectToDestroy, freezeDuration+0.1f);
            // player.GetComponent<Player>().pc.AddConsumable(this);
        }
    }
    
}
