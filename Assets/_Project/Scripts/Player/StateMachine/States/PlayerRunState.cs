using UnityEngine;

public class PlayerRunState : IPlayerState
{
    private readonly PlayerController player;

    public PlayerRunState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
    }

    public void HandleInput()
    {
        if (player.ConsumeInteractInput())
        {
            player.ChangeState(player.InteractState);
            return;
        }

        if (Mathf.Abs(player.HorizontalInput) < 0.01f)
        {
            player.ChangeState(player.IdleState);
        }
    }

    public void LogicUpdate()
    {
        player.Move(player.HorizontalInput);
    }

    public void Exit()
    {
    }

    public PlayerStates GetState()
    {
        return PlayerStates.Run;
    }
}
