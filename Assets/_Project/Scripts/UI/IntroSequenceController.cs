using System.Collections;
using TheFusionEngineer.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TheFusionEngineer.UI
{
    public sealed class IntroSequenceController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private Text introText;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField, Min(0.1f)] private float holdDuration = 4f;
        [SerializeField, Min(0.1f)] private float fadeOutDuration = 1f;
        [SerializeField, Range(1f, 1.2f)] private float endScaleMultiplier = 1.1f;

        private const string IntroMessage = "2008–2015\nCorning Precision Materials\nControl & Automation Engineer";

        private bool isPlaying;
        private bool hasCompleted;

        public bool IsPlaying => isPlaying;
        public bool HasCompleted => hasCompleted;

        private void Start()
        {
            if (!hasCompleted)
            {
                StartCoroutine(PlayIntro());
            }
        }

        private void OnDisable()
        {
            if (isPlaying)
            {
                SetPlayerInputEnabled(true);
                isPlaying = false;
            }
        }

        public void Configure(
            CanvasGroup panel,
            Text sequenceText,
            PlayerMovement movement,
            PlayerInteractor interactor)
        {
            fadePanel = panel;
            introText = sequenceText;
            playerMovement = movement;
            playerInteractor = interactor;
        }

        private IEnumerator PlayIntro()
        {
            isPlaying = true;
            SetPlayerInputEnabled(false);

            RectTransform textTransform = introText != null ? introText.rectTransform : null;
            Vector3 startScale = textTransform != null ? textTransform.localScale : Vector3.one;
            Color startTextColor = introText != null ? introText.color : Color.white;

            if (introText != null)
            {
                introText.text = IntroMessage;
                introText.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, 1f);
            }

            if (fadePanel != null)
            {
                fadePanel.gameObject.SetActive(true);
                fadePanel.alpha = 1f;
                fadePanel.blocksRaycasts = true;
            }

            float totalDuration = holdDuration + fadeOutDuration;
            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float sequenceProgress = Mathf.Clamp01(elapsed / totalDuration);

                if (textTransform != null)
                {
                    textTransform.localScale = Vector3.LerpUnclamped(
                        startScale,
                        startScale * endScaleMultiplier,
                        sequenceProgress);
                }

                if (elapsed > holdDuration)
                {
                    float fadeProgress = Mathf.Clamp01((elapsed - holdDuration) / fadeOutDuration);
                    if (fadePanel != null)
                    {
                        fadePanel.alpha = 1f - fadeProgress;
                    }

                }

                yield return null;
            }

            if (fadePanel != null)
            {
                fadePanel.alpha = 0f;
                fadePanel.blocksRaycasts = false;
                fadePanel.gameObject.SetActive(false);
            }

            if (introText != null)
            {
                introText.text = string.Empty;
                introText.color = startTextColor;
            }

            if (textTransform != null)
            {
                textTransform.localScale = startScale;
            }

            SetPlayerInputEnabled(true);
            isPlaying = false;
            hasCompleted = true;
        }

        private void SetPlayerInputEnabled(bool enabled)
        {
            if (playerMovement != null)
            {
                playerMovement.enabled = enabled;
            }

            if (playerInteractor != null)
            {
                playerInteractor.enabled = enabled;
            }
        }
    }
}
