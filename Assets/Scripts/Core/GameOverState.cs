using UnityEngine;

namespace RelicDash.Core
{
    /// <summary>
    /// State triggered when the autonomous guardian catches the player.
    /// Displays game over UI and unlocks cursor for restart.
    /// </summary>
    public class GameOverState : IGameState
    {
        private readonly GameManager _gameManager;

        public GameState StateType => GameState.GameOver;

        public GameOverState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            Debug.Log("[GameState] Entered GameOverState: Guardian caught the player!");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameEvents.TriggerGameStateChanged(GameState.GameOver);
        }

        public void Update()
        {
            // Game over idle updates
        }

        public void Exit()
        {
            Debug.Log("[GameState] Exited GameOverState.");
        }
    }
}
