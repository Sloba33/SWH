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

    private void Awake()
    {
        SpawnHelmet();
        // //comment part below once recording is done  // HELMET  RECORDING
        // if (!GetComponent<PlayerController>().AI)
        // {
        //     // helmet.startDurability = 3;
        //     helmet.DamageHelmet(1);
        //     helmet.DamageHelmet(1);
        //     helmet.DamageHelmet(1);
        //     // helmet.helmetDurability = 0;
        //     // helmet.gameObject.SetActive(false);
        // }
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

    }
    public float newMoveSpeed;
    public float PushAndPullSpeed(float obstacleWeight)
    {

        newMoveSpeed = Strength / obstacleWeight;

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
        Energy -= amount;
        float fillAmount = Energy / 100f;
        hitFillImage.fillAmount = fillAmount;

        Debug.Log("Energy: " + Energy);
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
        obstacle.transform.GetComponent<Obstacle>().ParticleDestroy();
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
    public bool hasSpeedBuff, hasStrengthBuff;
    public float buffedSpeed, buffedStrength;
    public void BuffSpeed(float duration, float amount)
    {
        Debug.Log("BUffing speed");
        hasSpeedBuff = true;
        MoveSpeed += amount;
        buffedSpeed = MoveSpeed;
        sprintParticleBlueJuice1.gameObject.SetActive(true);
        sprintParticleBlueJuice2.gameObject.SetActive(true);
        StartCoroutine(ResetSpeed(duration));
    }
    public void BuffStrength(float duration, float amount)
    {
        hasStrengthBuff = true;
        characterStats.strength += (int)amount;
        buffedStrength = Strength;
        StartCoroutine(ResetStrength(duration));
    }
    public void BuffStrengthAndSpeed(float duration, float speedAmount, float strengthAmount)
    {
        Debug.Log("Buffing Strength and speed");
        hasStrengthBuff = true;
        hasSpeedBuff = true;

        // characterStats.strength += strengthAmount; // Increment strength
        Strength += strengthAmount; // Increment strength
        buffedStrength = Strength;

        MoveSpeed += speedAmount;
        buffedSpeed = MoveSpeed;

        sprintParticleRedJuice1.gameObject.SetActive(true);
        sprintParticleRedJuice2.gameObject.SetActive(true);

        StartCoroutine(ResetSpeed(duration));
        StartCoroutine(ResetStrength(duration));
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

        yield return new WaitForSeconds(duration);

        sprintParticleBlueJuice1.gameObject.SetActive(false);
        sprintParticleBlueJuice2.gameObject.SetActive(false);
        sprintParticleRedJuice1.gameObject.SetActive(false);
        sprintParticleRedJuice2.gameObject.SetActive(false);

        MoveSpeed = StartingMoveSpeed;
        buffedSpeed = MoveSpeed;
        hasSpeedBuff = false;
    }
    private IEnumerator ResetStrength(float duration)
    {

        yield return new WaitForSeconds(duration);

        Strength = StartingStrenght;
        buffedSpeed = Strength;
        hasStrengthBuff = false;
    }
}

