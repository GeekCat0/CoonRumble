using UnityEngine;

public enum PlayerMovementState // Movement related player states
{
    Idling = 0,
    Running = 1,
    Jumping = 4,
    Falling = 5,
    WallRunning = 6,
    Sliding = 7
}
public enum PlayerActionState // Actions player can perform
{
    Idling = 0,
    Attacking = 1,
    Dashing = 2
}
public class PlayerState : MonoBehaviour 
{
    // We want other functions to be able to get the player state but only this class can set it
    [field: SerializeField] public PlayerMovementState CurrentPlayerMovementState { get; private set; } = PlayerMovementState.Idling; 
    [field: SerializeField] public PlayerActionState CurrentPlayerActionState { get; private set; } = PlayerActionState.Idling;

    public void SetPlayerMovementState(PlayerMovementState playerMovementState)
    {
        CurrentPlayerMovementState = playerMovementState;
    }
    public void SetPlayerActionState (PlayerActionState playerActionState)
    {
        CurrentPlayerActionState = playerActionState; 
    }
    public bool InGroundedState()
    {
        return IsStateGroundedState(CurrentPlayerMovementState);
    }
    public bool IsStateGroundedState(PlayerMovementState playerMovementState) // If the player is Idling or Running then we know they're on the ground duh 
    {
        return playerMovementState == PlayerMovementState.Idling || playerMovementState == PlayerMovementState.Running || playerMovementState == PlayerMovementState.Sliding;
    }
}
