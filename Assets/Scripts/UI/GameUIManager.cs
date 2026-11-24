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

        [Header("Psychological Horror Overlay")]
        [SerializeField] private Image fearVignetteOverlay;

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

        private void Update()
        {
            if (fearVignetteOverlay != null && Horror.SanityAndFearSystem.Instance != null)
            {
                float fear = Horror.SanityAndFearSystem.Instance.CurrentFear;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (2f + fear * 8f));
                float alpha = Mathf.Lerp(0f, 0.65f, fear) + (fear > 0.4f ? pulse * 0.15f : 0f);
                Color c = fearVignetteOverlay.color;
                c.a = Mathf.Clamp01(alpha);
                fearVignetteOverlay.color = c;
            }
        }

        private void HandleRelicCountChanged(int current, int total)
        {
            if (hudRelicText != null)
            {
                hudRelicText.text = $"CURSED CORES: {current} / {total}";
            }

            if (objectiveText != null)
            {
                if (current < total)
                {
                    objectiveText.text = $"Escape the Hollow Loop: Locate and claim all {total} anomaly cores. Beware the Stalker.";
                }
                else
                {
                    objectiveText.text = "All cores bound! The Loop Fracture Gate is unsealed. Escape into the rift!";
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
