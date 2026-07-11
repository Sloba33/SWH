using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private InputActions inputActions;
    private Vector2 _moveInput;

    /// <summary>
    /// Reads zero while the pre-match countdown freeze is on (the backing value
    /// still updates, so a stick held through "GO!" engages immediately).
    /// </summary>
    public Vector2 MoveInput => InputLocked ? Vector2.zero : _moveInput;

    // Players may not act during the pre-match countdown. Replay take sessions
    // are exempt: there the first action WHILE frozen is what starts recording.
    private static bool InputLocked =>
        GameManager.Instance != null && GameManager.Instance.PreMatchFreeze && !GameManager.Instance.IsReplayTakeSession;

    private CameraController cameraController;

    private void Awake()
    {
        inputActions = new InputActions();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += _ => _moveInput = Vector2.zero;

        // No need to set up specific event handlers for these in PlayerInputHandler.
        // WasPressedThisFrame() handles the single-frame detection automatically.
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        Vector2 raw = ctx.ReadValue<Vector2>();

        // Only keyboard / real-gamepad input needs rotation here. The on-screen
        // stick's value is already camera-rotated through the joystickHolder
        // transform (the authored visual tilt isn't part of that path), so any
        // rotation we apply on top of it just doubles up. Virtual devices
        // created by OnScreenControl report native == false, so we use that to
        // skip the rotation for joystick input.
        if (ctx.control.device.native)
        {
            if (cameraController == null)
                cameraController = FindObjectOfType<CameraController>();

            if (cameraController != null)
            {
                float thetaRad = cameraController.CameraYawDegrees * Mathf.Deg2Rad;
                float c = Mathf.Cos(thetaRad);
                float s = Mathf.Sin(thetaRad);
                raw = new Vector2(raw.x * c + raw.y * s, -raw.x * s + raw.y * c);
            }
        }

        OnMove(raw);
    }

    private void OnMove(Vector2 rawInput)
    {
        // Prevent diagonal movement, prioritize larger axis
        if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
        {
            _moveInput = new Vector2(Mathf.Sign(rawInput.x), 0);
        }
        else if (Mathf.Abs(rawInput.y) > 0)
        {
            _moveInput = new Vector2(0, Mathf.Sign(rawInput.y));
        }
        else
        {
            _moveInput = Vector2.zero;
        }
    }

    // New methods to check if attack buttons were pressed this frame
    public bool GetJumpPressedThisFrame()
    {
        if (InputLocked) return false;
        return inputActions.Player.Jump.WasPressedThisFrame();
    }

    public bool GetHitPressedThisFrame()
    {
        if (InputLocked) return false;
        return inputActions.Player.Hit.WasPressedThisFrame(); // Assuming "Hit" action for C key
    }

    public bool GetHitDownPressedThisFrame()
    {
        if (InputLocked) return false;
        return inputActions.Player.HitDown.WasPressedThisFrame(); // Assuming "HitDown" action for X key
    }

    // You might need a "Special" action in your InputActions for keyboard/gamepad special attack
    public bool GetSpecialAttackPressedThisFrame()
    {
        if (InputLocked) return false;
        // Assuming you add a "Special" action (e.g., bound to 'V' key from PlayerControls)
        return inputActions.Player.Special.WasPressedThisFrame();
    }
    public bool GetPullPressedThisFrame()
    {
        if (InputLocked) return false;
        return inputActions.Player.Pull.WasPressedThisFrame();
    }
    public bool GetPullReleasedThisFrame()
    {
        if (InputLocked) return false;
        return inputActions.Player.Pull.WasReleasedThisFrame();
    }


}