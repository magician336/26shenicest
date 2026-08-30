using UnityEngine;

public class PlayerIdleState : IPlayerState
{
    private readonly PlayerController player;

    public PlayerIdleState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        player.Move(0f);
        player.GetComponent<SimpleWalkAnimation>()?.SetMoving(false);
    }

    public void HandleInput()
    {
        if (player.ConsumeInteractInput())
        {
            player.ChangeState(player.InteractState);
            return;
        }

        if (Mathf.Abs(player.HorizontalInput) > 0.01f)
        {
            player.ChangeState(player.RunState);
        }
    }

    public void LogicUpdate()
    {
        player.Move(0f);
    }

    public void Exit()
    {
    }

    public PlayerStates GetState()
    {
        return PlayerStates.Idle;
    }
}
