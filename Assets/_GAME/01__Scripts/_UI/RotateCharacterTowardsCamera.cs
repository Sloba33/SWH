using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public Transform targetCamera; // Drag the camera into this slot in the inspector or assign it via code

    public bool x_flag = false;
    public bool y_flag = true;  // Default to true for Y-axis rotation
    public bool z_flag = false;

    void Update()
    {
        if (targetCamera)
        {
            Vector3 directionToCamera = targetCamera.position - transform.position;
            
            // Determine the axes to lock
            bool lockX = !x_flag;
            bool lockY = !y_flag;
            bool lockZ = !z_flag;

            directionToCamera.x *= lockX ? 0 : 1;
            directionToCamera.y *= lockY ? 0 : 1;
            directionToCamera.z *= lockZ ? 0 : 1;

            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
        else
        {
            Debug.LogWarning("Target camera is not assigned!");
        }
    }
}
