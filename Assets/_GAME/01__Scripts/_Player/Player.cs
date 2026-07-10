using System.Collections;
using Coherence;
using Coherence.Toolkit;
using UnityEngine;
using UnityEngine.UI;


[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    public GameObject EndScreenCamera;
    public CharacterStats characterStats;
    public ParticleSystem sprintParticleBlueJuice1, sprintParticleBlueJuice2;
    public ParticleSystem sprintParticleRedJuice1, sprintParticleRedJuice2;
    public ParticleSystem strengthParticle1, strengthParticle2;
    [Header("Stats")]
    public PlayerControls pc;
    public float Energy, HitDownEnergy;
    public float MaxEnergy;
    public float Strength;

    public float StartingStrenght;
    public float StartingMoveSpeed = 2f;
    public float MoveSpeed;
    public bool blackHoleDebuff;

    public Transform helmetParentTransform;
    public Helmet helmet, helmetToSpawn;
    public Image hitFillImage;
    public Image hitDownFillImage;
    public Weapon weapon;
    public int specialCharges, specialChargesMax;
    private LevelGoal levelGoal;
    private CoherenceSync _coherenceSync;

    private void Awake()
    {
        _coherenceSync = GetComponent<CoherenceSync>();
        SpawnHelmet();

    }
    private void Start()
    {
        PlayerController playerController = GetComponent<PlayerController>();
        pc = FindObjectOfType<PlayerControls>();
        
        if(GameManager.Instance.IsMultiplayer)
        {
            if(playerController.HasAuthority)
            {
                pc.AssignControls(playerController);
            }
        }
        else
        {
            if(!playerController.AI)
            {
                StartingStrenght =
                    PlayerPrefs.GetFloat(characterStats.characterName + "_strength", characterStats.strength);

                StartingMoveSpeed = PlayerPrefs.GetFloat(characterStats.characterName + "_speed", characterStats.speed);
            }
            
            pc.AssignControls(playerController);
        }
        
        Strength = StartingStrenght;
        // GetComponent<PlayerController>().playerControls = pc;
        MaxEnergy = 100f;
        Energy = MaxEnergy;
        HitDownEnergy = MaxEnergy;
        MoveSpeed = StartingMoveSpeed;
        // fillRate = weapon.energyRecharge;
        if (GameManager.Instance.IsMultiplayer && playerController.HasAuthority || 
            !GameManager.Instance.IsMultiplayer && !playerController.AI)
        {

            hitFillImage.fillAmount = Energy;
            hitDownFillImage.fillAmount = HitDownEnergy;
            StartCoroutine(FillEnergyOverTime());
        }
        if (helmet != null) helmet.playerAttack = GetComponent<PlayerAttack>();


        levelGoal = FindFirstObjectByType<LevelGoal>();




        // StartingStrenght = 20f;
        // StartingMoveSpeed = 4f;

    }
    public float newMoveSpeed;
    public float PushAndPullSpeed(float obstacleWeight)
    {

        newMoveSpeed = Strength / obstacleWeight;
        if (newMoveSpeed > StartingMoveSpeed)
        {
            newMoveSpeed = StartingMoveSpeed;
        }
        return newMoveSpeed;
    }
    public void SpawnHelmet()
    {
        if (helmetToSpawn != null)
        {
            Helmet helm = Instantiate(helmetToSpawn, helmetParentTransform);

            helmet = helm;
        }
        else
            Debug.LogError("Helmet to spawn is null");
    }
    public void SpendEnergy(float amount)
    {
        // Spend energy by default; only the faster-recharge level flag suppresses the cost.
        // levelGoal can legitimately be null (multiplayer spawn order), which must not make hits free.
        if (levelGoal == null || !levelGoal.shouldHaveFasterEnergyRecharge)
        {

            Energy -= amount;
            float fillAmount = Energy / 100f;
            hitFillImage.fillAmount = fillAmount;

            Debug.Log("Energy: " + Energy);
        }
    }
    public void SpendHitDownEnergy(float amount)
    {
        HitDownEnergy -= amount;
        float fillAmount = HitDownEnergy / 100f;
        hitDownFillImage.fillAmount = fillAmount;

        Debug.Log("Energy: " + Energy);
    }
    public void BuffEnergy(float amount)
    {
        Energy += amount;
        HitDownEnergy += amount;
        if (Energy > 100) Energy = 100f;
        if (HitDownEnergy > 100) HitDownEnergy = 100f;
        float fillAmount = Energy / 100f;
        float fillHitDownAmount = HitDownEnergy / 100f;
        Debug.Log("Energy increased by : " + amount + " to the amount of : " + Energy + " And setting it to " + fillAmount);
        hitFillImage.fillAmount = fillAmount;
        hitDownFillImage.fillAmount = fillHitDownAmount;
    }
    IEnumerator FillEnergyOverTime()
    {
        while (true)
        {

            float fillAmount = fillRate * Time.deltaTime;
            Energy = Mathf.Clamp(Energy + fillAmount, 0f, 100f);
            HitDownEnergy = Mathf.Clamp(HitDownEnergy + fillAmount, 0f, 100f);
            UpdateEnergyFill();

            yield return null;
        }
    }

    private void Update()
    {
        if (dur != 0)
        {
            Debug.Log("Duration: " + dur);
        }
    }
    public float fillRate;
    public float fillDuration = 3f;
    void UpdateEnergyFill()
    {


        float fillAmount = Energy / 100f;
        float fillHitDownAmount = HitDownEnergy / 100f;
        hitFillImage.fillAmount = fillAmount;
        hitDownFillImage.fillAmount = fillHitDownAmount;
    }
    /// <summary>
    /// Raised when any player dies, before the lose flow runs. The state-replay
    /// recorder subscribes to capture the death as a replay event.
    /// </summary>
    public static event System.Action<Player> PlayerDied;

    public void Die(Transform obstacle)
    {
        PlayerDied?.Invoke(this);
        GetComponent<Animator>().Play("Death_Animation");
        obstacle.transform.GetComponent<Obstacle>().ParticleDestroy(Obstacle.ObstacleDestructionSource.Other);
        GetComponent<PlayerController>().enabled = false;
        if (GameManager.Instance != null && GameManager.Instance.IsBotMatch)
        {
            // The shared Settings means a player's own LoseLevel can't decide the
            // match — the arbiter maps "who died" to the local human's outcome.
            BotMatchArbiter.Instance?.NotifyDeath(this);
            return;
        }
        NotifyOpponentOfWinIfMultiplayer();
        StartCoroutine(LoseLevel());
    }
    public void Die()
    {
        PlayerDied?.Invoke(this);
        GetComponent<Animator>().Play("Death_Animation");
        if (GameManager.Instance != null && GameManager.Instance.IsBotMatch)
        {
            GetComponent<PlayerController>().enabled = false;
            BotMatchArbiter.Instance?.NotifyDeath(this);
            return;
        }
        NotifyOpponentOfWinIfMultiplayer();
        StartCoroutine(LoseLevel());

    }

    [Command(defaultRouting = MessageTarget.Other)]
    public void CmdPlayDeathAnimation()
    {
        GetComponent<Animator>().Play("Death_Animation");
    }

    private void NotifyOpponentOfWinIfMultiplayer()
    {
        if (!GameManager.Instance.IsMultiplayer) return;
        var controller = GetComponent<PlayerController>();
        if (controller != null && !controller.HasAuthority) return;
        if (_coherenceSync != null)
            _coherenceSync.SendCommand<Player>(nameof(CmdPlayDeathAnimation), MessageTarget.Other);
        if (GameManager.Instance.levelGoal != null)
            GameManager.Instance.levelGoal.NotifyOpponentOfDeath();
    }
    public IEnumerator LoseLevel()
    {
        Settings settings = FindObjectOfType<Settings>();
        if (settings != null) settings.gameLost = true;
        yield return new WaitForSeconds(0.5f);
        if (settings != null && !settings.gameWon)
        {
            Debug.Log("Activating panel");
            settings.ActivateLosePanel();
        }
        GetComponent<PlayerController>().enabled = false;
    }

    /// <summary>
    /// Direct-lose path (falling off the platform — see CameraController.HandlePlayerFall),
    /// which bypasses Die(). It must still: raise PlayerDied so the state-replay
    /// recorder captures fall-deaths (deduped if Die already fired), and in a bot
    /// match route through the arbiter so the loss carries the MP trophy delta via
    /// LevelGoal.LoseLevel instead of popping a bare lose panel here.
    /// </summary>
    public IEnumerator LoseLevel(float delay)
    {
        PlayerDied?.Invoke(this);

        if (GameManager.Instance != null && GameManager.Instance.IsBotMatch)
        {
            BotMatchArbiter.Instance?.NotifyDeath(this);
            GetComponent<PlayerController>().enabled = false;
            yield break;
        }

        Settings settings = FindObjectOfType<Settings>();
        if (settings != null) settings.gameLost = true;
        yield return new WaitForSeconds(delay);
        if (settings != null && !settings.gameWon)
        {
            Debug.Log("Activating panel");
            settings.ActivateLosePanel();
        }
        GetComponent<PlayerController>().enabled = false;
    }
    public bool hasSpeedBuff, hasStrengthBuff;
    public float buffedSpeed, buffedStrength;
    private Coroutine speedBuffCoroutine;
    private Coroutine strengthBuffCoroutine;
    private float remainingSpeedBuffTime = 0f;
    private float remainingStrengthBuffTime = 0f;
    public void BuffSpeed(float duration, float amount)
    {
        if (!hasSpeedBuff)
        {
            MoveSpeed += amount;
            hasSpeedBuff = true;
            SetSprintParticlesNetworked(SprintParticleKind.Blue, true);
            remainingSpeedBuffTime = duration;
        }
        else
        {
            remainingSpeedBuffTime += duration;
        }

        if (speedBuffCoroutine != null)
            StopCoroutine(speedBuffCoroutine);

        speedBuffCoroutine = StartCoroutine(CountdownSpeedBuff());
        buffedSpeed = MoveSpeed;
    }

    private IEnumerator CountdownSpeedBuff()
    {
        while (remainingSpeedBuffTime > 0)
        {
            remainingSpeedBuffTime -= Time.deltaTime;
            yield return null;
        }

        SetSprintParticlesNetworked(SprintParticleKind.Blue, false);
        SetSprintParticlesNetworked(SprintParticleKind.Red, false);

        MoveSpeed = StartingMoveSpeed;
        buffedSpeed = MoveSpeed;
        hasSpeedBuff = false;
        remainingSpeedBuffTime = 0f;
        speedBuffCoroutine = null;
    }
    private float dur;
    public void BuffStrength(float duration, float amount)
    {
        if (!hasStrengthBuff)
        {
            characterStats.strength += (int)amount;
            Strength += amount;
            hasStrengthBuff = true;
            remainingStrengthBuffTime = duration;
            SetSprintParticlesNetworked(SprintParticleKind.Strength, true);
        }
        else
        {
            remainingStrengthBuffTime += duration;
        }

        if (strengthBuffCoroutine != null)
            StopCoroutine(strengthBuffCoroutine);

        strengthBuffCoroutine = StartCoroutine(CountdownStrengthBuff());
        buffedStrength = Strength;
    }
    private IEnumerator CountdownStrengthBuff()
    {
        while (remainingStrengthBuffTime > 0)
        {
            remainingStrengthBuffTime -= Time.deltaTime;
            Debug.Log($"Strength buff remaining: {remainingStrengthBuffTime:F2} seconds");
            yield return null;
        }
        SetSprintParticlesNetworked(SprintParticleKind.Strength, false);
        Strength = StartingStrenght;
        characterStats.strength = (int)StartingStrenght;
        buffedStrength = Strength;
        hasStrengthBuff = false;
        remainingStrengthBuffTime = 0f;
        strengthBuffCoroutine = null;
        Debug.Log("Strength buff EXPIRED!");
    }

    public void BuffStrengthAndSpeed(float duration, float speedAmount, float strengthAmount)
    {
        // Handle speed buff
        if (!hasSpeedBuff)
        {
            MoveSpeed += speedAmount;
            hasSpeedBuff = true;
            SetSprintParticlesNetworked(SprintParticleKind.Red, true);
            remainingSpeedBuffTime = duration;
        }
        else
        {
            remainingSpeedBuffTime += duration;
        }

        // Handle strength buff
        if (!hasStrengthBuff)
        {
            Strength += strengthAmount;
            hasStrengthBuff = true;
            remainingStrengthBuffTime = duration;
            SetSprintParticlesNetworked(SprintParticleKind.Strength, true);
        }
        else
        {
            remainingStrengthBuffTime += duration;
        }

        // Stop existing coroutines
        if (speedBuffCoroutine != null)
            StopCoroutine(speedBuffCoroutine);
        if (strengthBuffCoroutine != null)
            StopCoroutine(strengthBuffCoroutine);

        // Start fresh coroutines
        speedBuffCoroutine = StartCoroutine(CountdownSpeedBuff());
        strengthBuffCoroutine = StartCoroutine(CountdownStrengthBuff());

        buffedStrength = Strength;
        buffedSpeed = MoveSpeed;
    }

    public enum SprintParticleKind { Blue = 0, Red = 1, Strength = 2 }

    public void SetSprintParticlesNetworked(SprintParticleKind kind, bool active)
    {
        ApplySprintParticles((int)kind, active);
        if (_coherenceSync != null)
            _coherenceSync.SendCommand<Player>(nameof(CmdSetSprintParticles), MessageTarget.Other, (int)kind, active);
    }

    [Command(defaultRouting = MessageTarget.Other)]
    public void CmdSetSprintParticles(int kind, bool active)
    {
        ApplySprintParticles(kind, active);
    }

    private void ApplySprintParticles(int kind, bool active)
    {
        if (kind == (int)SprintParticleKind.Blue)
        {
            if (sprintParticleBlueJuice1 != null) sprintParticleBlueJuice1.gameObject.SetActive(active);
            if (sprintParticleBlueJuice2 != null) sprintParticleBlueJuice2.gameObject.SetActive(active);
        }
        else if (kind == (int)SprintParticleKind.Red)
        {
            if (sprintParticleRedJuice1 != null) sprintParticleRedJuice1.gameObject.SetActive(active);
            if (sprintParticleRedJuice2 != null) sprintParticleRedJuice2.gameObject.SetActive(active);
        }
        else if (kind == (int)SprintParticleKind.Strength)
        {
            if (strengthParticle1 != null) strengthParticle1.gameObject.SetActive(active);
            if (strengthParticle2 != null) strengthParticle2.gameObject.SetActive(active);
        }
    }
    public void RepairHelmet()
    {
        ApplyHelmetRepair();
        if (_coherenceSync != null)
            _coherenceSync.SendCommand<Player>(nameof(CmdRepairHelmet), MessageTarget.Other);
    }

    public void DamageHelmetNetworked(int amount)
    {
        ApplyHelmetDamage(amount);
        if (_coherenceSync != null)
            _coherenceSync.SendCommand<Player>(nameof(CmdDamageHelmet), MessageTarget.Other, amount);
    }

    [Command(defaultRouting = MessageTarget.Other)]
    public void CmdDamageHelmet(int amount)
    {
        ApplyHelmetDamage(amount);
    }

    [Command(defaultRouting = MessageTarget.Other)]
    public void CmdRepairHelmet()
    {
        ApplyHelmetRepair();
    }

    private void ApplyHelmetDamage(int amount)
    {
        if (helmet != null) helmet.DamageHelmet(amount);
    }

    private void ApplyHelmetRepair()
    {
        if (helmet == null) return;
        if (!helmet.gameObject.activeSelf) helmet.gameObject.SetActive(true);
        helmet.FullRepairHelmet();
    }


}

