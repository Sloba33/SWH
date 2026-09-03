using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PlayerControls : MonoBehaviour
{
    public List<ConsumableSlot> consumableSlots = new(); //

    public Image pullFrame, pullHand; //
    public Color interactableColor, uninteractableColor; //
    public TextMeshProUGUI bombCountText; //
    public int bombCounter; //
    public PlayerController playerController; //
    public GameObject playerCamera; //
    public Camera playerRegularCamera; //
    public GameObject bombPrefab; //
    public Button pullButton, jumpButton, hitButton, hitDownButton, bombButton; //
    public Image hitFill, hitDownFill; //
    public RectTransform joystickHolder; //
    public PlayerAttack playerAttack; //

    public LevelGoal levelGoal; //
    public Image handJoystick, handJump; //
    public GameObject hintPull; //
    public bool isPulling; //
    public TutorialHandler tutorialHandler; //
    public Button specialButton; //
    public Settings settings;
    public Image specialChargeImage;

    // Removed the Update method for keyboard input for attacks.
    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.V))
    //     {
    //         PlaceBombViaBind();
    //     }
    //     if (Input.GetKeyDown(KeyCode.Z))
    //     {
    //         playerController.StartPull();
    //     }
    //     else if (Input.GetKeyUp(KeyCode.Z))
    //     {
    //         playerController.StopPull();
    //     }
    // }
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.F))
        {
            playerController.StopPull();
        }
    }
    public void FillChargeImage()
    {
        if (specialChargeImage != null) specialChargeImage.fillAmount += 0.33333f;
    }
    public void EmptyChargeImage()
    {
        if (specialChargeImage != null) specialChargeImage.fillAmount = 0f;
    }
    public void AssignControls() //
    {
        playerController = FindObjectOfType<PlayerController>(); //
        if (playerController != null) // Safety check
        {
            playerAttack = playerController.GetComponent<PlayerAttack>(); //
            Player playerComponent = playerController.GetComponent<Player>(); //
            if (playerComponent != null) // Safety check
            {
                playerComponent.hitFillImage = hitFill; //
                playerComponent.hitDownFillImage = hitDownFill; //
            }
        }
    }

    private void OnMobileJumpButtonClicked() //
    {
        GameEvents.OnJumpButtonPressed.Invoke(); //
        Debug.Log("GameEvents.OnJumpButtonPressed invoked from UI button."); //
    }

    private IEnumerator Start() //
    {
        _isInteractable = true; //
        levelGoal = FindObjectOfType<LevelGoal>(); //
        yield return new WaitForSeconds(0.5f); //
        settings = FindAnyObjectByType<Settings>(FindObjectsInactive.Include); //
        specialChargeImage = GameObject.Find("SpecialChargeFrame")?.GetComponent<Image>(); //
        // if (specialChargeImage != null) specialChargeImage.GetComponent<Image>().enabled = false;
        // Only add listener if jumpButton is assigned
        // if (jumpButton != null) //
        // {
        //     jumpButton.onClick.AddListener(OnMobileJumpButtonClicked); //
        // }
        // else
        // {
        //     Debug.LogWarning("Jump Button not assigned in PlayerControls. Ensure it's hooked up in the Inspector."); //
        // }
        SetReferences();
        // Invoke(nameof(SetReferences), 0f); //

        // --- All tutorial-related UI active/inactive logic is fine as is ---
        if (levelGoal != null && levelGoal.Tutorial) //
        {
            if (levelGoal.levelType == LevelGoal.LevelType.Move && tutorialHandler != null) //
            {
                tutorialHandler.shouldGuide = true; //
                if (handJoystick != null) handJoystick.gameObject.SetActive(true); //
            }
            if (levelGoal.levelType == LevelGoal.LevelType.Bomb) //
            {
                if (hintPull != null) hintPull.gameObject.SetActive(false); //
                if (handJump != null) handJump.gameObject.SetActive(false); //
                if (tutorialHandler != null) tutorialHandler.shouldGuide = false; //
            }

            if (levelGoal.bombCount == 0 && !levelGoal.bombs) //
            {
                if (bombButton != null) bombButton.gameObject.SetActive(false); //
            }
            else if (levelGoal.bombs && levelGoal.bombCount == 0) //
            {
                if (bombButton != null) bombButton.gameObject.SetActive(false); //
            }
            if (!levelGoal.weapons) //
            {
                if (hitButton != null) hitButton.gameObject.SetActive(false); //
                if (hitDownButton != null) hitDownButton.gameObject.SetActive(false); //
            }
            if (!levelGoal.jump) //
            {
                if (jumpButton != null) jumpButton.gameObject.SetActive(false); //
            }
            if (!levelGoal.pull) //
            {
                if (pullButton != null) pullButton.gameObject.SetActive(false); //
            }
        }
    }

    public bool _isInteractable; //
    public void Interactable(bool interactable) //
    {
        if (pullFrame == null || pullHand == null || pullButton == null) return; // Safety check

        // It's crucial to set the Button's interactable property here
        // as well as your custom _isInteractable and color changes.
        pullButton.interactable = interactable;

        if (interactable && !_isInteractable) //
        {
            _isInteractable = true; //
            pullFrame.color = interactableColor; //
            pullHand.color = interactableColor; //
        }
        else if (!interactable && _isInteractable) //
        {
            _isInteractable = false; //
            pullFrame.color = uninteractableColor; //
            pullHand.color = uninteractableColor; //
        }
    }

    public void SetReferences() //
    {
        if (playerController == null) return; //
        if (playerAttack != null) playerAttack.playerControls = this;
        playerRegularCamera = playerController.playerCamera; //
        // playerCamera = playerController.followCamera.gameObject; //

        // UI Button Listeners for Attack actions
        if (hitButton != null) // Safety check
        {
            hitButton.onClick.AddListener(() =>
            {
                if (playerAttack != null) playerAttack.Hit(); // Call Hit method on PlayerAttack
            });
        }

        if (specialButton != null && levelGoal != null && !levelGoal.Tutorial) // Safety check and tutorial condition
        {
            specialButton.onClick.AddListener(() =>
            {
                if (playerAttack != null) playerAttack.SpecialAttack(); // Call SpecialAttack on PlayerAttack
            });
        }

        if (hitDownButton != null) // Safety check
        {
            hitDownButton.onClick.AddListener(() =>
            {
                if (playerAttack != null) playerAttack.HitDown(); // Call HitDown method on PlayerAttack
            });
        }

        // Pull button EventTriggers (already setup correctly)
        // Add a check to ensure EventTrigger exists before adding entries
        EventTrigger pullEventTrigger = pullButton?.GetComponent<EventTrigger>();
        if (pullButton != null && pullEventTrigger == null)
        {
            // If it doesn't exist, add it
            pullEventTrigger = pullButton.gameObject.AddComponent<EventTrigger>();
        }

        if (pullEventTrigger != null)
        {
            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry(); //
            pointerDownEntry.eventID = EventTriggerType.PointerDown; //
            pointerDownEntry.callback.AddListener((data) => { OnPointerDownDelegatePull((PointerEventData)data); }); //
            pullEventTrigger.triggers.Add(pointerDownEntry); //

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry(); //
            pointerUpEntry.eventID = EventTriggerType.PointerUp; //
            pointerUpEntry.callback.AddListener((data) => { OnPointerUpDelegatePull((PointerEventData)data); }); //
            pullEventTrigger.triggers.Add(pointerUpEntry); //
        }
        else
        {
            Debug.LogWarning("Pull Button or its EventTrigger is null. Ensure the Pull Button GameObject has an EventTrigger component.");
        }

        EventTrigger jumpEventTrigger = jumpButton?.GetComponent<EventTrigger>();
        if (jumpButton != null)
        {
            if (jumpEventTrigger == null)
            {
                // If it doesn't exist, add it
                jumpEventTrigger = jumpButton.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry jumpPointerDownEntry = new EventTrigger.Entry();
            jumpPointerDownEntry.eventID = EventTriggerType.PointerDown;
            jumpPointerDownEntry.callback.AddListener((data) => { OnPointerDownDelegateJump((PointerEventData)data); });
            jumpEventTrigger.triggers.Add(jumpPointerDownEntry);
            Debug.Log("Jump button added");
        }
        else
        {
            Debug.LogWarning("Jump Button not assigned or its EventTrigger is null. Ensure the Jump Button GameObject has an EventTrigger component or is assigned in Inspector.");
        }
    }
    public void OnPointerDownDelegateJump(PointerEventData eventData)
    {
        // You might want to add an IsInteractable check here too, similar to pullButton
        if (jumpButton != null && !jumpButton.IsInteractable())
        {
            Debug.Log("Jump button clicked but it is not interactable. Ignoring action.");
            return;
        }

        // This is where you call your existing method that invokes the game event
        OnMobileJumpButtonClicked(); // Call your existing method that invokes GameEvents.OnJumpButtonPressed
        if (handJump != null) handJump.gameObject.SetActive(false); // Your existing jump hint logic
        else if (levelGoal != null && levelGoal.Tutorial && levelGoal.jumpHint != null) levelGoal.jumpHint.gameObject.SetActive(false);
        Debug.Log("Jumping (from PointerDownDelegateJump)");
    }
    public void OnPointerDownDelegatePull(PointerEventData eventData) //
    {
        // THIS IS THE CRUCIAL CHANGE: Check if the button is interactable
        if (pullButton != null && !pullButton.IsInteractable())
        {
            Debug.Log("Pull button clicked but it is not interactable. Ignoring action.");
            return; // Do nothing if the button is not interactable
        }

        if (playerController != null) playerController.StartPull(); //
        isPulling = true; //
        if (hintPull != null) hintPull.gameObject.SetActive(false); //
    }

    public void OnPointerUpDelegatePull(PointerEventData eventData) //
    {
        // Add the same check here for consistency, though PointerUp might not need to be blocked if PointerDown is.
        if (pullButton != null && !pullButton.IsInteractable())
        {
            Debug.Log("Pull button released but it was not interactable. Ignoring action.");
            return; // Do nothing if the button is not interactable
        }

        if (playerController != null) playerController.StopPull(); //
        isPulling = false; //
        if (!levelGoal.Tutorial) return; //
        if (hintPull != null) hintPull.gameObject.SetActive(true); //
    }

    // This method is now redundant for general jump, as Jump is handled by GameEvents
    // public void OnPointerDownDelegateJump(PointerEventData eventData)
    // {
    //     playerController.HandleJump();
    //     if (handJump != null) handJump.gameObject.SetActive(false);
    //     else if (levelGoal != null && levelGoal.Tutorial && levelGoal.jumpHint != null) levelGoal.jumpHint.gameObject.SetActive(false);
    //     Debug.Log("Jumping");
    // }

    private void PlaceBomb() //
    {
        if (bombCounter > 0) //
        {
            Debug.Log("Placing bomb"); //
            if (playerController == null) { Debug.LogError("PlayerController is null for bomb placement."); return; } // Safety check
            Vector3 position = playerController._movement.FindNeighbouringTilePosition(); //
            position.y = Mathf.Round(playerController.transform.position.y); //
            GameObject bomb = Instantiate(bombPrefab, position, bombPrefab.transform.rotation); //
            Bomb bombComponent = bomb.GetComponent<Bomb>(); //
            if (bombComponent != null) // Safety check
            {
                bombComponent.playerCamera = playerCamera; //
            }
            bombCounter--; //
            if (bombCountText != null) bombCountText.text = bombCounter.ToString(); //
            RemoveBomb(consumableSlots[0]); //
        }
        else
        {
            Debug.Log("No bombs left to place."); //
        }
    }

    private void PlaceBombViaBind() //
    {
        Debug.Log("Placing bomb on tile"); //
        if (playerController == null) { Debug.LogError("PlayerController is null for bomb placement via bind."); return; } // Safety check
        Vector3 position = playerController._movement.FindNeighbouringTilePosition(); //
        position.y = Mathf.Round(playerController.transform.position.y); //
        GameObject bomb = Instantiate(bombPrefab, position, bombPrefab.transform.rotation); //
        Bomb bombComponent = bomb.GetComponent<Bomb>(); //
        if (bombComponent != null) // Safety check
        {
            bombComponent.playerCamera = playerCamera; //
        }
        bombCounter--; //
        // bombCountText.text = bombCounter.ToString(); // // Commented out in original
        RemoveBomb(consumableSlots[0]); //
    }

    public void OnPointerDownDelegateBomb(PointerEventData eventData, GameObject bprefab, ConsumableSlot consumableSlot) //
    {
        if (consumableSlot == null) { Debug.LogError("ConsumableSlot is null for bomb delegate."); return; } // Safety check
        if (playerController == null) { Debug.LogError("PlayerController is null for bomb delegate."); return; } // Safety check

        if (consumableSlot.counter < 1) //
        {
            return; //
        }
        if (playerController._movement.IsBombBlocked || playerController.isPushing) //
        {
            Debug.Log("Bomb not placeable " + playerController._movement.IsBombBlocked); //

            GameObject bomb = Instantiate(bprefab, playerController.transform.position + new Vector3(0, 0.02f, 0), bprefab.transform.rotation); //
            Bomb bombComponent = bomb.GetComponent<Bomb>(); //
            if (bombComponent != null && bombComponent.isColored) //
            {
                if (bombComponent.boxCollider != null) bombComponent.boxCollider.enabled = false; //
            }
            if (bombComponent != null) //
            {
                bombComponent.playerCamera = playerCamera; //
                if (playerController.GetComponent<Collider>() != null) bombComponent.IgnorePlayerCollision(playerController.GetComponent<Collider>()); //
            }
            AudioSource.PlayClipAtPoint(bombComponent.spawnSound, transform.position); //
            RemoveBomb(consumableSlot); //
        }
        else
        {
            Debug.Log("Bomb IS placeable"); //
            Vector3 position = playerController._movement.FindNeighbouringTilePosition(); //
            position.y = Mathf.Round(playerController.transform.position.y); //
            GameObject bomb = Instantiate(bprefab, position, bprefab.transform.rotation); //
            Bomb bombComponent = bomb.GetComponent<Bomb>(); //
            if (bombComponent != null) //
            {
                bombComponent.playerCamera = playerCamera; //
            }
            AudioSource.PlayClipAtPoint(bombComponent.spawnSound, transform.position); //
            RemoveBomb(consumableSlot); //
        }
    }

    public void AddConsumable(CollectibleItem collectibleItem)
    {
        if (collectibleItem == null || !collectibleItem.isConsumable) return;

        BombCollectible bombCollectible = collectibleItem.GetComponent<BombCollectible>();
        if (bombCollectible == null) return;

        ObstacleColor newBombColor = bombCollectible.bombColor;
        bool slotFound = false;

        // FIRST: Check for an existing slot with EXACT same color (excluding Universal)
        for (int i = 0; i < consumableSlots.Count; i++)
        {
            if (consumableSlots[i].gameObject.activeSelf && consumableSlots[i].obstacleColor == newBombColor)
            {
                // Found a slot with the same color
                consumableSlots[i].SetConsumable(collectibleItem);
                UpdateSlotUI(consumableSlots[i]);
                slotFound = true;
                Debug.Log($"Added bomb to existing slot: {collectibleItem.name} (Color: {newBombColor}) to slot: {consumableSlots[i].name}");
                break;
            }
        }

        // SECOND: If no exact color match, check for an empty slot OR a Universal slot
        if (!slotFound)
        {
            for (int i = 0; i < consumableSlots.Count; i++)
            {
                ConsumableSlot slot = consumableSlots[i];

                // Check if slot is inactive (empty slot)
                if (!slot.gameObject.activeSelf)
                {
                    // Use this empty slot
                    slot.gameObject.SetActive(true);
                    AnimateIconPop(slot);
                    slot.SetConsumable(collectibleItem);
                    SetupBombButton(slot);
                    UpdateSlotUI(slot);
                    slotFound = true;
                    Debug.Log($"Added bomb to new slot: {collectibleItem.name} (Color: {newBombColor}) to slot: {slot.name}");
                    break;
                }
            }
        }

        // THIRD: If no empty slot, try to find a Universal slot (only as last resort)
        if (!slotFound)
        {
            for (int i = 0; i < consumableSlots.Count; i++)
            {
                ConsumableSlot slot = consumableSlots[i];
                if (slot.gameObject.activeSelf && slot.obstacleColor == ObstacleColor.Universal)
                {
                    // Found a Universal slot - only use if the new bomb is NOT Universal
                    if (newBombColor != ObstacleColor.Universal)
                    {
                        // Check if the Universal slot can be overwritten
                        // Option 1: Replace the Universal bomb with the colored one
                        slot.SetConsumable(collectibleItem);
                        SetupBombButton(slot);
                        UpdateSlotUI(slot);
                        slotFound = true;
                        Debug.Log($"Replaced Universal slot with colored bomb: {collectibleItem.name} (Color: {newBombColor})");
                        break;
                    }
                    else
                    {
                        // If the new bomb is also Universal, combine them
                        slot.SetConsumable(collectibleItem);
                        UpdateSlotUI(slot);
                        slotFound = true;
                        Debug.Log($"Added Universal bomb to Universal slot: {collectibleItem.name}");
                        break;
                    }
                }
            }
        }

        // If still no slot found, log warning
        if (!slotFound)
        {
            Debug.LogWarning($"No available slot for bomb: {collectibleItem.name} (Color: {newBombColor})");
        }
    }

    // Helper method to update slot UI
    private void UpdateSlotUI(ConsumableSlot slot)
    {
        if (slot.counter <= 1)
        {
            if (slot.counterText != null) slot.counterText.text = "";
            if (slot.bombBackgroundImage != null) slot.bombBackgroundImage.enabled = false;
        }
        else if (slot.counter > 1)
        {
            if (slot.counterText != null) slot.counterText.text = slot.counter.ToString();
            if (slot.bombBackgroundImage != null) slot.bombBackgroundImage.enabled = true;
        }
    }

    // Helper method to setup bomb button events
    private void SetupBombButton(ConsumableSlot slot)
    {
        EventTrigger.Entry bomb = new EventTrigger.Entry();
        bomb.eventID = EventTriggerType.PointerDown;
        bomb.callback.AddListener((data) => { OnPointerDownDelegateBomb((PointerEventData)data, slot.bombPrefab.gameObject, slot); });

        if (slot.btn != null && slot.btn.GetComponent<EventTrigger>() != null)
        {
            slot.btn.GetComponent<EventTrigger>().triggers.Clear(); // Clear any old listeners
            slot.btn.GetComponent<EventTrigger>().triggers.Add(bomb);
        }
    }
    public void RemoveBomb(ConsumableSlot consumableSlot) //
    {
        if (consumableSlot == null) { Debug.LogError("ConsumableSlot is null for bomb removal."); return; } // Safety check

        consumableSlot.counter--; //
        if (consumableSlot.counterText != null) consumableSlot.counterText.text = consumableSlot.counter + ""; //
        if (consumableSlot.counter > 1) //
        {
            Debug.Log("Turning off"); //
            if (consumableSlot.bombBackgroundImage != null) consumableSlot.bombBackgroundImage.enabled = true; //
            if (consumableSlot.counterText != null) consumableSlot.counterText.text = consumableSlot.counter + ""; //
        }
        if (consumableSlot.counter == 1) //
        {
            if (consumableSlot.counterText != null) consumableSlot.counterText.text = ""; //
            if (consumableSlot.bombBackgroundImage != null) consumableSlot.bombBackgroundImage.enabled = false; //
            Debug.Log("Turning off"); //
        }
        else if (consumableSlot.counter <= 0) //
        {
            if (consumableSlot.btn != null && consumableSlot.btn.GetComponent<EventTrigger>() != null) //
            {
                consumableSlot.btn.GetComponent<EventTrigger>().triggers.Clear(); //
            }
            consumableSlot.ClearSlot(); //
        }
    }
    private void AnimateIconPop(ConsumableSlot slot)
    {
        // Get the icon image from the slot's button
        Image iconImage = slot.btn?.GetComponent<Image>();
        if (iconImage != null)
        {
            // Store the original scale
            Vector3 originalScale = iconImage.transform.localScale;

            // Scale up from original, then back
            iconImage.transform.DOScale(originalScale * 1.2f, 0.15f)
                .OnComplete(() => iconImage.transform.DOScale(originalScale, 0.15f)).Play();
        }
    }

}