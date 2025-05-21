using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public NetworkBool JumpPressed;
    public NetworkBool PullHeld;
    public NetworkBool HitPressed;
    public NetworkBool HitDownPressed;
}