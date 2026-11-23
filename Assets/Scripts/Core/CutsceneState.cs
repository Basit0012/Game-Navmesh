using UnityEngine;

namespace RelicDash.Core
{
    /// <summary>
    /// State active during the 5-second cinematic Timeline sequence after 3 relics are collected.
    /// Disables player inputs and hands camera control over to Timeline.
    /// </summary>
    public class CutsceneState : IGameState
    {
        private readonly GameManager _gameManager;

        public GameState StateType => GameState.Cutscene;

        public CutsceneState(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Enter()
        {
            Debug.Log("[GameState] Entered CutsceneState: Player controls locked, cinematic active.");
            GameEvents.TriggerCutsceneStarted();
            GameEvents.TriggerGameStateChanged(GameState.Cutscene);
        }

        public void Update()
        {
            // Cinematic camera / timeline updates
        }

        public void Exit()
        {
            Debug.Log("[GameState] Exited CutsceneState.");
            GameEvents.TriggerCutsceneFinished();
        }
    }
}
