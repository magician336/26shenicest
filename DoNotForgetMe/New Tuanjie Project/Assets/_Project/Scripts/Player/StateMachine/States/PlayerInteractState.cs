using UnityEngine;

public class PlayerInteractState : IPlayerState
{
    private readonly PlayerController player;
    private float interactionTimer;
    private const float InteractionLockDuration = 0.2f;

    public PlayerInteractState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        interactionTimer = InteractionLockDuration;
        player.PerformInteraction();
    }

    public void HandleInput()
    {
        if (player.ConsumeInteractInput())
        {
            player.PerformInteraction();
        }
    }

    public void LogicUpdate()
    {
        interactionTimer -= Time.deltaTime;

        if (interactionTimer > 0f)
        {
            return;
        }

        if (Mathf.Abs(player.HorizontalInput) > 0.01f)
        {
            player.ChangeState(player.RunState);
        }
        else
        {
            player.ChangeState(player.IdleState);
        }
    }

    public void Exit()
    {
    }

    public PlayerStates GetState()
    {
        return PlayerStates.Interact;
    }
}
