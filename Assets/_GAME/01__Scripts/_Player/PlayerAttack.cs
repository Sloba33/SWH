using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coherence;
using Coherence.Toolkit;

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
    private CoherenceSync _coherenceSync;

    private IEnumerator Start()
    {
        levelGoal = FindFirstObjectByType<LevelGoal>();
        yield return new WaitForSeconds(0.1f);
        playerMovement = GetComponent<PlayerMovement>();
        playerController = GetComponent<PlayerController>();
        player = GetComponent<Player>();
        _anim = GetComponent<Animator>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _coherenceSync = GetComponent<CoherenceSync>();

        if (headSmashCollider != null)
            headSmashCollider.playerController = playerController;

        InitializeWeaponVisuals();
    }

    private bool _weaponVisualsInitialized;

    /// <summary>
    /// Spawns the hand + back weapon models and applies unlock visibility.
    /// Factored out of Start so replay ghosts — whose Start never runs (they are
    /// neutralized right after Instantiate) — get weapon visuals too. Also caches
    /// the refs the swing-visual coroutines need on a ghost.
    /// </summary>
    public void InitializeWeaponVisuals()
    {
        if (_weaponVisualsInitialized) return;
        _weaponVisualsInitialized = true;

        if (_anim == null) _anim = GetComponent<Animator>();
        if (_coherenceSync == null) _coherenceSync = GetComponent<CoherenceSync>();

        SpawnWeapon();
        if (backWeaponSlot != null && weapon != null)
        {
            backWeapon = Instantiate(weapon.gameObject, backWeaponSlot.transform);
            backWeapon.GetComponent<Weapon>().WeaponStandard.gameObject.SetActive(true);
            backWeapon.GetComponent<Weapon>().WeaponDown.gameObject.SetActive(false);
        }
        CheckWeaponAvailability();
        ShowWeapon(PlayerPrefs.GetInt("AnyWeaponsUnlocked") == 1); //
    }

    // --- Replay ghost swing visuals ------------------------------------------
    // Triggered by StateReplayDriver when the recorded animator bools flip on
    // (Hit / HitDown / HitSpecial). The live visual coroutines are reused where
    // they are side-effect free on a ghost: didHitObstacle=false skips the
    // destruction in HitVisuals, and HitDownVisuals' destroy is gated on
    // ObstacleToHit, which a ghost never sets. Their trailing anim-bool resets
    // coincide with the recorded resets — harmless. The special swing gets its
    // own routine because SpecialVisuals calls PerformSpecialAttack (real AOE
    // destruction); the ghost's destruction comes from recorded events instead.

    public void ReplayPlayHitVisuals() => StartCoroutine(HitVisuals(false, transform.position));

    public void ReplayPlayHitDownVisuals() => StartCoroutine(HitDownVisuals());

    public void ReplayPlaySpecialVisuals() => StartCoroutine(ReplaySpecialVisualsRoutine());

    private IEnumerator ReplaySpecialVisualsRoutine()
    {
        if (weapon != null && weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(true);

        yield return new WaitForSeconds(delayBeforeSwing);
        yield return new WaitForSeconds(delayAfterSwing);
        yield return new WaitForSeconds(0.15f);

        if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = false;

        if (useTrail)
        {
            if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = true;
        }
        else
        {
            if (weaponSpecialSwingParticle != null) weaponSpecialSwingParticle.Play();
        }

        yield return new WaitForSeconds(0.4f);

        // No PerformSpecialAttack — the obstacles it destroyed replay as events.
        if (weaponSpecialAOE != null)
        {
            Vector3 aoePosition = transform.position + transform.forward * 0.5f;
            ParticleSystem ps = Instantiate(weaponSpecialAOE, aoePosition, weaponSpecialAOE.transform.rotation);
            ps.Play();
        }

        yield return new WaitForSeconds(0.1f);
        if (weapon != null && weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(false);
    }

    private void Update()
    {
        if (_inputHandler == null) return;
        if (_coherenceSync != null && !_coherenceSync.HasStateAuthority) return;

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

    public Vector3 hitPoint;
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

    // -------------------------------------------------------------------------
    // Hit
    // -------------------------------------------------------------------------

    public void Hit()
    {
        if (GameManager.PreMatchInputLocked) return; // UI button bypasses the input handler
        if (player == null || weapon == null || _anim == null || playerMovement == null)
        {
            Debug.LogError("PlayerAttack.Hit: Missing required references. Cannot perform attack.");
            return;
        }

        if (!canHit) return;

        if (!playerController.AI)
        {
            if (player.Energy < weapon.energyConsumption) return;
            player.SpendEnergy(weapon.energyConsumption);
        }

        canHit = false;
        ObstacleToHit = FindHitObstacle();

        if (ObstacleToHit != null && levelGoal != null && !levelGoal.Tutorial)
        {
            player.specialCharges++;
            // SPECIAL ATTACK DISABLED
            // if (player.specialCharges >= player.specialChargesMax && player.pc != null && player.pc.specialButton != null)
            //     player.pc.specialButton.gameObject.SetActive(true);
        }

        _anim.SetBool("Hit", true);

        bool didHitObstacle = ObstacleToHit != null && ObstacleToHit.isHammerable;

        if(_coherenceSync != null)
        {
            _coherenceSync.SendCommand<PlayerAttack>(nameof(CmdPlayHitVisuals), MessageTarget.All, didHitObstacle, hitPoint);
        }
        else
        {
            CmdPlayHitVisuals(didHitObstacle, hitPoint);
        }
    }
    
    public void ShowWeapon(bool flag)
    {
        if (flag)
        {
            if (backWeapon != null) backWeapon.SetActive(true);
        }
        else
        {
            if (backWeapon != null) backWeapon.SetActive(false);
        }
    }

    [Command(defaultRouting = MessageTarget.All)]
    public void CmdPlayHitVisuals(bool didHitObstacle, Vector3 impactPoint)
    {
        StartCoroutine(HitVisuals(didHitObstacle, impactPoint));
    }

    private IEnumerator HitVisuals(bool didHitObstacle, Vector3 impactPoint)
    {
        if (backWeaponSlot != null) backWeaponSlot.SetActive(false);
        if (weapon != null && weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(true);

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

        if (didHitObstacle)
        {
            if (weaponHitParticle != null)
                Instantiate(weaponHitParticle, impactPoint, weaponHitParticle.transform.rotation);

            // Destroy obstacle only on authority
            if (_coherenceSync == null || _coherenceSync.HasStateAuthority)
                ObstacleToHit?.ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);
        }

        yield return new WaitForSeconds(0.15f);
        if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = false;
        yield return new WaitForSeconds(0.15f);

        if (weapon != null && weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(false);
        if (backWeaponSlot != null) backWeaponSlot.SetActive(true);

        if (_coherenceSync == null || _coherenceSync.HasStateAuthority)
        {
            _anim.SetBool("Hit", false);
            if (playerController != null) playerMovement.CanMove = true;
            canHit = true;
        }
    }

    // -------------------------------------------------------------------------
    // HitDown
    // -------------------------------------------------------------------------

    public void HitDown()
    {
        if (GameManager.PreMatchInputLocked) return; // UI button bypasses the input handler
        if (player == null || weapon == null || _anim == null || playerMovement == null)
        {
            Debug.LogError("PlayerAttack.HitDown: Missing required references. Cannot perform attack.");
            return;
        }

        if (!canHit || !playerMovement.IsGrounded) return;

        playerMovement.CanMove = false;
        playerController._movement.CanPush = false;
        canHit = false;

        if (!playerController._movement.IsGrounded)
        {
            Debug.LogWarning("PlayerAttack.HitDown: Player not grounded, cannot perform HitDown.");
            playerMovement.CanMove = true;
            playerController._movement.CanPush = true;
            canHit = true;
            hittingDown = false;
            return;
        }

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
            player.SpendHitDownEnergy(weapon.energyConsumption);
        }

        if (playerController._movement.groundHits.Length > 0 && playerController._movement.groundHits[0] != null)
        {
            Vector3 directionToCenter = playerController._movement.groundHits[0].transform.position - transform.position;
            directionToCenter.y = 0;
            if (directionToCenter.magnitude > 0.25f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToCenter, Vector3.up);
                transform.rotation = Quaternion.Euler(0f, lookRotation.eulerAngles.y, 0f);
            }
        }
        else
        {
            Debug.LogWarning("PlayerAttack.HitDown: No ground obstacle detected for direction calculation.");
        }

        ObstacleToHit = (playerController._movement.groundHits.Length > 0 && playerController._movement.groundHits[0] != null)
            ? playerController._movement.groundHits[0].transform.GetComponent<Obstacle>()
            : null;

        _anim.SetBool("HitDown", true);

        if(_coherenceSync != null)
        {
            _coherenceSync.SendCommand<PlayerAttack>(nameof(CmdPlayHitDownVisuals), MessageTarget.All);
        }
        else
        {
            CmdPlayHitDownVisuals();
        }
    }

    [Command(defaultRouting = MessageTarget.All)]
    public void CmdPlayHitDownVisuals()
    {
        StartCoroutine(HitDownVisuals());
    }

    private IEnumerator HitDownVisuals()
    {
        if (backWeaponSlot != null) backWeaponSlot.SetActive(false);
        if (weapon != null && weapon.WeaponDown != null) weapon.WeaponDown.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        // Destroy obstacle only on authority
        if ((_coherenceSync == null || _coherenceSync.HasStateAuthority) && ObstacleToHit != null && ObstacleToHit.isHammerable)
            ObstacleToHit.ParticleDestroy(Obstacle.ObstacleDestructionSource.Weapon);

        yield return new WaitForSeconds(0.3f);

        if (weapon != null && weapon.WeaponDown != null) weapon.WeaponDown.SetActive(false);
        if (backWeaponSlot != null) backWeaponSlot.SetActive(true);

        if (_coherenceSync == null || _coherenceSync.HasStateAuthority)
        {
            _anim.SetBool("HitDown", false);
            if (playerController != null)
            {
                playerMovement.CanMove = true;
                playerController._movement.CanPush = true;
            }
            hittingDown = false;
            canHit = true;
        }
    }

    // -------------------------------------------------------------------------
    // Special Attack
    // -------------------------------------------------------------------------

    public void SpecialAttack()
    {
        if (GameManager.PreMatchInputLocked) return; // UI button bypasses the input handler
        if (player == null || weapon == null || _anim == null || playerMovement == null)
        {
            Debug.LogError("PlayerAttack.SpecialAttack: Missing required references. Cannot perform special attack.");
            return;
        }

        if (!canHit) return;

        if (!playerController.AI)
        {
            if (player.specialCharges < player.specialChargesMax) return;
            player.specialCharges = 0;
            if (player.pc != null && player.pc.specialButton != null)
                player.pc.specialButton.gameObject.SetActive(false);
        }

        canHit = false;
        _anim.SetBool("HitSpecial", true);

        Vector3 aoePosition = playerMovement != null ? playerMovement.WallDetectPosition : transform.position;
        if (_coherenceSync != null)
            _coherenceSync.SendCommand<PlayerAttack>(nameof(CmdPlaySpecialVisuals), MessageTarget.All, aoePosition);
        else
            CmdPlaySpecialVisuals(aoePosition);
    }

    [Command(defaultRouting = MessageTarget.All)]
    public void CmdPlaySpecialVisuals(Vector3 aoePosition)
    {
        StartCoroutine(SpecialVisuals(aoePosition));
    }

    private IEnumerator SpecialVisuals(Vector3 aoePosition)
    {
        if (weapon != null && weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(true);

        yield return new WaitForSeconds(delayBeforeSwing);
        yield return new WaitForSeconds(delayAfterSwing);
        yield return new WaitForSeconds(0.15f);

        if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = false;

        if (useTrail)
        {
            if (weapon != null && weapon.trailRenderer != null) weapon.trailRenderer.enabled = true;
        }
        else
        {
            if (weaponSpecialSwingParticle != null) weaponSpecialSwingParticle.Play();
        }

        yield return new WaitForSeconds(0.4f);

        // AOE damage only on authority
        if (_coherenceSync == null || _coherenceSync.HasStateAuthority)
            PerformSpecialAttack();

        if (weaponSpecialAOE != null)
        {
            ParticleSystem ps = Instantiate(weaponSpecialAOE, aoePosition, weaponSpecialAOE.transform.rotation);
            ps.Play();
        }

        yield return new WaitForSeconds(0.1f);

        if (weapon != null && weapon.WeaponStandard != null) weapon.WeaponStandard.SetActive(false);

        if (_coherenceSync == null || _coherenceSync.HasStateAuthority)
        {
            _anim.SetBool("HitSpecial", false);
            if (playerMovement != null) playerMovement.CanMove = true;
            canHit = true;
        }
    }

    // -------------------------------------------------------------------------
    // Special Attack helpers
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Weapon setup
    // -------------------------------------------------------------------------

    public float delayBeforeSwing, delayAfterSwing;
    public bool useTrail;

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
