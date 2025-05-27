using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator _anim;
    private PlayerMovement _playerMovement;
    private Player _player;

    public PlayerAnimState CurrentAnimState { get; private set; } = PlayerAnimState.Idle;

    private bool IsGrounded => _playerMovement != null && _playerMovement.IsGrounded;
    private bool IsJumping => _playerMovement != null && _playerMovement.IsJumping;
    private bool IsFalling => _playerMovement != null && _playerMovement.IsFalling;
    private Vector3 CurrentMoveDirection => _playerMovement != null ? _playerMovement.CurrentMoveDirection : Vector3.zero;
    private float MoveSpeed => _player != null ? _player.MoveSpeed : 0f;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _playerMovement = GetComponent<PlayerMovement>();
        _player = GetComponent<Player>();

        if (_anim == null) Debug.LogError("PlayerAnimation: Animator component missing.");
        if (_playerMovement == null) Debug.LogError("PlayerAnimation: PlayerMovement component missing.");
        if (_player == null) Debug.LogError("PlayerAnimation: Player component missing.");
    }

    private void Update()
    {
        if (_anim == null || _playerMovement == null || _player == null) return;

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        var newState = DetermineAnimationState();

        if (newState != CurrentAnimState)
        {
            ExitState(CurrentAnimState);
            CurrentAnimState = newState;
            EnterState(CurrentAnimState);
        }
    }

    private PlayerAnimState DetermineAnimationState()
    {
        bool isMoving = CurrentMoveDirection != Vector3.zero;
        Debug.Log("Is moving" + isMoving + " | Move Speed: " + MoveSpeed);
        bool isRunning = isMoving && IsGrounded && MoveSpeed >= 3f;
        bool isWalking = isMoving && IsGrounded && MoveSpeed < 3f;

        if (IsJumping)
            return PlayerAnimState.Jump;

        if (IsFalling && !IsGrounded)
            return PlayerAnimState.Fall;

        if (IsGrounded)
        {
            if (isRunning)
                return PlayerAnimState.Run;
            if (isWalking)
                return PlayerAnimState.Walk;

            return PlayerAnimState.Idle;
        }

        // Fallback to current state
        return CurrentAnimState;
    }

    private void EnterState(PlayerAnimState state)
    {
        // Reset all animation flags before setting the new one
        _anim.SetBool("Grounded", false);
        _anim.SetBool("Jumping", false);
        _anim.SetBool("Falling", false);
        _anim.SetBool("Running", false);
        _anim.SetBool("Walking", false);
        _anim.SetBool("AFK", false);
        _anim.SetBool("Hit", false);
        _anim.SetBool("Dead", false);
        _anim.SetBool("Pull", false);
        _anim.SetBool("Push", false);

        switch (state)
        {
            case PlayerAnimState.Idle:
                _anim.SetBool("Grounded", true);
                break;
            case PlayerAnimState.Walk:
                _anim.SetBool("Grounded", true);
                _anim.SetBool("Walking", true);
                break;
            case PlayerAnimState.Run:
                _anim.SetBool("Grounded", true);
                _anim.SetBool("Running", true);
                break;
            case PlayerAnimState.Jump:
                _anim.SetBool("Jumping", true);
                break;
            case PlayerAnimState.Fall:
                _anim.SetBool("Falling", true);
                break;
            case PlayerAnimState.AFK:
                _anim.SetBool("Grounded", true);
                _anim.SetBool("AFK", true);
                break;
            case PlayerAnimState.Hit:
                _anim.SetBool("Hit", true);
                break;
            case PlayerAnimState.Dead:
                _anim.SetBool("Dead", true);
                break;
            case PlayerAnimState.Pull:
                _anim.SetBool("Pull", true);
                break;
            case PlayerAnimState.Push:
                _anim.SetBool("Push", true);
                break;
        }
    }

    private void ExitState(PlayerAnimState state)
    {
        // If you want to handle any cleanup or triggers when leaving a state, do it here
    }
}

public enum PlayerAnimState
{
    Idle,
    Walk,
    Run,
    Jump,
    Fall,
    AFK,
    Hit,
    Dead,
    Pull,
    Push
}
