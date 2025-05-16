using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    public List<GameObject> players = new();
    public List<GameObject> stands = new();
    public Transform spawnPosPlayer, spawnPosStand;
    public GameObject currentCharacter;
    public GameObject currentStand;

    // public void SetProfileCharacter(int index)
    // {
    //     currentCharacter = Instantiate(players[index], spawnPosPlayer, true);
    //     currentStand = Instantiate(stands[index], spawnPosStand, true);
    // }
}
