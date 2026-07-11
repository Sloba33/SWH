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
    public bool isPaintable;
    
    // Cache the property ID for better performance
    private int baseMapPropertyID;
    
    // Awake (not Start) on purpose: replay ghosts neutralize all MonoBehaviours
    // right after Instantiate, so Start never runs on their helmet — but Awake
    // fires during Instantiate itself. Without this, startDurability stays 0 and
    // DamageHelmet's percentage math can never pick a crack texture.
    private void Awake()
    {
        startDurability = helmetDurability;
        baseMapPropertyID = Shader.PropertyToID("_BaseMap");
        // material.SetTexture("_BaseMap", null);
    }
    
    // Helper method to check if material has _BaseMap property
    private bool HasBaseMapProperty(Material mat)
    {
        return mat != null && mat.HasProperty(baseMapPropertyID);
    }
    
    public bool IsHelmetRepairable()
    {
        if (helmetDurability < startDurability)
        {
            Debug.Log("Repairable");
            return true;
        }
        else
        {
            Debug.Log("Not repairable");

            return false;
        }
    }
    public void FullRepairHelmet()
    {
        if (helmetDurability == startDurability) return;
        helmetDurability = startDurability;
        if (playerAttack != null) playerAttack.headSmashCollider.gameObject.SetActive(true);

        Material[] materials = mesh.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            // Skip materials without _BaseMap property (like outline materials)
            if (!HasBaseMapProperty(materials[i])) continue;
            
            Texture currentTexture = materials[i].GetTexture("_BaseMap");

            // Only clear damage textures, preserve custom textures
            if (currentTexture == cracked1 || currentTexture == cracked2)
            {
                // This was a damage texture, remove it
                materials[i].SetTexture("_BaseMap", null);
                Debug.Log($"Material {i} damage texture cleared");
            }
            else if (currentTexture != null)
            {
                // This is a custom texture, keep it
                Debug.Log($"Material {i} custom texture preserved: {currentTexture.name}");
            }
            else
            {
                // No texture, leave as is
                materials[i].SetTexture("_BaseMap", null);
            }
        }
    }
    public void DamageHelmet(int amount)
    {
        helmetDurability -= amount;

        // Ensure durability doesn't go below zero
        helmetDurability = Mathf.Max(0, helmetDurability);

        // Calculate the percentage of durability remaining
        float durabilityPercentage = (float)helmetDurability / startDurability;
        Debug.Log("Durability percentage " + durabilityPercentage);

        // Determine target damage texture based on durability
        Texture targetDamageTexture = null;

        if (durabilityPercentage > 0.67f)
        {
            targetDamageTexture = null; // No damage texture
        }
        else if (durabilityPercentage <= 0.67f && durabilityPercentage > 0.34f)
        {
            Debug.Log("Setting dmg texture 1");
            targetDamageTexture = cracked1;
        }
        else
        {
            Debug.Log("Setting dmg texture 2");
            targetDamageTexture = cracked2;

            // Check if durability is now 0 and destroy the helmet
            if (helmetDurability == 0)
            {
                this.gameObject.SetActive(false);
            }
        }

        // Iterate through all materials on the helmet
        Material[] materials = mesh.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            // Skip materials without _BaseMap property (like outline materials)
            if (!HasBaseMapProperty(materials[i])) continue;
            
            Texture currentTexture = materials[i].GetTexture("_BaseMap");

            // Check if the material already has a custom texture
            bool hasCustomTexture = currentTexture != null &&
                                    currentTexture != cracked1 &&
                                    currentTexture != cracked2;

            if (hasCustomTexture)
            {
                // Material has a custom texture (like paint, decal, etc.)
                // Keep it and don't apply damage textures
                Debug.Log($"Material {i} has custom texture, preserving it");
                continue; // Skip applying damage texture
            }
            else if (currentTexture == cracked1 || currentTexture == cracked2)
            {
                // Material currently has a damage texture
                // Update it to the new appropriate damage texture
                materials[i].SetTexture("_BaseMap", targetDamageTexture);
                Debug.Log($"Material {i} updated damage texture");
            }
            else
            {
                // Material has no texture or has default texture
                // Apply the appropriate damage texture
                materials[i].SetTexture("_BaseMap", targetDamageTexture);
                Debug.Log($"Material {i} applied new damage texture");
            }
        }
    }

    public void SetTex(Material material)
    {
        // Skip if material doesn't have _BaseMap property
        if (!HasBaseMapProperty(material)) return;
        
        if (defaultTexture != null)
        {
            material.SetTexture("_BaseMap", defaultTexture);
        }
        else
            material.SetTexture("_BaseMap", null);
    }

}