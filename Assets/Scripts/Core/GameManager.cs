using UnityEngine;
using UnityEngine.SceneManagement;
using RelicDash.Data;

namespace RelicDash.Core
{
    /// <summary>
    /// Singleton GameManager managing core game lifecycle via the State Design Pattern.
    /// Manages state transitions (Playing, Cutscene, GameOver, Victory) and coordinates with GameSettings.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameSettings gameSettings;

        [Header("Runtime State")]
        [SerializeField] private int relicsCollected = 0;
        [SerializeField] private GameState currentStateType = GameState.Playing;

        private IGameState _currentState;

        private PlayingState _playingState;
        private CutsceneState _cutsceneState;
        private GameOverState _gameOverState;
        private VictoryState _victoryState;

        // State instances with lazy instantiation ensuring availability in editor & playmode
        public PlayingState PlayingState => _playingState ??= new PlayingState(this);
        public CutsceneState CutsceneState => _cutsceneState ??= new CutsceneState(this);
        public GameOverState GameOverState => _gameOverState ??= new GameOverState(this);
        public VictoryState VictoryState => _victoryState ??= new VictoryState(this);

        public GameSettings Settings => gameSettings;
        public int RelicsCollected => relicsCollected;
        public GameState CurrentStateType => currentStateType;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Pre-warm state references
            _playingState = new PlayingState(this);
            _cutsceneState = new CutsceneState(this);
            _gameOverState = new GameOverState(this);
            _victoryState = new VictoryState(this);
        }

        private void OnEnable()
        {
            GameEvents.OnRelicCollected += HandleRelicCollected;
            GameEvents.OnPlayerCaught += HandlePlayerCaught;
            GameEvents.OnRestartRequested += RestartGame;
        }

        private void OnDisable()
        {
            GameEvents.OnRelicCollected -= HandleRelicCollected;
            GameEvents.OnPlayerCaught -= HandlePlayerCaught;
            GameEvents.OnRestartRequested -= RestartGame;
        }

        private void Start()
        {
            relicsCollected = 0;
            int target = gameSettings != null ? gameSettings.TargetRelicCount : 3;
            GameEvents.TriggerRelicCountChanged(relicsCollected, target);

            ChangeState(PlayingState);
        }

        private void Update()
        {
            _currentState?.Update();
        }

        /// <summary>
        /// Transitions the game into a new state, invoking Exit and Enter callbacks.
        /// </summary>
        public void ChangeState(IGameState newState)
        {
            if (newState == null) return;

            _currentState?.Exit();
            _currentState = newState;
            currentStateType = newState.StateType;
            _currentState.Enter();
        }

        private void HandleRelicCollected(GameObject relicObj)
        {
            relicsCollected++;
            int target = gameSettings != null ? gameSettings.TargetRelicCount : 3;
            
            Debug.Log($"[GameManager] Relic collected! Progress: {relicsCollected} / {target}");
            GameEvents.TriggerRelicCountChanged(relicsCollected, target);

            if (relicsCollected >= target)
            {
                Debug.Log("[GameManager] All ancient relics collected! Unsealing ancient gate...");
                GameEvents.TriggerAllRelicsCollected();
                ChangeState(CutsceneState);
            }
        }

        private void HandlePlayerCaught()
        {
            if (currentStateType != GameState.Playing) return;

            Debug.Log("[GameManager] Guardian intercepted player. Transitioning to GameOver.");
            ChangeState(GameOverState);
        }

        public void CompleteCutscene()
        {
            if (currentStateType == GameState.Cutscene)
            {
                ChangeState(VictoryState);
            }
        }

        public void RestartGame()
        {
            Debug.Log("[GameManager] Reloading scene...");
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting application.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
