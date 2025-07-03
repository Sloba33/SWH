using Coherence.Toolkit;
using UnityEngine;

public class MultiplayerLevelManager : MonoBehaviour
{
    [SerializeField] private GameObject MultiplayerBoxesPrefab;

    [Sync]
    public bool spawnedBoxes;

    public void Start()
    {
        SpawnBoxes();
        GameManager.Instance.SpawnPlayer();
    }

    public void SpawnBoxes()
    {
        if (spawnedBoxes)
            return;

        Instantiate(MultiplayerBoxesPrefab);
        spawnedBoxes = true;
    }
}
