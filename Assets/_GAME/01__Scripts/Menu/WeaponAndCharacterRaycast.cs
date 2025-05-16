using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAndCharacterRaycast : MonoBehaviour
{
    public MainMenuManager mainMenuManager;
    CustomizationPanelManager cmp;
    public GameObject weaponsPanel;
    
    public Camera mainCamera; 
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Perform the raycast
            if (Physics.Raycast(ray, out hit))
            {
                // Check if the ray hit an object with the "PlayerSelect" tag
                if (hit.collider.CompareTag("PlayerSelect"))
                {
                    mainMenuManager.OpenCustomizationPanel();

                }
                else if(hit.collider.CompareTag("WeaponSelect"))
                {
                    mainMenuManager.OpenWeaponsPanel();

                }
            }
        }
    }
}
