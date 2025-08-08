using System.Collections;
using UnityEngine;
public class PowerupCollectible : CollectibleItem
{

    public GameObject objectToDestroy, sprinkles;
    [SerializeField] private MeshRenderer mesh, icing;
    public PowerupType powerupType;
    private bool _beingPickedUp;
    private Player other;
    public ParticleSystem currencyParticleSystem;

    public Collider collectibleTrigger;
    private void Start()
    {
        _beingPickedUp = false;
    }
    public override void Collect(PlayerController player)
    {
        other = player.GetComponent<Player>();

        collectibleTrigger.enabled = false;
        switch (powerupType)
        {
            case PowerupType.Speed:
                other.BuffSpeed(8f, 1f);
                break;
            case PowerupType.Strength:
                other.BuffStrength(5f, 3f);

                sprinkles.SetActive(false);
                break;
            case PowerupType.Both:
                other.BuffStrengthAndSpeed(4f, 1f, 2f);
                break;
            case PowerupType.Energy:
                other.BuffEnergy(50f);
                break;
            case PowerupType.Helmet:
                StartCoroutine(FloatAndLand(other.helmet.transform, 1f, 10f, 10f));
                break;
            case PowerupType.Loot_Coins:
                AddCurrency("coins", 100);
                break;
            case PowerupType.Loot_Money:
                AddCurrency("money", 100);
                break;
            case PowerupType.Loot_Gems:
                AddCurrency("gems", 100);
                break;
        }
        if (icing != null)
        {
            icing.enabled = false;
        }
        if (powerupType != PowerupType.Helmet)
        {
            if (mesh != null)
                mesh.enabled = false;
            PlayCollectSound(objectToDestroy);
        }
    }
    private void AddCurrency(string currency, int amount)
    {
        PlayerPrefs.SetInt(currency, PlayerPrefs.GetInt(currency) + amount);
        ParticleSystem ps = Instantiate(currencyParticleSystem, transform.position, Quaternion.identity);
        ps.Play();
        Destroy(this.gameObject, 0.1f);
    }

    public bool Grounded;
    private IEnumerator FloatAndLand(Transform characterHead, float floatHeight, float floatSpeed, float landingSpeed)
    {
        if (other != null)
            transform.rotation = other.GetComponent<PlayerController>().transform.rotation;

        Animator anim = collectibleTrigger.transform.GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = false;
            anim.transform.rotation = Quaternion.identity; // Reset rotation to (0,0,0)
        }
        _beingPickedUp = true;

        Vector3 floatTarget = characterHead.position + Vector3.up * floatHeight; // Fixed target position
        Vector3 startPos = transform.position;
        float totalDistance = Vector3.Distance(startPos, floatTarget);
        float traveledDistance = 0f;

        float timeout = 3f; // Max time to float up
        float elapsedTime = 0f;

        // Floating up animation
        while (Vector3.Distance(transform.position, floatTarget) > 0.2f)
        {
            Vector3 previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, floatTarget, floatSpeed * Time.deltaTime);
            traveledDistance += Vector3.Distance(previousPosition, transform.position);

            float percentComplete = Mathf.Clamp01(traveledDistance / totalDistance);
            float rotationAmount = percentComplete * 360f;
            transform.rotation = Quaternion.Euler(rotationAmount, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

            elapsedTime += Time.deltaTime;
            if (elapsedTime > timeout)
            {
                Debug.LogWarning("Float animation timed out!");
                break;
            }
            yield return null;
        }

        transform.position = floatTarget;
        transform.rotation = other.GetComponent<PlayerController>().transform.rotation;

        // Landing animation
        Vector3 landingTarget = characterHead.position;
        timeout = 2f; // Max time to land
        elapsedTime = 0f;

        while (Vector3.Distance(transform.position, landingTarget) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, landingTarget, landingSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;

            if (elapsedTime > timeout)
            {
                Debug.LogWarning("Landing animation timed out!");
                break;
            }
            yield return null;
        }
        if (other != null) other.RepairHelmet();
        transform.position = landingTarget;
        if (mesh != null)
            mesh.enabled = false;
        PlayCollectSound(objectToDestroy);
    }


    private void Update()
    {
        Grounded = CheckForCollisions();
        if (!Grounded && !_beingPickedUp) ApplyFakeGravity();
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
    public LayerMask fallLayer;
    public bool CheckForCollisions()
    {
        Vector3 pos = parent.transform.position;
        Ray ray = new Ray(pos, Vector3.down);


        if (Physics.Raycast(ray, 0.53f, fallLayer)) return true;
        else return false;

    }
    public enum PowerupType
    {
        Speed, Strength, Both, Energy, Helmet, Loot_Coins, Loot_Money, Loot_Gems
    }
}
