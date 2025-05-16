using UnityEngine;

public class Weapon : MonoBehaviour
{
    public string weaponName;
    public GameObject WeaponStandard, WeaponDown;
    public float energyConsumption, energyRecharge;
    public ParticleSystem weaponHitParticle;
    public ParticleSystem weaponSwingParticle;
    public TrailRenderer trailRenderer;
    public WeaponSpecialRadius weaponSpecialRadius;
    public int specialCharges, specialChargesMax;
    public WeaponType weaponType;

    private void Start()
    {
        energyConsumption = PlayerPrefs.GetFloat(weaponName + "_EnergyConsumption", energyConsumption);
        energyRecharge = PlayerPrefs.GetFloat(weaponName + "_EnergyRecharge", energyRecharge);
        Player player = FindObjectOfType<Player>();
        PlayerAttack playerAttack = player.GetComponent<PlayerAttack>();
        if (player != null)
        {
            player.weapon = this;
            player.fillRate = energyRecharge;
            playerAttack.SetWeaponSpecial(weaponSpecialRadius);
            player.specialCharges = this.specialCharges;
            player.specialChargesMax = this.specialChargesMax;
        }
    }
}
public enum WeaponSpecialRadius
{
    Small, Medium, Large
}
