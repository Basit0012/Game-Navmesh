using UnityEngine;

namespace RelicDash.Core
{
    /// <summary>
    /// State entered when the cinematic concludes and exit gate portal is open.
    /// Displays victory UI and statistics.
    /// </summary>
    public class VictoryState : IGameState
    {
        private readonly GameManager _gameManager;

        public GameState StateType => GameState.Victory;

        public VictoryState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            Debug.Log("[GameState] Entered VictoryState: Ancient exit gate unsealed! Mission Complete.");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameEvents.TriggerVictoryAchieved();
            GameEvents.TriggerGameStateChanged(GameState.Victory);
        }

        public void Update()
        {
            // Victory screen updates
        }

        public void Exit()
        {
            Debug.Log("[GameState] Exited VictoryState.");
        }
    }
}
