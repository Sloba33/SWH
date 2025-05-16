using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    public List<Chapter> Chapters = new();
    public Transform Content;
    public GameObject levels;

    private void Start()
    {
        foreach (Transform tr in Content)
        {
            if (tr.GetComponent<Chapter>() != null)
            {
                Chapters.Add(tr.GetComponent<Chapter>());
            }
        }
    }

}
