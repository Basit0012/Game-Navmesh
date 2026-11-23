using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RelicDash.Core;

namespace RelicDash.UI
{
    /// <summary>
    /// Screen-space HUD and UI manager.
    /// Manages HUD counters, cinematic letterbox bars, GameOver screen, and Victory screen.
    /// Reacts to Observer Pattern events.
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private TextMeshProUGUI hudRelicText;
        [SerializeField] private TextMeshProUGUI objectiveText;

        [Header("Cinematic Letterbox Bars")]
        [SerializeField] private GameObject letterboxBars;

        [Header("Game Over Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButtonGameOver;
        [SerializeField] private Button quitButtonGameOver;

        [Header("Victory Screen")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private TextMeshProUGUI victoryStatsText;
        [SerializeField] private Button playAgainButtonVictory;
        [SerializeField] private Button quitButtonVictory;

        private float _gameplayStartTime;

        private void Awake()
        {
            if (restartButtonGameOver != null)
                restartButtonGameOver.onClick.AddListener(OnRestartClicked);

            if (quitButtonGameOver != null)
                quitButtonGameOver.onClick.AddListener(OnQuitClicked);

            if (playAgainButtonVictory != null)
                playAgainButtonVictory.onClick.AddListener(OnRestartClicked);

            if (quitButtonVictory != null)
                quitButtonVictory.onClick.AddListener(OnQuitClicked);
        }

        private void Start()
        {
            _gameplayStartTime = Time.time;

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (letterboxBars != null) letterboxBars.SetActive(false);
            if (hudRoot != null) hudRoot.SetActive(true);
        }

        private void OnEnable()
        {
            GameEvents.OnRelicCountChanged += HandleRelicCountChanged;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnRelicCountChanged -= HandleRelicCountChanged;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleRelicCountChanged(int current, int total)
        {
            if (hudRelicText != null)
            {
                hudRelicText.text = $"RELICS: {current} / {total}";
            }

            if (objectiveText != null)
            {
                if (current < total)
                {
                    objectiveText.text = $"Explore the lost floating island and recover all {total} ancient energy relics.";
                }
                else
                {
                    objectiveText.text = "All relics gathered! Proceed through the unsealed Ancient Portal!";
                }
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    if (hudRoot != null) hudRoot.SetActive(true);
                    if (letterboxBars != null) letterboxBars.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (victoryPanel != null) victoryPanel.SetActive(false);
                    break;

                case GameState.Cutscene:
                    if (hudRoot != null) hudRoot.SetActive(false);
                    if (letterboxBars != null) letterboxBars.SetActive(true);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (victoryPanel != null) victoryPanel.SetActive(false);
                    break;

                case GameState.GameOver:
                    if (hudRoot != null) hudRoot.SetActive(false);
                    if (letterboxBars != null) letterboxBars.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(true);
                    if (victoryPanel != null) victoryPanel.SetActive(false);
                    break;

                case GameState.Victory:
                    if (hudRoot != null) hudRoot.SetActive(false);
                    if (letterboxBars != null) letterboxBars.SetActive(false);
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    if (victoryPanel != null)
                    {
                        victoryPanel.SetActive(true);
                        float elapsed = Time.time - _gameplayStartTime;
                        if (victoryStatsText != null)
                        {
                            victoryStatsText.text = $"Time: {elapsed:F1}s | Relics: 3 / 3";
                        }
                    }
                    break;
            }
        }

        private void OnRestartClicked()
        {
            GameEvents.TriggerRestartRequested();
        }

        private void OnQuitClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }
    }
}
