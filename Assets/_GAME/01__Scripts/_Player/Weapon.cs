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
        Debug.Log("Weapon Start: " + weaponName + " Energy Consumption: " + energyConsumption);
        energyRecharge = PlayerPrefs.GetFloat(weaponName + "_EnergyRecharge", energyRecharge);
        // The weapon is spawned under its owner's hierarchy. A scene-wide find
        // could bind to the opponent's Player in multiplayer and overwrite their
        // weapon stats with ours.
        Player player = GetComponentInParent<Player>();
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.GetLocalPlayer();
        if (player != null)
        {
            PlayerAttack playerAttack = player.GetComponent<PlayerAttack>();
            player.weapon = this;
            player.fillRate = energyRecharge;
            if (playerAttack != null) playerAttack.SetWeaponSpecial(weaponSpecialRadius);
            player.specialCharges = this.specialCharges;
            player.specialChargesMax = this.specialChargesMax;
        }
    }
}
public enum WeaponSpecialRadius
{
    Small, Medium, Large
}
