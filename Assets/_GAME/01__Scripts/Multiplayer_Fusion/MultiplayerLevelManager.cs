using System.Collections;
using Coherence.Toolkit;
using UnityEngine;

public class MultiplayerLevelManager : MonoBehaviour
{
    [SerializeField] private GameObject MultiplayerBoxesPrefab;

    [Sync]
    public bool spawnedBoxes;

    public void Start()
    {
        StartCoroutine(SpawnBoxes());
        GameManager.Instance.SpawnPlayer();
    }

    public IEnumerator SpawnBoxes()
    {
        yield return new WaitForSecondsRealtime(1f);
        // if (gameObject.name.StartsWith("[netw"))
        //     yield break;
        Debug.LogError("spawnedBoxes: " + spawnedBoxes);
        if (spawnedBoxes)
            yield break;

        Instantiate(MultiplayerBoxesPrefab);
        spawnedBoxes = true;
    }
}
