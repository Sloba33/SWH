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
    public bool canHit = true; // Default to true, player can hit
    public bool hittingDown; // Exposed for PlayerAnimation
    private PlayerController playerController;
    public Player player;
    LevelGoal levelGoal;
    private Animator _anim;
    public WeaponSpecialRadius weaponSpecialRadius;

    private PlayerInputHandler _inputHandler; // New reference to PlayerInputHandler

    private IEnumerator Start()
    {
        levelGoal = FindObjectOfType<LevelGoal>();
        yield return new WaitForSeconds(0.1f);

        playerController = GetComponent<PlayerController>();
        player = GetComponent<Player>();
        _anim = GetComponent<Animator>();
        _inputHandler = GetComponent<PlayerInputHandler>(); // Get PlayerInputHandler reference

        if (headSmashCollider != null) // Safety check
        {
            headSmashCollider.playerController = playerController;
        }

        SpawnWeapon();
        if (backWeaponSlot != null && weapon != null) // Safety check
        {
            backWeapon = Instantiate(weapon.gameObject, backWeaponSlot.transform);
            backWeapon.GetComponent<Weapon>().WeaponStandard.gameObject.SetActive(true);
            backWeapon.GetComponent<Weapon>().WeaponDown.gameObject.SetActive(false);
        }
        CheckWeaponAvailability();
    }

    // New Update method to check for keyboard/gamepad input
    private void Update()
    {
        if (_inputHandler == null) return;

        // Handle regular hit
        if (_inputHandler.GetHitPressedThisFrame()) // Uses Input System's WasPressedThisFrame
        {
            Hit();
        }

        // Handle hit down
        if (_inputHandler.GetHitDownPressedThisFrame()) // Uses Input System's WasPressedThisFrame
        {
            HitDown();
        }

        // Handle special attack
        if (_inputHandler.GetSpecialAttackPressedThisFrame()) // Uses Input System's WasPressedThisFrame
        {
            SpecialAttack();
        }
    }

    private void CheckWeaponAvailability()
    {
        if (weapon == null) return; // Safety check

        if (PlayerPrefs.GetInt(weapon.weaponType.ToString()) == 1)
        {
            if (backWeapon != null) backWeapon.SetActive(true);
            Debug.Log("Enabling weapon since its unlocked");
        }
        else
        {
            if (backWeapon != null) backWeapon.SetActive(false);
        }
    }

    public Vector3 hitPoint; //
    public Obstacle FindHitObstacle()
    {
        // Ensure playerController is not null before accessing its members
        if (playerController == null)
        {
            Debug.LogError("PlayerAttack: playerController is null. Cannot find hit obstacle.");
            return null;
        }
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
        // Ensure necessary references are not null before proceeding
        if (player == null || weapon == null || _anim == null || playerController == null)
        {
            Debug.LogError("PlayerAttack.Hit: Missing required references. Cannot perform attack.");
            return;
        }

        if (canHit)
        {
            if (!playerController.AI) //
            {
                if (player.Energy < weapon.energyConsumption) return; //
                else player.SpendEnergy(weapon.energyConsumption); //
            }
            Debug.Log("Hitting");
            canHit = false; //
            if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(false); //
            if (weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(true); //

            ObstacleToHit = FindHitObstacle(); //
            if (ObstacleToHit != null && levelGoal != null && !levelGoal.Tutorial) //
            {
                player.specialCharges++; //
                if (player.specialCharges >= player.specialChargesMax && player.pc != null && player.pc.specialButton != null) //
                {
                    player.pc.specialButton.gameObject.SetActive(true); //
                }
            }

            _anim.SetBool("Hit", true); //
            // AudioManager.Instance.PlayPlayerSound("hit", transform.position); //
            StartCoroutine(FinishHit()); //
        }
    }

    public void SpecialAttack()
    {
        // Ensure necessary references are not null before proceeding
        if (player == null || weapon == null || _anim == null || playerController == null)
        {
            Debug.LogError("PlayerAttack.SpecialAttack: Missing required references. Cannot perform special attack.");
            return;
        }

        if (canHit)
        {
            if (!playerController.AI) //
            {
                if (player.specialCharges < player.specialChargesMax) return; //
                else
                {
                    player.specialCharges = 0; //
                    if (player.pc != null && player.pc.specialButton != null) player.pc.specialButton.gameObject.SetActive(false); //
                }
            }
            canHit = false; //
            _anim.SetBool("HitSpecial", true); //
            if (weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(true); //
            StartCoroutine(FinishSpecial()); //
        }
    }

    public IEnumerator FinishSpecial()
    {
        if (playerController != null) playerController.HitJump(); // Ensure playerController is not null
        yield return new WaitForSeconds(delayBeforeSwing); //

        yield return new WaitForSeconds(delayAfterSwing); //
        yield return new WaitForSeconds(0.15f); //

        if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = false; //

        if (useTrail) //
        {
            if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = true; //
        }
        else
        {
            if (weaponSpecialSwingParticle != null) weaponSpecialSwingParticle.Play(); //
        }
        yield return new WaitForSeconds(0.4f); //
        PerformSpecialAttack(); //

        if (weaponSpecialAOE != null && playerController != null) // Safety checks
        {
            ParticleSystem ps = Instantiate(weaponSpecialAOE, playerController.WallDetectPosition, weaponSpecialAOE.transform.rotation); //
            ps.Play(); //
        }
        yield return new WaitForSeconds(0.1f); //
        _anim.SetBool("HitSpecial", false); //
        if (playerController != null) playerController.canMove = true; //
        if (weapon != null) weapon.WeaponStandard.SetActive(false); //

        canHit = true; //
    }

    public List<BoxCollider> weaponRadiusColliders = new(); //
    public BoxCollider currentSpecialRadiusTrigger; //
    public void PerformSpecialAttack()
    {
        if (currentSpecialRadiusTrigger == null) return; //

        // Get all colliders within the trigger area
        Collider[] hitColliders = Physics.OverlapBox(
            currentSpecialRadiusTrigger.bounds.center,
            currentSpecialRadiusTrigger.bounds.extents,
            currentSpecialRadiusTrigger.transform.rotation
        );
        Debug.Log("" + currentSpecialRadiusTrigger.bounds.center); //
        // Loop through the colliders and destroy obstacles
        foreach (Collider col in hitColliders) //
        {
            if (col.CompareTag("Obstacle")) // Assuming obstacles have the tag "Obstacle"
            {
                Obstacle obs = col.GetComponent<Obstacle>(); //
                if (obs != null) obs.ParticleDestroy(); //
            }
        }
    }

    public void SetActiveWeaponSpecialCollider(int index) //
    {
        for (int i = 0; i < weaponRadiusColliders.Count; i++) //
        {
            if (i == index) //
            {
                weaponRadiusColliders[i].enabled = true; //
                currentSpecialRadiusTrigger = weaponRadiusColliders[i]; //
            }
            else weaponRadiusColliders[i].enabled = false; //
        }
    }

    public void SetWeaponSpecial(WeaponSpecialRadius weaponSpecialRadius) //
    {
        switch (weaponSpecialRadius) //
        {
            case WeaponSpecialRadius.Small: //
                SetActiveWeaponSpecialCollider(0); //
                break;
            case WeaponSpecialRadius.Medium: //
                SetActiveWeaponSpecialCollider(1); //
                break;
            case WeaponSpecialRadius.Large: //
                SetActiveWeaponSpecialCollider(2); //
                break;
            default: //
                Debug.LogWarning("Unknown weapon special type."); //
                break;
        }
    }

    public void HitDown()
    {
        // Ensure necessary references are not null before proceeding
        if (player == null || weapon == null || _anim == null || playerController == null)
        {
            Debug.LogError("PlayerAttack.HitDown: Missing required references. Cannot perform attack.");
            return;
        }

        if (canHit && playerController.grounded) //
        {
            playerController.canMove = false; //
            playerController.canPush = false; //
            canHit = false; //

            // Check if player is grounded using PlayerMovement's IsGrounded
            if (playerController.IsGrounded) //
            {
                hittingDown = true; //
                if (!playerController.AI) //
                {
                    if (player.HitDownEnergy < weapon.energyConsumption) //
                    {
                        // Re-enable movement and hit if energy is insufficient
                        playerController.canMove = true;
                        playerController.canPush = true;
                        canHit = true;
                        hittingDown = false; // Reset hittingDown if attack is aborted
                        return; //
                    }
                    else player.SpendHitDownEnergy(weapon.energyConsumption); //
                }

                // Direction and rotation logic for HitDown
                if (playerController._ground.Length > 0 && playerController._ground[0] != null) // Check if _ground array has an element
                {
                    Vector3 directionToCenter = playerController._ground[0].transform.position - transform.position; //
                    directionToCenter.y = 0; // Project onto the XZ plane
                    float distanceToCenter = directionToCenter.magnitude; //
                    float deadzoneRadius = 0.25f; // Adjust this value as needed

                    if (distanceToCenter > deadzoneRadius) //
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(directionToCenter, Vector3.up); //
                        transform.rotation = Quaternion.Euler(0f, lookRotation.eulerAngles.y, 0f); //
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerAttack.HitDown: No ground obstacle detected for direction calculation.");
                }
            }
            else
            {
                Debug.LogWarning("PlayerAttack.HitDown: Player not grounded, cannot perform HitDown.");
                // Reset states if HitDown was attempted while not grounded
                playerController.canMove = true;
                playerController.canPush = true;
                canHit = true;
                hittingDown = false;
                return;
            }

            // Ensure _ground has an element before trying to access it
            ObstacleToHit = (playerController._ground.Length > 0 && playerController._ground[0] != null)
                ? playerController._ground[0].transform.GetComponent<Obstacle>()
                : null;

            _anim.SetBool("HitDown", true); //
            if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(false); //
            if (weapon.WeaponDown != null) weapon.WeaponDown.SetActive(true); //
            // AudioManager.Instance.PlayPlayerSound("hit", transform.position); //
            StartCoroutine(FinishHitDown()); //
        }
    }

    public float delayBeforeSwing, delayAfterSwing; //
    public bool useTrail; //
    public IEnumerator FinishHit()
    {
        yield return new WaitForSeconds(delayBeforeSwing); //

        if (useTrail) //
        {
            if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = true; //
        }
        else
        {
            if (weaponSwingParticle != null) weaponSwingParticle.Play(); //
        }

        yield return new WaitForSeconds(delayAfterSwing); //
        if (ObstacleToHit != null && ObstacleToHit.isHammerable) //
        {
            if (weaponHitParticle != null) // Safety check
            {
                ParticleSystem PSHit = Instantiate(weaponHitParticle, hitPoint, weaponHitParticle.transform.rotation); //
            }
            ObstacleToHit.ParticleDestroy(); // Call directly on the ObstacleToHit instance
        }
        yield return new WaitForSeconds(0.15f); //
        if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = false; //
        yield return new WaitForSeconds(0.15f); //
        _anim.SetBool("Hit", false); //
        if (weapon != null) weapon.WeaponStandard.SetActive(false); //
        if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(true); //
        if (playerController != null) playerController.canMove = true; //
        canHit = true; //
    }

    public IEnumerator FinishHitDown()
    {
        yield return new WaitForSeconds(0.2f); //
        if (ObstacleToHit != null && ObstacleToHit.isHammerable) //
        {
            ObstacleToHit.ParticleDestroy(); // Call directly on the ObstacleToHit instance
        }
        yield return new WaitForSeconds(0.3f); //
        _anim.SetBool("HitDown", false); //
        if (weapon != null) weapon.WeaponDown.SetActive(false); //
        if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(true); //
        if (playerController != null) playerController.canMove = true; //
        canHit = true; //
        if (playerController != null) playerController.canPush = true; //
        hittingDown = false; //
    }

    public void SpawnWeapon()
    {
        if (weapon == null || weaponParentTransform == null)
        {
            Debug.LogError("PlayerAttack.SpawnWeapon: Weapon or WeaponParentTransform is null.");
            return;
        }
        Weapon wep = Instantiate(weapon, weaponParentTransform);
        weapon = wep;
    }
}