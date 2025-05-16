using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageItem : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ScrollableItem"))
        {
            Debug.Log("yes");
        }
    }
}
