namespace RelicDash.Core
{
    /// <summary>
    /// Interface for game states in the State Design Pattern.
    /// </summary>
    public interface IGameState
    {
        GameState StateType { get; }
        void Enter();
        void Update();
        void Exit();
    }
}
