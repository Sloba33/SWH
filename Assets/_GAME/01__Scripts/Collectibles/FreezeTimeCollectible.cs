using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeTimeCollectible : CollectibleItem
{
    public MeshRenderer mesh;
    public List<MeshRenderer> MeshList = new();
    public SphereCollider sphereCollider;
    public GameObject objectToDestroy;
    private LevelGoal levelGoal;
    public float freezeDuration = 5f;
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
            RaiseConsumed();
            isCollected = true;
            if (MeshList.Count > 0)
            {
                for(int i = 0; i < MeshList.Count; i++)
                {
                    MeshList[i].enabled = false;
                }
            }
            mesh.enabled = false;
            sphereCollider.enabled = false;
            PlayCollectSound(objectToDestroy, freezeDuration + 0.1f);
            // player.GetComponent<Player>().pc.AddConsumable(this);
        }
    }

    /// <summary>
    /// Replay pickup: vanish + sound only. The time-freeze effect itself needs no
    /// replay action — the frozen obstacles' (non-)motion is baked into the
    /// recorded movement tracks.
    /// </summary>
    public override void ReplayCollect(Player ghostPlayer)
    {
        if (isCollected) return;
        isCollected = true;
        foreach (MeshRenderer m in MeshList)
        {
            if (m != null) m.enabled = false;
        }
        if (mesh != null) mesh.enabled = false;
        if (sphereCollider != null) sphereCollider.enabled = false;
        PlayCollectSound(objectToDestroy);
    }
}
