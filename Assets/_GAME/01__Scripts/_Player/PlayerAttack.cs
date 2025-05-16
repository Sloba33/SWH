using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerAttack : MonoBehaviour
{
    public ParticleSystem weaponHitParticle;
    public ParticleSystem weaponSwingParticle;
    public ParticleSystem weaponSpecialSwingParticle;
    public ParticleSystem weaponSpecialAOE;
    public Transform weaponParentTransform;
    public HeadSmashCollider headSmashCollider;
    public List<Weapon> weapons = new();
    public int weaponIndex;
    public Weapon weapon;
    public GameObject backWeaponSlot;
    public GameObject backWeapon;
    [SerializeField] Obstacle ObstacleToHit;
    [SerializeField] GameObject tool, toolDown;
    [SerializeField] Vector3 hitRayOffset;
    [SerializeField] float hitRayDistance = 1f;
    public bool canHit, hittingDown;
    private PlayerController playerController;
    public Player player;
    LevelGoal levelGoal;
    private Animator _anim;
    public WeaponSpecialRadius weaponSpecialRadius;

    private IEnumerator Start()
    {
        levelGoal = FindObjectOfType<LevelGoal>(); ;
        yield return new WaitForSeconds(0.1f);

        playerController = GetComponent<PlayerController>();
        player = GetComponent<Player>();
        _anim = GetComponent<Animator>();
        headSmashCollider.playerController = playerController;
        SpawnWeapon();
        backWeapon = Instantiate(weapon.gameObject, backWeaponSlot.transform);
        backWeapon.GetComponent<Weapon>().WeaponStandard.gameObject.SetActive(true);
        backWeapon.GetComponent<Weapon>().WeaponDown.gameObject.SetActive(false);
        // PlayerPrefs.SetInt(weapon.weaponType.ToString(),1);
        CheckWeaponAvailability();

    }
    private void CheckWeaponAvailability()
    {
        if (PlayerPrefs.GetInt(weapon.weaponType.ToString()) == 1)
        {
            backWeapon.SetActive(true);
            Debug.Log("Enabling weapon since its unlocked");
        }
        else backWeapon.SetActive(false);
    }
    public Vector3 hitPoint;
    public Obstacle FindHitObstacle()
    {
        if (Physics.Raycast(transform.position + hitRayOffset, transform.forward, out RaycastHit hitObstacle, hitRayDistance, playerController._obstacleMask))
        {
            ObstacleToHit = hitObstacle.transform.GetComponent<Obstacle>();
            hitPoint = hitObstacle.point;
            return ObstacleToHit;
        }
        else return null;
    }
    public void Hit()
    {

        if (canHit)
        {
            if (!playerController.AI)
            {

                if (player.Energy < weapon.energyConsumption) return;
                else
                    player.SpendEnergy(weapon.energyConsumption);
            }
            Debug.Log("Hitting");
            canHit = false;
            backWeaponSlot.gameObject.SetActive(false);
            weapon.WeaponStandard.SetActive(true);
            // player.MoveSpeed = 1.5f;
            ObstacleToHit = FindHitObstacle();
            if (ObstacleToHit != null && !levelGoal.Tutorial)
            {
                player.specialCharges++;
                if (player.specialCharges >= player.specialChargesMax) player.pc.specialButton.gameObject.SetActive(true);
            }

            _anim.SetBool("Hit", true);
            if (!playerController.AI) AudioManager.Instance.PlayPlayerSound("hit", transform.position);
            StartCoroutine(FinishHit());
        }
    }
    public void SpecialAttack()
    {
        if (canHit)
        {
            if (!playerController.AI)
            {
                if (player.specialCharges < player.specialChargesMax) return;
                else
                {
                    player.specialCharges = 0;
                    player.pc.specialButton.gameObject.SetActive(false);

                }
            }
            canHit = false;
            _anim.SetBool("HitSpecial", true);
            // _anim.Play("SpecialAttack");
            weapon.WeaponStandard.SetActive(true);
            StartCoroutine(FinishSpecial());
        }
    }
    public IEnumerator FinishSpecial()
    {
        playerController.HitJump();
        yield return new WaitForSeconds(delayBeforeSwing);


        yield return new WaitForSeconds(delayAfterSwing);
        yield return new WaitForSeconds(0.15f);


        if (weapon.trailRenderer != null) weapon.trailRenderer.enabled = false;

        // yield return new WaitForSeconds(0.1f);

        if (useTrail)
        {
            weapon.trailRenderer.enabled = true;
        }
        else
        {
            weaponSpecialSwingParticle.Play();
        }
        yield return new WaitForSeconds(0.4f);
        PerformSpecialAttack();
        ParticleSystem ps = Instantiate(weaponSpecialAOE, playerController.WallDetectPosition, weaponSpecialAOE.transform.rotation);
        ps.Play();
        yield return new WaitForSeconds(0.1f);
        _anim.SetBool("HitSpecial", false);
        //  player.MoveSpeed = 2f;
        playerController.canMove = true;
        weapon.WeaponStandard.SetActive(false);
        // weaponSpecialAOE.Play();

        canHit = true;
    }
    public List<BoxCollider> weaponRadiusColliders = new();
    public BoxCollider currentSpecialRadiusTrigger;
    public void PerformSpecialAttack()
    {
        if (currentSpecialRadiusTrigger == null) return;

        // Get all colliders within the trigger area
        Collider[] hitColliders = Physics.OverlapBox(
            currentSpecialRadiusTrigger.bounds.center,
            currentSpecialRadiusTrigger.bounds.extents,
            currentSpecialRadiusTrigger.transform.rotation
        );
        Debug.Log("" + currentSpecialRadiusTrigger.bounds.center);
        // Loop through the colliders and destroy obstacles
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Obstacle")) // Assuming obstacles have the tag "Obstacle"
            {
                Obstacle obs = col.GetComponent<Obstacle>();
                if (obs != null) obs.ParticleDestroy();
            }
        }
    }
    public void SetActiveWeaponSpecialCollider(int index)
    {
        for (int i = 0; i < weaponRadiusColliders.Count; i++)
        {
            if (i == index)
            {
                weaponRadiusColliders[i].enabled = true;
                currentSpecialRadiusTrigger = weaponRadiusColliders[i];
            }
            else weaponRadiusColliders[i].enabled = false;
        }
    }
    public void SetWeaponSpecial(WeaponSpecialRadius weaponSpecialRadius)
    {
        switch (weaponSpecialRadius)
        {
            case WeaponSpecialRadius.Small:
                SetActiveWeaponSpecialCollider(0);
                break;
            case WeaponSpecialRadius.Medium:
                SetActiveWeaponSpecialCollider(1);
                break;
            case WeaponSpecialRadius.Large:
                SetActiveWeaponSpecialCollider(2);
                break;
            default:
                Debug.LogWarning("Unknown weapon special type.");
                break;

        }
    }
    public void HitDown()
    {

        if (canHit && playerController.grounded)
        {
            playerController.canMove = false;
            playerController.canPush = false;
            canHit = false;
            if (playerController.IsGrounded)
            {
                hittingDown = true;
                if (!playerController.AI)
                {

                    if (player.HitDownEnergy < weapon.energyConsumption)
                    {
                        playerController.canMove = true;
                        playerController.canPush = true;
                        canHit = true;
                        return;
                    }
                    else
                        player.SpendHitDownEnergy(weapon.energyConsumption);
                }
                Vector3 directionToCenter = playerController._ground[0].transform.position - transform.position;

                // Project the direction onto the XZ plane (ignore the Y component)
                directionToCenter.y = 0;

                // Calculate the distance from the player to the center of the obstacle
                float distanceToCenter = directionToCenter.magnitude;

                // Define a threshold for the deadzone
                float deadzoneRadius = 0.25f; // Adjust this value as needed

                if (distanceToCenter > deadzoneRadius)
                {
                    // Create a rotation that only affects the player's Y-axis (yaw)
                    Quaternion lookRotation = Quaternion.LookRotation(directionToCenter, Vector3.up);

                    // Apply the Y-axis rotation to the player's transform
                    transform.rotation = Quaternion.Euler(0f, lookRotation.eulerAngles.y, 0f);
                }
            }
            ObstacleToHit = playerController._ground[0].transform.GetComponent<Obstacle>();
            _anim.SetBool("HitDown", true);
            backWeaponSlot.gameObject.SetActive(false);
            weapon.WeaponDown.SetActive(true);
            if (!playerController.AI) AudioManager.Instance.PlayPlayerSound("hit", transform.position);
            StartCoroutine(FinishHitDown());
        }
    }
    public float delayBeforeSwing, delayAfterSwing;
    public bool useTrail;
    public IEnumerator FinishHit()
    {
        yield return new WaitForSeconds(delayBeforeSwing);

        if (useTrail)
        {
            weapon.trailRenderer.enabled = true;
        }
        else
        {
            weaponSwingParticle.Play();
        }


        yield return new WaitForSeconds(delayAfterSwing);
        if (ObstacleToHit != null && ObstacleToHit.isHammerable)
        {
            ParticleSystem PSHit = Instantiate(weaponHitParticle, hitPoint, weaponHitParticle.transform.rotation);
            GameObject originalObject = ObstacleToHit.transform.gameObject;
            originalObject.GetComponent<Obstacle>().ParticleDestroy();
        }
        yield return new WaitForSeconds(0.15f);
        if (weapon.trailRenderer != null) weapon.trailRenderer.enabled = false;
        yield return new WaitForSeconds(0.15f);
        _anim.SetBool("Hit", false);
        weapon.WeaponStandard.SetActive(false);
        backWeaponSlot.gameObject.SetActive(true);
        //  player.MoveSpeed = 2f;
        playerController.canMove = true;
        canHit = true;

    }

    public IEnumerator FinishHitDown()
    {
        yield return new WaitForSeconds(0.2f);
        if (ObstacleToHit != null && ObstacleToHit.isHammerable)
        {
            GameObject originalObject = ObstacleToHit.transform.gameObject;

            originalObject.GetComponent<Obstacle>().ParticleDestroy();
        }
        yield return new WaitForSeconds(0.3f);
        _anim.SetBool("HitDown", false);
        weapon.WeaponDown.SetActive(false);
        backWeaponSlot.gameObject.SetActive(true);
        playerController.canMove = true;
        canHit = true;
        playerController.canPush = true;
        hittingDown = false;
    }
    public void SpawnWeapon()
    {
        Weapon wep = Instantiate(weapon, weaponParentTransform);
        weapon = wep;
    }

}
