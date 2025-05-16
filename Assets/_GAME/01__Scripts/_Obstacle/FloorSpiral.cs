using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorSpiral : MonoBehaviour
{
    public Collider holeTrigger;
    public Transform rotationSphere;
    public GameObject currentObject;
    [SerializeField] float rotationDuration;
    [SerializeField] float targetScale;
    private Vector3 initialSphereScale;
    private bool isRotating;
    private void Start()
    {
        initialSphereScale = rotationSphere.localScale;
    }
    private void FixedUpdate()
    {
        // if (currentObject != null)
        // {
        //     StartCoroutine(RotateAndScaleDown());
        // }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Obstacle"))
        {
            other.GetComponent<Obstacle>().enabled = false;
            other.GetComponent<Collider>().enabled = false;
            other.transform.parent = rotationSphere;
            currentObject = other.gameObject;
            StartCoroutine(RotateAndScaleDown());
        }
    }
    private IEnumerator RotateAndScaleDown()
    {
        isRotating = true;
        float elapsedTime = 0f;

        while (elapsedTime < rotationDuration)
        {
            // Calculate the rotation for this frame (360 degrees over rotationDuration)
            float rotationAmount = 180f * (Time.deltaTime / rotationDuration);
            rotationSphere.Rotate(0, rotationAmount, 0, Space.World);

            // Calculate the scale for this frame (lerp from initial scale to zero)
            float scaleAmount = Mathf.Lerp(1f, targetScale, elapsedTime / rotationDuration);
            rotationSphere.localScale = initialSphereScale * scaleAmount;

            // Increase the elapsed time
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Once the rotation and scaling are complete, reset the rotation and scale
        rotationSphere.localScale = initialSphereScale;
        rotationSphere.rotation = Quaternion.identity;
        isRotating = false;
        currentObject.SetActive(false);
        // currentObject = null;
        Destroy(currentObject);
    }
}
