using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private InputActions inputActions;
    public Vector2 MoveInput { get; private set; }

    private void Awake()
    {
        inputActions = new InputActions();
        inputActions.Player.Move.performed += ctx => OnMove(ctx.ReadValue<Vector2>());
        inputActions.Player.Move.canceled += _ => MoveInput = Vector2.zero;

        // No need to set up specific event handlers for these in PlayerInputHandler.
        // WasPressedThisFrame() handles the single-frame detection automatically.
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void OnMove(Vector2 rawInput)
    {
        // Prevent diagonal movement, prioritize larger axis
        if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
        {
            MoveInput = new Vector2(Mathf.Sign(rawInput.x), 0);
        }
        else if (Mathf.Abs(rawInput.y) > 0)
        {
            MoveInput = new Vector2(0, Mathf.Sign(rawInput.y));
        }
        else
        {
            MoveInput = Vector2.zero;
        }
    }

    // New methods to check if attack buttons were pressed this frame
    public bool GetJumpPressedThisFrame()
    {
        return inputActions.Player.Jump.WasPressedThisFrame();
    }

    public bool GetHitPressedThisFrame()
    {
        return inputActions.Player.Hit.WasPressedThisFrame(); // Assuming "Hit" action for C key
    }

    public bool GetHitDownPressedThisFrame()
    {
        return inputActions.Player.HitDown.WasPressedThisFrame(); // Assuming "HitDown" action for X key
    }

    // You might need a "Special" action in your InputActions for keyboard/gamepad special attack
    public bool GetSpecialAttackPressedThisFrame()
    {
        // Assuming you add a "Special" action (e.g., bound to 'V' key from PlayerControls)
        return inputActions.Player.Special.WasPressedThisFrame();
    }
}