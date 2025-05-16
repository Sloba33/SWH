using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

public class Helmet : MonoBehaviour
{
    public string helmetName;
    public PlayerAttack playerAttack;
    public MeshRenderer mesh;
    public Material material;
    public Color defaultColor;
    public Texture cracked1, cracked2;
    public int helmetDurability, startDurability;
    public Texture defaultTexture;
    private void Start()
    {
        startDurability = helmetDurability;

        // material.SetTexture("_BaseMap", null);
    }
    public void FullRepairHelmet()
    {

        helmetDurability = startDurability;
        if (playerAttack != null) playerAttack.headSmashCollider.gameObject.SetActive(true);
        mesh.material.SetTexture("_BaseMap", null);
    }
    public void DamageHelmet(int amount)
    {
        helmetDurability -= amount;

        // Ensure durability doesn't go below zero
        helmetDurability = Mathf.Max(0, helmetDurability);

        // Calculate the percentage of durability remaining
        float durabilityPercentage = (float)helmetDurability / startDurability;
        Debug.Log("Durability percentage " + durabilityPercentage);
        // Determine which texture to apply based on the percentage
        if (durabilityPercentage > 0.67f)
        {
            // Apply the default texture
            // mesh.material.SetTexture("_BaseMap", null);
            mesh.material.SetTexture("_BaseMap", null);
        }
        else if (durabilityPercentage <= 0.67f && durabilityPercentage > 0.34f)
        {
            // Apply cracked1 texture
            Debug.Log("Setting dmg texture 1");
            mesh.material.SetTexture("_BaseMap", cracked1);
        }
        else
        {
            Debug.Log("Setting dmg texture 2");
            // Apply cracked2 texture
            mesh.material.SetTexture("_BaseMap", cracked2);

            // Check if durability is now 0 and destroy the helmet
            if (helmetDurability == 0)
            {
                mesh.material.SetTexture("_BaseMap", null);
                this.gameObject.SetActive(false);
                // if (playerAttack != null) playerAttack.headSmashCollider.gameObject.SetActive(false);

            }
        }
    }
    public void SetTex(Material material)
    {
        if (defaultTexture != null)
        {
            material.SetTexture("_BaseMap", defaultTexture);
        }
        else
            material.SetTexture("_BaseMap", null);
    }

}
