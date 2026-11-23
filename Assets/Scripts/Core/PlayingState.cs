using UnityEngine;

namespace RelicDash.Core
{
    /// <summary>
    /// State when player is actively exploring, avoiding the guardian, and collecting relics.
    /// </summary>
    public class PlayingState : IGameState
    {
        private readonly GameManager _gameManager;

        public GameState StateType => GameState.Playing;

        public PlayingState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            Debug.Log("[GameState] Entered PlayingState.");
            Time.timeScale = 1.0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            GameEvents.TriggerGameStateChanged(GameState.Playing);
        }

        public void Update()
        {
            // Playing-specific frame updates if needed
        }

        public void Exit()
        {
            Debug.Log("[GameState] Exited PlayingState.");
        }
    }
}
