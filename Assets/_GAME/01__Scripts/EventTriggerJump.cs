using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventTriggerJump : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.GetType() == typeof(CapsuleCollider))
            {
                PlayerController pcontroller = other.GetComponent<PlayerController>();
                pcontroller.playerControls.handJump.gameObject.SetActive(true);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.GetType() == typeof(CapsuleCollider))
            {
                PlayerController pcontroller = other.GetComponent<PlayerController>();
                pcontroller.playerControls.handJump.gameObject.SetActive(false);
            }
        }
    }
}
