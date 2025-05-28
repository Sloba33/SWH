using UnityEngine.Events;

public static class GameEvents
{
    // Define a UnityEvent for the Jump button click
    // This allows you to hook it up in the Inspector if you prefer,
    // or through code.
    public static UnityEvent OnJumpButtonPressed = new UnityEvent();

    // You could also use a standard C# event if you prefer pure code subscription:
    // public static event System.Action OnJumpButtonPressed;
}
