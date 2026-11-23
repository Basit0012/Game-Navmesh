using System;
using UnityEngine;

namespace RelicDash.Core
{
    /// <summary>
    /// Supported core game states managed by the State Pattern.
    /// </summary>
    public enum GameState
    {
        Playing,
        Cutscene,
        GameOver,
        Victory
    }

    /// <summary>
    /// Central event bus implementing the Observer Pattern.
    /// Decouples publishers (Relic, Enemy, Timeline) from subscribers (UI, Audio, Managers).
    /// </summary>
    public static class GameEvents
    {
        // Relic Events
        public static event Action<int, int> OnRelicCountChanged; // currentCount, targetCount
        public static event Action<GameObject> OnRelicCollected;   // relic GameObject
        public static event Action OnAllRelicsCollected;

        // State & Lifecycle Events
        public static event Action<GameState> OnGameStateChanged;
        public static event Action OnPlayerCaught;
        public static event Action OnVictoryAchieved;
        public static event Action OnCutsceneStarted;
        public static event Action OnCutsceneFinished;
        public static event Action OnRestartRequested;

        // Invocation dispatchers
        public static void TriggerRelicCountChanged(int current, int target)
        {
            OnRelicCountChanged?.Invoke(current, target);
        }

        public static void TriggerRelicCollected(GameObject relicObj)
        {
            OnRelicCollected?.Invoke(relicObj);
        }

        public static void TriggerAllRelicsCollected()
        {
            OnAllRelicsCollected?.Invoke();
        }

        public static void TriggerGameStateChanged(GameState newState)
        {
            OnGameStateChanged?.Invoke(newState);
        }

        public static void TriggerPlayerCaught()
        {
            OnPlayerCaught?.Invoke();
        }

        public static void TriggerVictoryAchieved()
        {
            OnVictoryAchieved?.Invoke();
        }

        public static void TriggerCutsceneStarted()
        {
            OnCutsceneStarted?.Invoke();
        }

        public static void TriggerCutsceneFinished()
        {
            OnCutsceneFinished?.Invoke();
        }

        public static void TriggerRestartRequested()
        {
            OnRestartRequested?.Invoke();
        }
    }
}
