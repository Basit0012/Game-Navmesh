using UnityEngine;
using TMPro;
using RelicDash.Core;

namespace RelicDash.UI
{
    /// <summary>
    /// World-Space Canvas UI component attached to the Player character.
    /// Subscribes to GameEvents.OnRelicCountChanged (Observer Pattern)
    /// and dynamically displays current collected relics count with punch animation.
    /// Fulfills examination requirement #6 and #9.
    /// </summary>
    public class PlayerWorldUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI relicCounterText;
        [SerializeField] private Transform scalePunchTarget;

        [Header("Styling & Animation")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.9f, 1f, 1f);
        [SerializeField] private Color completeColor = new Color(0.2f, 1f, 0.4f, 1f);
        [SerializeField] private float punchScale = 1.35f;
        [SerializeField] private float punchSpeed = 6.0f;

        private Vector3 _originalScale = Vector3.one;
        private float _currentPunch = 1.0f;

        private void Awake()
        {
            if (scalePunchTarget == null)
            {
                scalePunchTarget = transform;
            }
            _originalScale = scalePunchTarget.localScale;
        }

        private void OnEnable()
        {
            // Subscribe to Observer pattern event
            GameEvents.OnRelicCountChanged += HandleRelicCountChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            GameEvents.OnRelicCountChanged -= HandleRelicCountChanged;
        }

        private void Update()
        {
            if (_currentPunch > 1.0f)
            {
                _currentPunch = Mathf.MoveTowards(_currentPunch, 1.0f, punchSpeed * Time.deltaTime);
                if (scalePunchTarget != null)
                {
                    scalePunchTarget.localScale = _originalScale * _currentPunch;
                }
            }
        }

        private void HandleRelicCountChanged(int current, int total)
        {
            if (relicCounterText != null)
            {
                relicCounterText.text = $"RELICS: {current} / {total}";
                relicCounterText.color = (current >= total) ? completeColor : normalColor;
            }

            // Punch scale feedback
            _currentPunch = punchScale;
        }
    }
}
