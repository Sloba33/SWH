using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class ImageShowDelay : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] float delay;
    void OnEnable()
    {
        if (image != null)
        {
            StartCoroutine(EnableImage(delay));
        }
    }
    private IEnumerator EnableImage(float delay)
    {
        yield return new WaitForSeconds(delay);
        image.enabled = true;
    }
}
