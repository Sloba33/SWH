using System.Collections;
using System.Collections.Generic;
using Coherence;
using Coherence.Toolkit;
using NaughtyAttributes.Test;
using UnityEngine;

public class BombCollectible : CollectibleItem
{
    public MeshRenderer mesh;
    public SphereCollider sphereCollider;
    public GameObject objectToDestroy;
    public ObstacleColor bombColor; // Changed from ObstacleColor to BombType
    public Bomb bombPrefab;
    public CoherenceSync coherenceSync;
    // private AudioSource audioSource;

    private void Awake()
    {
        coherenceSync = GetComponent<CoherenceSync>();
    }

    public override void Collect(PlayerController player)
    {
        if (isCollected) return;

        Player other = player.GetComponent<Player>();

        // Real networked multiplayer collects only for this client's player. A
        // local bot match runs both locally, so let either collect (they only
        // overlap their own side's collectibles).
        bool networkedMp = GameManager.Instance.IsMultiplayer && !GameManager.Instance.IsBotMatch;
        if (networkedMp && GameManager.Instance.LocalPlayer != other) return;

        DisableCollectible();

        // The bomb goes into the consumable UI, which belongs to the human; a bot
        // just clears the collectible from the field.
        if (other == GameManager.Instance.LocalPlayer)
            other.pc.AddConsumable(this);

        if (networkedMp)
        {
            coherenceSync.SendCommand<BombCollectible>(nameof(CmdDisableCollectible), MessageTarget.Other);
        }
    }

    [Command(defaultRouting = MessageTarget.Other)]
    public void CmdDisableCollectible()
    {
        DisableCollectible();
    }

    private void DisableCollectible()
    {
        isCollected = true;
        mesh.enabled = false;
        sphereCollider.enabled = false;
        PlayCollectSound(objectToDestroy);
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
