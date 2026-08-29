using UnityEngine;

public class PlayerDeadState : IPlayerState
{
    private readonly PlayerController player;

    public PlayerDeadState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        Debug.Log("Player Dead");
        player.SetMovementInput(0f);
        player.Move(0f);
    }

    public void HandleInput()
    {
        // No input allowed while dead
    }

    public void LogicUpdate()
    {
        // Physics still apply but no control
    }

    public void Exit()
    {
    }

    public PlayerStates GetState()
    {
        return PlayerStates.Dead;
    }
}
