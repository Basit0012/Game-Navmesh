using UnityEngine;
using UnityEngine.Playables;
using RelicDash.Core;
using RelicDash.Environment;

namespace RelicDash.Cinematics
{
    /// <summary>
    /// Controls the 5-second cinematic Unity Timeline sequence triggered when all 3 relics are collected.
    /// Drives the Exit Gate opening animation and Cinemachine camera pan toward the exit gate.
    /// Fulfills examination requirement #12.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineVictoryController : MonoBehaviour
    {
        [Header("Cinematic Components")]
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private ExitGate exitGate;
        [SerializeField] private float fallbackDuration = 5.0f;

        [Header("Audio")]
        [SerializeField] private AudioSource cutsceneAudio;

        private bool _hasPlayed = false;

        private void Awake()
        {
            if (playableDirector == null)
            {
                playableDirector = GetComponent<PlayableDirector>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnAllRelicsCollected += TriggerVictoryCinematic;

            if (playableDirector != null)
            {
                playableDirector.stopped += OnTimelineStopped;
            }
        }

        private void OnDisable()
        {
            GameEvents.OnAllRelicsCollected -= TriggerVictoryCinematic;

            if (playableDirector != null)
            {
                playableDirector.stopped -= OnTimelineStopped;
            }
        }

        /// <summary>
        /// Automatically triggered when 3 relics are collected.
        /// </summary>
        [ContextMenu("Trigger Victory Sequence")]
        public void TriggerVictoryCinematic()
        {
            if (_hasPlayed) return;
            _hasPlayed = true;

            Debug.Log("[TimelineVictoryController] Initiating 5-second Victory Cinematic sequence...");

            // Transition game state to Cutscene
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameManager.Instance.CutsceneState);
            }

            // Open exit gate
            if (exitGate != null)
            {
                exitGate.OpenGate();
            }

            // Play timeline sequence
            if (playableDirector != null && playableDirector.playableAsset != null)
            {
                playableDirector.time = 0;
                playableDirector.Play();
            }
            else
            {
                // Fallback timer if playable asset isn't assigned
                Invoke(nameof(FallbackSequenceEnd), fallbackDuration);
            }

            if (cutsceneAudio != null)
            {
                cutsceneAudio.Play();
            }
        }

        private void OnTimelineStopped(PlayableDirector director)
        {
            Debug.Log("[TimelineVictoryController] Victory Timeline sequence finished.");
            ConcludeCutscene();
        }

        private void FallbackSequenceEnd()
        {
            Debug.Log("[TimelineVictoryController] Fallback cutscene timer completed.");
            ConcludeCutscene();
        }

        private void ConcludeCutscene()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteCutscene();
            }
        }
    }
}
