using UnityEngine;

public class MobileInput : MonoBehaviour
{
    public static MobileInput Instance;

    public FloatingJoystick joystick;
    public bool JumpPressed { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool PullHeld { get; private set; }

    public void OnJumpButtonDown() => JumpPressed = true;
    public void OnAttackButtonDown() => AttackPressed = true;
    public void OnPullButtonDown() => PullHeld = true;
    public void OnPullButtonUp() => PullHeld = false;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public void ResetFrameInputs()
    {
        JumpPressed = false;
        AttackPressed = false;
        // PullHeld is continuous
    }

    public NetworkInputData CreateNetworkInput()
    {
        var data = new NetworkInputData
        {
            MoveDirection = new Vector2(joystick.Horizontal, joystick.Vertical),
            JumpPressed = JumpPressed,
            HitPressed = AttackPressed,
            // HitDownPressed = AttackDownPressed;
            PullHeld = PullHeld
        };

        ResetFrameInputs();
        return data;
    }
}
