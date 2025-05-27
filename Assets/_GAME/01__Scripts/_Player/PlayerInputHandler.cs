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
}

