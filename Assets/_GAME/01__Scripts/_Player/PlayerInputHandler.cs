using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private InputActions inputActions;
    public Vector2 MoveInput { get; private set; }

    private CameraController cameraController;

    // --- Bot control ---------------------------------------------------------
    // When BotControlled is true this handler ignores real device input and is
    // instead driven by BotController through the BotSet*/BotQueue* methods
    // below. Every consumer (PlayerMovement, PlayerAttack, PlayerController)
    // keeps reading this handler exactly as it does for a human, so the bot runs
    // the identical movement/push/pull/attack pipeline.
    [HideInInspector] public bool BotControlled = false;
    private bool _botJump, _botHit, _botHitDown, _botSpecial, _botPull, _botPullReleased;

    public void BotSetMove(Vector2 dir) { if (BotControlled) MoveInput = dir; }
    public void BotQueueJump() { _botJump = true; }
    public void BotQueueHit() { _botHit = true; }
    public void BotQueueHitDown() { _botHitDown = true; }
    public void BotQueueSpecial() { _botSpecial = true; }
    public void BotQueuePull() { _botPull = true; }
    public void BotQueuePullReleased() { _botPullReleased = true; }

    private void Awake()
    {
        inputActions = new InputActions();
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += _ => { if (!BotControlled) MoveInput = Vector2.zero; };

        // No need to set up specific event handlers for these in PlayerInputHandler.
        // WasPressedThisFrame() handles the single-frame detection automatically.
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (BotControlled) return; // Bot drives MoveInput via BotSetMove.

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

    // New methods to check if attack buttons were pressed this frame.
    // When bot-controlled, each returns the queued one-shot and clears it so it
    // fires for exactly one consumer-read, mirroring WasPressedThisFrame.
    public bool GetJumpPressedThisFrame()
    {
        if (BotControlled) { bool v = _botJump; _botJump = false; return v; }
        return inputActions.Player.Jump.WasPressedThisFrame();
    }

    public bool GetHitPressedThisFrame()
    {
        if (BotControlled) { bool v = _botHit; _botHit = false; return v; }
        return inputActions.Player.Hit.WasPressedThisFrame(); // Assuming "Hit" action for C key
    }

    public bool GetHitDownPressedThisFrame()
    {
        if (BotControlled) { bool v = _botHitDown; _botHitDown = false; return v; }
        return inputActions.Player.HitDown.WasPressedThisFrame(); // Assuming "HitDown" action for X key
    }

    // You might need a "Special" action in your InputActions for keyboard/gamepad special attack
    public bool GetSpecialAttackPressedThisFrame()
    {
        if (BotControlled) { bool v = _botSpecial; _botSpecial = false; return v; }
        // Assuming you add a "Special" action (e.g., bound to 'V' key from PlayerControls)
        return inputActions.Player.Special.WasPressedThisFrame();
    }
    public bool GetPullPressedThisFrame()
    {
        if (BotControlled) { bool v = _botPull; _botPull = false; return v; }
        return inputActions.Player.Pull.WasPressedThisFrame();
    }
    public bool GetPullReleasedThisFrame()
    {
        if (BotControlled) { bool v = _botPullReleased; _botPullReleased = false; return v; }
        return inputActions.Player.Pull.WasReleasedThisFrame();
    }


}