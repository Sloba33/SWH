using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator _anim;
    private PlayerMovement _playerMovement;
    private Player _player;
    private PlayerController _playerController; // Assuming you have this reference now

    // Change this line:
    [field: SerializeField] // This attribute makes the public property visible in the Inspector
    public PlayerAnimState CurrentAnimState { get; private set; } = PlayerAnimState.Idle;

    // Helper properties to keep code clean and readable
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
        _playerController = GetComponent<PlayerController>();

        if (_anim == null) Debug.LogError("PlayerAnimation: Animator component missing.");
        if (_playerMovement == null) Debug.LogError("PlayerAnimation: PlayerMovement component missing.");
        if (_player == null) Debug.LogError("PlayerAnimation: Player component missing.");
        if (_playerController == null) Debug.LogError("PlayerAnimation: PlayerController component missing.");
    }

    private void Update()
    {
        if (_anim == null || _playerMovement == null || _player == null || _playerController == null) return;

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        var newState = DetermineAnimationState();

        if (newState != CurrentAnimState)
        {
            ExitState(CurrentAnimState);
            CurrentAnimState = newState; // This update will now be visible in the Inspector
            EnterState(CurrentAnimState);
        }
    }

    private PlayerAnimState DetermineAnimationState()
    {
        bool isMoving = CurrentMoveDirection != Vector3.zero;
        bool isRunning = isMoving && IsGrounded && MoveSpeed >= 3f;
        bool isWalking = isMoving && IsGrounded && MoveSpeed < 3f;

        // Prioritize states
        if (_playerController.isPushing)
            return PlayerAnimState.Push;
        if (_playerController.isPulling)
            return PlayerAnimState.Pull;

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

            if (!isMoving && !_playerController.isPushing && !_playerController.isPulling)
            {
                return PlayerAnimState.Idle;
            }
        }

        return CurrentAnimState; // Fallback
    }

    private void EnterState(PlayerAnimState state)
    {
        _anim.SetBool("Grounded", IsGrounded);
        _anim.SetBool("Jumping", IsJumping);
        _anim.SetBool("Falling", IsFalling);
        _anim.SetBool("Running", false);
        _anim.SetBool("Walking", false);
        _anim.SetBool("Idle", false);
        _anim.SetBool("Hit", false);
        _anim.SetBool("HitDown", false);
        _anim.SetBool("Dead", false);
        _anim.SetBool("Pull", false);
        _anim.SetBool("Push", false);

        switch (state)
        {
            case PlayerAnimState.Walk:
                _anim.SetBool("Walking", true);
                break;
            case PlayerAnimState.Run:
                _anim.SetBool("Running", true);
                break;
            case PlayerAnimState.Jump:
                _anim.SetBool("Jumping", true);
                break;
            case PlayerAnimState.Fall:
                _anim.SetBool("Falling", true);
                break;
            case PlayerAnimState.Idle:
                _anim.SetBool("Idle", true);
                LookAnimation(); // Only call if Idle animation specifically requires this
                break;
            case PlayerAnimState.Hit:
                _anim.SetBool("Hit", true);
                break;
            case PlayerAnimState.HitDown:
                _anim.SetBool("HitDown", true);
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
        // No specific exit logic implemented yet
    }

    public void LookAnimation()
    {
        if (_playerController == null || _playerController.playerCamera == null) return;

        Vector3 cameraDirection = _playerController.playerCamera.transform.position - transform.position;
        cameraDirection.y = 0;
        if (cameraDirection == Vector3.zero) return;

        Vector3 localCameraDirection = transform.InverseTransformDirection(cameraDirection);

        if (Vector3.Dot(transform.forward, cameraDirection.normalized) > 0.5f)
        {
            if (localCameraDirection.x >= 0.1f)
            {
                _anim.SetBool("LookLeft", false);
                _anim.SetBool("LookRight", true);
            }
            else if (localCameraDirection.x <= -0.1f)
            {
                _anim.SetBool("LookLeft", true);
                _anim.SetBool("LookRight", false);
            }
            else
            {
                _anim.SetBool("LookLeft", false);
                _anim.SetBool("LookRight", false);
            }
        }
        else
        {
            _anim.SetBool("LookLeft", false);
            _anim.SetBool("LookRight", false);
        }
    }
}

public enum PlayerAnimState
{
    Idle,
    Walk,
    Run,
    Jump,
    Fall,
    Hit,
    HitDown,
    Dead,
    Pull,
    Push
}