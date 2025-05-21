using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Fusion;
public class PlayerControls : MonoBehaviour
{
    public List<ConsumableSlot> consumableSlots = new();

    public Image pullFrame, pullHand;
    public Color interactableColor, uninteractableColor;
    public TextMeshProUGUI bombCountText;
    public int bombCounter;
    public PlayerController playerController;
    public GameObject playerCamera;
    public Camera playerRegularCamera;
    public GameObject bombPrefab;
    public Button pullButton, jumpButton, hitButton, hitDownButton, bombButton;
    public Image hitFill, hitDownFill;
    public RectTransform joystickHolder;
    public PlayerAttack playerAttack;

    public LevelGoal levelGoal;
    public Image handJoystick, handJump;
    public GameObject hintPull;
    public bool isPulling;
    public TutorialHandler tutorialHandler;
    public Button specialButton;
    // Start is called before the first frame update

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.V))
        {
            PlaceBombViaBind();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            playerController.StartPull();
        }
        else if (Input.GetKeyUp(KeyCode.Z))
        {
            playerController.StopPull();
        }

    }

    public void AssignControls()
    {
        playerController = FindObjectOfType<PlayerController>();
        playerAttack = playerController.GetComponent<PlayerAttack>();
        playerController.GetComponent<Player>().hitFillImage = hitFill;
        playerController.GetComponent<Player>().hitDownFillImage = hitDownFill;
    }
    private IEnumerator Start()
    {
        _isInteractable = true;
        levelGoal = FindObjectOfType<LevelGoal>();
        yield return new WaitForSeconds(0.5f);

        // playerController.joystick = dynamicJoystick;
        Invoke(nameof(SetReferences), 1f);
        if (levelGoal.Tutorial)
        {

            if (levelGoal.levelType == LevelGoal.LevelType.Move)
            {

                tutorialHandler.shouldGuide = true;
                handJoystick.gameObject.SetActive(true);
            }
            // if (levelGoal.levelType == LevelGoal.LevelType.Pull)
            // {
            //     Debug.Log("Enabling pull");
            //     handPull.gameObject.SetActive(true);
            // }
            if (levelGoal.levelType == LevelGoal.LevelType.Bomb)
            {
                hintPull.gameObject.SetActive(false);
                handJump.gameObject.SetActive(false);
                // handJoystick.gameObject.SetActive(false);
                tutorialHandler.shouldGuide = false;
            }

            if (levelGoal.bombCount == 0 && !levelGoal.bombs)
            {
                bombButton.gameObject.SetActive(false);
            }
            else if (levelGoal.bombs && levelGoal.bombCount == 0)
            {
                bombButton.gameObject.SetActive(false);
            }
            if (!levelGoal.weapons)
            {
                hitButton.gameObject.SetActive(false);
                hitDownButton.gameObject.SetActive(false);
            }
            if (!levelGoal.jump)
            {
                jumpButton.gameObject.SetActive(false);
            }

            if (!levelGoal.pull)
            {
                pullButton.gameObject.SetActive(false);
            }
            // interactableColor = new Color(pullFrame.color.r, pullFrame.color.g, pullFrame.color.b, 1f);

            // uninteractableColor = new Color(pullFrame.color.r, pullFrame.color.g, pullFrame.color.b, 0.3f);
            // // uninteractableColor.a = 0.3f;

        }
    }
    public bool _isInteractable;
    public void Interactable(bool interactable)
    {
        if (interactable && !_isInteractable)
        {
            _isInteractable = true;
            pullFrame.color = interactableColor;
            pullHand.color = interactableColor;
        }
        else if (!interactable && _isInteractable)
        {
            _isInteractable = false;
            pullFrame.color = uninteractableColor;
            pullHand.color = uninteractableColor;
        }
    }
    public void SetReferences()
    {
        if (playerController == null) return;
        playerRegularCamera = playerController.playerRegularCamera;

        playerCamera = playerController.camerasPrefab;
        hitButton.onClick.AddListener(() =>
        {
            playerAttack.Hit();
        });
        if (!levelGoal.Tutorial)
            specialButton.onClick.AddListener(() =>
            {
                playerAttack.SpecialAttack();
            });
        hitDownButton.onClick.AddListener(() =>
        {
            playerAttack.HitDown();
        });
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { OnPointerDownDelegatePull((PointerEventData)data); });
        pullButton.GetComponent<EventTrigger>().triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { OnPointerUpDelegatePull((PointerEventData)data); });
        pullButton.GetComponent<EventTrigger>().triggers.Add(pointerUpEntry);

        EventTrigger.Entry jump = new EventTrigger.Entry();
        jump.eventID = EventTriggerType.PointerDown;
        jump.callback.AddListener((data) => { OnPointerDownDelegateJump((PointerEventData)data); });
        jumpButton.GetComponent<EventTrigger>().triggers.Add(jump);

        // EventTrigger.Entry bomb = new EventTrigger.Entry();
        // bomb.eventID = EventTriggerType.PointerDown;
        // bomb.callback.AddListener((data) => { OnPointerDownDelegateBomb((PointerEventData)data); });
        // bombButton.GetComponent<EventTrigger>().triggers.Add(bomb);

    }
    public void OnPointerDownDelegatePull(PointerEventData eventData)
    {
        MobileInput.Instance.OnPullButtonDown();
        isPulling = true;
        if (hintPull != null) hintPull.SetActive(false);
    }
    public void OnPointerUpDelegatePull(PointerEventData eventData)
    {
        MobileInput.Instance.OnPullButtonUp();
        isPulling = false;
        if (levelGoal?.Tutorial == true && hintPull != null)
            hintPull.SetActive(true);
    }
    public void OnPointerDownDelegateJump(PointerEventData eventData)
    {
        MobileInput.Instance.OnJumpButtonDown();
        if (handJump != null) handJump.gameObject.SetActive(false);
        else if (levelGoal != null && levelGoal.Tutorial && levelGoal.jumpHint != null) levelGoal.jumpHint.gameObject.SetActive(false);
        Debug.Log("Jumping");
    }
    private void PlaceBomb()
    {
        if (bombCounter > 0)
        {
            Debug.Log("Placing bomb");
            Vector3 position = playerController.FindNeighbouringTile();
            position.y = Mathf.Round(playerController.transform.position.y);
            GameObject bomb = Instantiate(bombPrefab, position, bombPrefab.transform.rotation);
            bomb.GetComponent<Bomb>().playerCamera = playerCamera;
            bombCounter--;
            bombCountText.text = bombCounter.ToString();
            RemoveBomb(consumableSlots[0]);

        }
        else
        {
            Debug.Log("No bombs left to place.");
        }

    }
    private void PlaceBombViaBind()
    {
        Debug.Log("Placing bomb");
        Vector3 position = playerController.FindNeighbouringTile();
        position.y = Mathf.Round(playerController.transform.position.y);
        GameObject bomb = Instantiate(bombPrefab, position, bombPrefab.transform.rotation);
        bomb.GetComponent<Bomb>().playerCamera = playerCamera;
        bombCounter--;
        // bombCountText.text = bombCounter.ToString();
        RemoveBomb(consumableSlots[0]);
    }
    public void OnPointerDownDelegateBomb(PointerEventData eventData, GameObject bprefab, ConsumableSlot consumableSlot)
    {
        if (consumableSlot.counter < 1)
        {
            return;
        }
        if (playerController.playerMovement.IsBombBlocked || playerController.isPushing)
        {
            Debug.Log("Bomb not placeable " + playerController.playerMovement.IsBombBlocked);

            GameObject bomb = Instantiate(bprefab, playerController.transform.position + new Vector3(0, 0.02f, 0), bprefab.transform.rotation);
            if (bomb.GetComponent<Bomb>() != null && bomb.GetComponent<Bomb>().isColored)
            {
                bomb.GetComponent<Bomb>().boxCollider.enabled = false;
            }
            Bomb bombComponent = bomb.GetComponent<Bomb>();
            bombComponent.playerCamera = playerCamera;
            bombComponent.IgnorePlayerCollision(playerController.GetComponent<Collider>());
            AudioSource.PlayClipAtPoint(bombComponent.spawnSound, transform.position);
            RemoveBomb(consumableSlot);
        }
        else
        {

            Debug.Log("Bomb IS placeable");
            Vector3 position = playerController.FindNeighbouringTile();
            position.y = Mathf.Round(playerController.transform.position.y);
            GameObject bomb = Instantiate(bprefab, position, bprefab.transform.rotation);
            Bomb bombComponent = bomb.GetComponent<Bomb>();
            bombComponent.playerCamera = playerCamera;
            AudioSource.PlayClipAtPoint(bombComponent.spawnSound, transform.position);
            RemoveBomb(consumableSlot);
        }
        // Debug.Log(playerController.WallDetectPosition);
    }
    public void AddConsumable(CollectibleItem collectibleItem)
    {
        if (!collectibleItem.isConsumable) return;

        bool slotFound = false;

        // Check for an existing active slot with the same type or a universal type
        for (int i = 0; i < consumableSlots.Count; i++)
        {
            if (consumableSlots[i].gameObject.activeSelf && (consumableSlots[i].slotType == collectibleItem.GetComponent<BombCollectible>().bombType || consumableSlots[i].slotType == BombCollectible.BombType.None))
            {
                consumableSlots[i].SetConsumable(collectibleItem);
                Debug.Log("Added bomb to existing slot : " + collectibleItem.name + " to slot :" + consumableSlots[i].name + " with bomb prefab : " + consumableSlots[i].bombPrefab.gameObject);

                if (consumableSlots[i].counter <= 1)
                {
                    consumableSlots[i].counterText.text = "";
                    consumableSlots[i].bombBackgroundImage.enabled = false;
                    Debug.Log("Turning off");
                }
                else if (consumableSlots[i].counter > 1)
                {
                    consumableSlots[i].bombBackgroundImage.enabled = true;
                }

                slotFound = true;
                break;
            }
        }

        // If no active slot found, look for an inactive slot to activate and add the consumable
        if (!slotFound)
        {
            for (int i = 0; i < consumableSlots.Count; i++)
            {
                if (!consumableSlots[i].gameObject.activeSelf)
                {
                    ConsumableSlot slot = consumableSlots[i];
                    slot.gameObject.SetActive(true);
                    slot.SetConsumable(collectibleItem);

                    EventTrigger.Entry bomb = new EventTrigger.Entry();
                    bomb.eventID = EventTriggerType.PointerDown;
                    bomb.callback.AddListener((data) => { OnPointerDownDelegateBomb((PointerEventData)data, slot.bombPrefab.gameObject, slot); });
                    slot.btn.GetComponent<EventTrigger>().triggers.Add(bomb);

                    Debug.Log("Added bomb to new slot : " + collectibleItem.name + " to slot :" + slot.name + " with bomb prefab : " + slot.bombPrefab.gameObject);

                    if (slot.counter <= 1)
                    {
                        slot.counterText.text = "";
                        if (slot.bombBackgroundImage != null) slot.bombBackgroundImage.enabled = false;
                        Debug.Log("Turning off");
                    }
                    else if (slot.counter > 1)
                    {
                        slot.bombBackgroundImage.enabled = true;
                    }

                    break;
                }
            }
        }


    }
    public void RemoveBomb(ConsumableSlot consumableSlot)
    {
        consumableSlot.counter--;
        consumableSlot.counterText.text = consumableSlot.counter + "";
        if (consumableSlot.counter > 1)
        {
            Debug.Log("Turning off");
            consumableSlot.bombBackgroundImage.enabled = true;
            consumableSlot.counterText.text = consumableSlot.counter + "";
        }
        if (consumableSlot.counter == 1)
        {

            consumableSlot.counterText.text = "";
            consumableSlot.bombBackgroundImage.enabled = false;
            Debug.Log("Turning off");


        }
        else if (consumableSlot.counter <= 0)
        {
            consumableSlot.btn.GetComponent<EventTrigger>().triggers.Clear();
            consumableSlot.ClearSlot();
        }
    }
}