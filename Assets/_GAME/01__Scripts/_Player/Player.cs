using System.Collections;
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

    private void Awake()
    {
        SpawnHelmet();

    }
    private void Start()
    {

        if (!GetComponent<PlayerController>().AI)
        {

            StartingStrenght = PlayerPrefs.GetFloat(characterStats.characterName + "_strength", characterStats.strength);
            StartingMoveSpeed = PlayerPrefs.GetFloat(characterStats.characterName + "_speed", characterStats.speed);
        }
        pc = FindObjectOfType<PlayerControls>();
        pc.AssignControls();
        Strength = StartingStrenght;
        // GetComponent<PlayerController>().playerControls = pc;
        MaxEnergy = 100f;
        Energy = MaxEnergy;
        HitDownEnergy = MaxEnergy;
        MoveSpeed = StartingMoveSpeed;
        // fillRate = weapon.energyRecharge;
        if (!GetComponent<PlayerController>().AI)
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
        if (levelGoal != null && !levelGoal.shouldHaveFasterEnergyRecharge)
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
    public void Die(Transform obstacle)
    {
        GetComponent<Animator>().Play("Death_Animation");
        obstacle.transform.GetComponent<Obstacle>().ParticleDestroy(Obstacle.ObstacleDestructionSource.Other);
        GetComponent<PlayerController>().enabled = false;
        StartCoroutine(LoseLevel());
    }
    public void Die()
    {
        GetComponent<Animator>().Play("Death_Animation");
        StartCoroutine(LoseLevel());

    }
    public IEnumerator LoseLevel()
    {
        yield return new WaitForSeconds(0.5f);
        Settings settings = FindObjectOfType<Settings>();
        if (settings != null) Debug.Log("its fine");
        if (!settings.gameWon)
        {
            Debug.Log("Activating panel");
            settings.ActivateLosePanel();
        }
        GetComponent<PlayerController>().enabled = false;
    }
    public IEnumerator LoseLevel(float delay)
    {
        yield return new WaitForSeconds(delay);
        Settings settings = FindObjectOfType<Settings>();
        if (settings != null) Debug.Log("its fine");
        if (!settings.gameWon)
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
            sprintParticleBlueJuice1.gameObject.SetActive(true);
            sprintParticleBlueJuice2.gameObject.SetActive(true);
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

        sprintParticleBlueJuice1.gameObject.SetActive(false);
        sprintParticleBlueJuice2.gameObject.SetActive(false);
        sprintParticleRedJuice1.gameObject.SetActive(false);
        sprintParticleRedJuice2.gameObject.SetActive(false);

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
            strengthParticle1.gameObject.SetActive(true);
            strengthParticle2.gameObject.SetActive(true);
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
        strengthParticle1.gameObject.SetActive(false);
        strengthParticle2.gameObject.SetActive(false);
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
            sprintParticleRedJuice1.gameObject.SetActive(true);
            sprintParticleRedJuice2.gameObject.SetActive(true);
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
            strengthParticle1.gameObject.SetActive(true);
            strengthParticle2.gameObject.SetActive(true);
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
    public void RepairHelmet()
    {
        if (helmet != null)
        {
            if (!helmet.gameObject.activeSelf) helmet.gameObject.SetActive(true);
            helmet.FullRepairHelmet();
        }
    }


    private IEnumerator ResetSpeed(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            remainingSpeedBuffTime = duration - elapsed;
            yield return null;
        }

        // Buff expired
        sprintParticleBlueJuice1.gameObject.SetActive(false);
        sprintParticleBlueJuice2.gameObject.SetActive(false);
        sprintParticleRedJuice1.gameObject.SetActive(false);
        sprintParticleRedJuice2.gameObject.SetActive(false);

        MoveSpeed = StartingMoveSpeed;
        buffedSpeed = MoveSpeed;
        hasSpeedBuff = false;
        remainingSpeedBuffTime = 0f;
        speedBuffCoroutine = null;
    }

    private IEnumerator ResetStrength(float duration)
    {
        yield return new WaitForSeconds(duration);

        strengthParticle1.gameObject.SetActive(false);
        strengthParticle2.gameObject.SetActive(false);
        Strength = StartingStrenght;
        characterStats.strength = (int)StartingStrenght; // Also update characterStats
        buffedStrength = Strength;
        hasStrengthBuff = false;
        strengthBuffCoroutine = null; // Clear the reference
    }
}

