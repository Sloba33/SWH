using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private PlayerMovement playerMovement;
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
    private bool canHit = true;
    public bool hittingDown;
    private PlayerController playerController;
    public Player player;
    LevelGoal levelGoal;
    private Animator _anim;
    public WeaponSpecialRadius weaponSpecialRadius;

    private PlayerInputHandler _inputHandler;

    private IEnumerator Start()
    {
        levelGoal = FindFirstObjectByType<LevelGoal>();
        yield return new WaitForSeconds(0.1f);
        playerMovement = GetComponent<PlayerMovement>();
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


    private void Update()
    {
        if (_inputHandler == null) return;

        if (_inputHandler.GetHitPressedThisFrame())
        {
            Hit();
        }

        if (_inputHandler.GetHitDownPressedThisFrame())
        {
            HitDown();
        }

        if (_inputHandler.GetSpecialAttackPressedThisFrame())
        {
            SpecialAttack();
        }
    }

    private void CheckWeaponAvailability()
    {
        if (weapon == null) return;

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

        if (playerMovement == null)
        {
            Debug.LogError("PlayerAttack: playerMovement is null. Cannot find hit obstacle.");
            return null;
        }
        if (Physics.Raycast(transform.position + hitRayOffset, transform.forward, out RaycastHit hitObstacle, hitRayDistance, playerMovement._obstacleMask))
        {
            ObstacleToHit = hitObstacle.transform.GetComponent<Obstacle>();
            hitPoint = hitObstacle.point;
            return ObstacleToHit;
        }
        else return null;
    }
    public bool IsAttacking()
    {
        return _anim.GetBool("Hit") || _anim.GetBool("HitSpecial") || _anim.GetBool("HitDown");
    }
    public void Hit()
    {

        if (player == null || weapon == null || _anim == null || playerMovement == null)
        {
            Debug.LogError("PlayerAttack.Hit: Missing required references. Cannot perform attack.");
            return;
        }

        if (canHit)
        {
            if (!playerController.AI) //
            {
                if (player.Energy < weapon.energyConsumption) return;
                else player.SpendEnergy(weapon.energyConsumption);
            }
            Debug.Log("Hitting");
            canHit = false; //
            if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(false);
            if (weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(true);

            ObstacleToHit = FindHitObstacle(); //
            if (ObstacleToHit != null && levelGoal != null && !levelGoal.Tutorial)
            {
                player.specialCharges++;
                if (player.specialCharges >= player.specialChargesMax && player.pc != null && player.pc.specialButton != null) //
                {
                    //SPECIAL ATTACK DISABLED
                    // player.pc.specialButton.gameObject.SetActive(true);
                }
            }

            _anim.SetBool("Hit", true); //

            StartCoroutine(FinishHit()); //
        }
    }

    public void SpecialAttack()
    {

        if (player == null || weapon == null || _anim == null || playerMovement == null)
        {
            Debug.LogError("PlayerAttack.SpecialAttack: Missing required references. Cannot perform special attack.");
            return;
        }

        if (canHit)
        {
            if (!playerController.AI)
            {
                if (player.specialCharges < player.specialChargesMax) return; //
                else
                {
                    player.specialCharges = 0;
                    if (player.pc != null && player.pc.specialButton != null) player.pc.specialButton.gameObject.SetActive(false); //
                }
            }
            canHit = false; //
            _anim.SetBool("HitSpecial", true);
            if (weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(true);
            StartCoroutine(FinishSpecial());
        }
    }

    public IEnumerator FinishSpecial()
    {
        // if (playerMovement != null) playerController.HitJump(); // HIT JUMP NEEDS MOVING TO PLAYERMOVEMENT
        yield return new WaitForSeconds(delayBeforeSwing);

        yield return new WaitForSeconds(delayAfterSwing);
        yield return new WaitForSeconds(0.15f);

        if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = false;

        if (useTrail) //
        {
            if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = true;
        }
        else
        {
            if (weaponSpecialSwingParticle != null) weaponSpecialSwingParticle.Play();
        }
        yield return new WaitForSeconds(0.4f);
        PerformSpecialAttack(); //

        if (weaponSpecialAOE != null && playerMovement != null)
        {
            ParticleSystem ps = Instantiate(weaponSpecialAOE, playerMovement.WallDetectPosition, weaponSpecialAOE.transform.rotation); //
            ps.Play(); //
        }
        yield return new WaitForSeconds(0.1f);
        _anim.SetBool("HitSpecial", false);
        if (playerMovement != null) playerMovement.CanMove = true;
        if (weapon != null) weapon.WeaponStandard.SetActive(false);

        canHit = true; //
    }

    public List<BoxCollider> weaponRadiusColliders = new();
    public BoxCollider currentSpecialRadiusTrigger;
    public void PerformSpecialAttack()
    {
        if (currentSpecialRadiusTrigger == null) return;


        Collider[] hitColliders = Physics.OverlapBox(
            currentSpecialRadiusTrigger.bounds.center,
            currentSpecialRadiusTrigger.bounds.extents,
            currentSpecialRadiusTrigger.transform.rotation
        );
        Debug.Log("" + currentSpecialRadiusTrigger.bounds.center);

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Obstacle"))
            {
                Obstacle obs = col.GetComponent<Obstacle>();
                if (obs != null) obs.ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);
            }
        }
    }

    public void SetActiveWeaponSpecialCollider(int index)
    {
        for (int i = 0; i < weaponRadiusColliders.Count; i++)
        {
            if (i == index) //
            {
                weaponRadiusColliders[i].enabled = true;
                currentSpecialRadiusTrigger = weaponRadiusColliders[i];
            }
            else weaponRadiusColliders[i].enabled = false;
        }
    }

    public void SetWeaponSpecial(WeaponSpecialRadius weaponSpecialRadius)
    {
        switch (weaponSpecialRadius) //
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
            default: //
                Debug.LogWarning("Unknown weapon special type.");
                break;
        }
    }

    public void HitDown()
    {

        if (player == null || weapon == null || _anim == null || playerMovement == null)
        {
            Debug.LogError("PlayerAttack.HitDown: Missing required references. Cannot perform attack.");
            return;
        }

        if (canHit && playerMovement.IsGrounded)
        {
            playerMovement.CanMove = false;
            playerController._movement.CanPush = false;
            canHit = false;


            if (playerController._movement.IsGrounded)
            {
                hittingDown = true;
                if (!playerController.AI)
                {
                    if (player.HitDownEnergy < weapon.energyConsumption)
                    {

                        playerMovement.CanMove = true;
                        playerController._movement.CanPush = true;
                        canHit = true;
                        hittingDown = false;
                        return;
                    }
                    else player.SpendHitDownEnergy(weapon.energyConsumption);
                }


                if (playerController._movement.groundHits.Length > 0 && playerController._movement.groundHits[0] != null)
                {
                    Vector3 directionToCenter = playerController._movement.groundHits[0].transform.position - transform.position;
                    directionToCenter.y = 0;
                    float distanceToCenter = directionToCenter.magnitude;
                    float deadzoneRadius = 0.25f;
                    if (distanceToCenter > deadzoneRadius)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(directionToCenter, Vector3.up);
                        transform.rotation = Quaternion.Euler(0f, lookRotation.eulerAngles.y, 0f);
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

                playerMovement.CanMove = true;
                playerController._movement.CanPush = true;
                canHit = true;
                hittingDown = false;
                return;
            }


            ObstacleToHit = (playerController._movement.groundHits.Length > 0 && playerController._movement.groundHits[0] != null)
                ? playerController._movement.groundHits[0].transform.GetComponent<Obstacle>()
                : null;

            _anim.SetBool("HitDown", true);
            if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(false);
            if (weapon.WeaponDown != null) weapon.WeaponDown.SetActive(true);
            // AudioManager.Instance.PlayPlayerSound("hit", transform.position); //
            StartCoroutine(FinishHitDown()); //
        }
    }

    public float delayBeforeSwing, delayAfterSwing;
    public bool useTrail;
    public IEnumerator FinishHit()
    {
        yield return new WaitForSeconds(delayBeforeSwing);

        if (useTrail)
        {
            if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = true;
        }
        else
        {
            if (weaponSwingParticle != null) weaponSwingParticle.Play();
        }

        yield return new WaitForSeconds(delayAfterSwing);
        if (ObstacleToHit != null && ObstacleToHit.isHammerable)
        {
            if (weaponHitParticle != null)
            {
                ParticleSystem PSHit = Instantiate(weaponHitParticle, hitPoint, weaponHitParticle.transform.rotation); //
            }
            ObstacleToHit.ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);
            // GameManager.Instance.levelGoal.QueueObstacleForSpawnProcessing(ObstacleToHit);
        }
        yield return new WaitForSeconds(0.15f);
        if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = false;
        yield return new WaitForSeconds(0.15f);
        _anim.SetBool("Hit", false); //
        if (weapon != null) weapon.WeaponStandard.SetActive(false);
        if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(true);
        if (playerController != null) playerMovement.CanMove = true;
        canHit = true;
    }

    public IEnumerator FinishHitDown()
    {
        yield return new WaitForSeconds(0.2f);
        if (ObstacleToHit != null && ObstacleToHit.isHammerable)
        {
            ObstacleToHit.ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);
            // GameManager.Instance.levelGoal.QueueObstacleForSpawnProcessing(ObstacleToHit);
        }
        yield return new WaitForSeconds(0.3f);
        _anim.SetBool("HitDown", false);
        if (weapon != null) weapon.WeaponDown.SetActive(false);
        if (backWeaponSlot != null) backWeaponSlot.gameObject.SetActive(true);
        if (playerController != null) playerMovement.CanMove = true;
        canHit = true; //
        if (playerController != null) playerController._movement.CanPush = true;
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