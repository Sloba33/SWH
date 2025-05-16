using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToggler : MonoBehaviour
{
    public void ToggleOff()
    {
        this.gameObject.SetActive(false);
    }
}
