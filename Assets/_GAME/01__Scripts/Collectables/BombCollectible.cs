using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes.Test;
using UnityEngine;

public class BombCollectible : CollectibleItem
{
    public MeshRenderer mesh;
    public SphereCollider sphereCollider;
    public GameObject objectToDestroy;
    public BombType bombType;
    public Bomb bombPrefab;
    bool isCollected;
    // private AudioSource audioSource;

    public override void Collect(PlayerController player)
    {
        if (!isCollected)
        {
            isCollected = true;
            mesh.enabled = false;
            sphereCollider.enabled = false;
            PlayCollectSound(objectToDestroy);
            player.GetComponent<Player>().pc.AddConsumable(this);
        }


    }

    public bool Grounded;
    private void Update()
    {
        Grounded = CheckForCollisions();
        if (!Grounded)
        {
            ApplyFakeGravity();
        }
        else
        {
            // Snap or smoothly adjust the position to the ground level
            AdjustToGroundLevel();
        }
    }

    private float fakeGravity = -10f;
    public Transform parent;

    void ApplyFakeGravity()
    {
        // Calculate the fake gravity force vector (in the negative y direction)
        Vector3 gravityForce = new Vector3(0, -fakeGravity, 0);

        // Apply the force to the bomb
        parent.transform.position = Vector3.MoveTowards(parent.transform.position, new Vector3(parent.transform.position.x, 0, parent.transform.position.z), 0.003f * 20);
    }

    void AdjustToGroundLevel()
    {
        Vector3 pos = parent.transform.position;

        // Adjust the y position to be on top of the ground
        float groundHeight = Mathf.Round(pos.y); // Use Mathf.Round to snap to the nearest whole number
        pos.y = Mathf.Lerp(pos.y, groundHeight, Time.deltaTime * 10  ); // Smooth adjustment

        parent.transform.position = pos;
    }

    [SerializeField] LayerMask _groundMask;
    public bool CheckForCollisions()
    {
        Vector3 pos = parent.transform.position;
        Ray ray = new Ray(pos, Vector3.down);

        // Check if the ray hits something within the specified distance
        if (Physics.Raycast(ray, 0.525f, _groundMask))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public enum BombType
    {
        Universal, Red, Green, Blue, Black, Yellow, None
    }
}
