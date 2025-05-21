using UnityEngine;
using Fusion;


public class PlayerAnimation : NetworkBehaviour
{
    private Animator _anim;
    private PlayerMovement _playerMovement;
    private Player _player; // Reference to the Player script

    // The current animation state
    public PlayerAnimState _currentAnimState = PlayerAnimState.Idle; // Start with a default state

    // Helper to get networked states easily
    private bool IsGrounded => _playerMovement.IsGrounded;
    private bool IsJumping => _playerMovement.IsJumping; // Corrected from _playerMovement.IsJumping
    private bool IsFalling => _playerMovement.IsFalling;
    private Vector3 CurrentMoveDirection => _playerMovement.CurrentMoveDirection;
    private float MoveSpeed => _player.MoveSpeed;

    public override void Spawned()
    {
        _anim = GetComponent<Animator>();
        if (_anim == null)
        {
            Debug.LogError("PlayerAnimation: Animator component not found on this GameObject.");
            return;
        }

        _playerMovement = GetComponent<PlayerMovement>();
        _player = GetComponent<Player>(); // Assuming Player script is also on the same GameObject
        if (_playerMovement == null)
        {
            Debug.LogError("PlayerAnimation: PlayerMovement component not found on this GameObject.");
        }
        if (_player == null)
        {
            Debug.LogError("PlayerAnimation: Player component not found on this GameObject.");
        }

        // Initialize to the correct starting state
        UpdateAnimationState();
    }

    public override void Render()
    {
        if (_anim == null || _playerMovement == null || _player == null) return;

        UpdateAnimationState(); // Always update state based on current networked data
    }

    private void UpdateAnimationState()
    {
        PlayerAnimState newState = DetermineAnimationState();

        if (newState != _currentAnimState)
        {
            // Transition to the new state
            ExitState(_currentAnimState); // Optional: clean up old state's parameters
            _currentAnimState = newState;
            EnterState(_currentAnimState); // Set new state's parameters
        }
        // If you had continuous state logic (e.g., blend tree parameter updates), it would go here:
        // ExecuteState(_currentAnimState);
    }

    private PlayerAnimState DetermineAnimationState()
{
    // These now come from PlayerMovement and PlayerInteraction
    bool isMoving = _playerMovement.CurrentMoveDirection != Vector3.zero;
    bool isRunning = isMoving && _playerMovement.IsGrounded && _player.MoveSpeed >= 3;
    bool isWalking = isMoving && _playerMovement.IsGrounded && _player.MoveSpeed < 3;
    // Check interaction states as well
    // bool isInteracting = _playerInteraction.IsPulling || _playerInteraction.IsPushing;

    // Prioritize airborne states
    if (IsJumping)
    {
        return PlayerAnimState.Jump;
    }
    else if (IsFalling && !IsGrounded)
    {
        return PlayerAnimState.Fall;
    }
    else if (IsGrounded)
    {
        // Prioritize interaction over movement if interacting
       
        if (isRunning)
        {
            return PlayerAnimState.Run;
        }
        else if (isWalking)
        {
            return PlayerAnimState.Walk;
        }
        else // Not moving, grounded, not jumping, not falling, not interacting
        {
            // If the player is supposed to go AFK after a period of idle,
            // this might be handled by a timer in PlayerController, which then
            // sets PlayerAnimation.SetAFK(true)
            // if (_playerMovement.IsAFK) // Assuming PlayerMovement has an IsAFK networked property
            // {
            //     return PlayerAnimState.AFK;
            // }
            return PlayerAnimState.Idle;
        }
    }

    return _currentAnimState; // Fallback
}

    private void EnterState(PlayerAnimState state)
    {
        // Reset all relevant animation booleans to ensure clean transitions
        _anim.SetBool("Grounded", false);
        _anim.SetBool("Jumping", false);
        _anim.SetBool("Falling", false);
        _anim.SetBool("Running", false);
        _anim.SetBool("Walking", false);
        _anim.SetBool("AFK", false);

        // Set the boolean for the entering state
        switch (state)
        {
            case PlayerAnimState.Idle:
                _anim.SetBool("Grounded", true); // Idle implies grounded
                _anim.SetBool("Walking", false); // Explicitly ensure movement is off
                _anim.SetBool("Running", false); // Explicitly ensure movement is off
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
                // No need to set grounded here, as it's a jump
                break;
            case PlayerAnimState.Fall:
                _anim.SetBool("Falling", true);
                // No need to set grounded here
                break;
            case PlayerAnimState.AFK:
                _anim.SetBool("Grounded", true);
                _anim.SetBool("AFK", true);
                break;
                // Add cases for other states
        }
    }

    private void ExitState(PlayerAnimState state)
    {
        // For simple boolean-driven states, EnterState's resetting logic might be enough.
        // But if you had specific triggers or complex clean-up, it would go here.
        // For example, if you had a "Landing" trigger, you might trigger it on exit from Fall.
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
    HitDown,
    Dead,
    Pull, // Add for pulling animation state
    Push // Add for pushing animation state
}