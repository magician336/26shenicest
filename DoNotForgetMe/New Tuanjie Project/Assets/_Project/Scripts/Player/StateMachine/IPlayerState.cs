public interface IPlayerState
{
    void Enter();
    void HandleInput();
    void LogicUpdate();
    void Exit();

    PlayerStates GetState();
}
