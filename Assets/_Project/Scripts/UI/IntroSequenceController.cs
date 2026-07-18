using System.Collections;
using TheFusionEngineer.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheFusionEngineer.UI
{
    /// <summary>
    /// Plays a scene-local career intro while temporarily locking player input.
    /// The component intentionally does not persist between scenes.
    /// </summary>
    public sealed class IntroSequenceController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private CanvasGroup textGroup;
        [SerializeField] private RectTransform textRoot;
        [SerializeField] private TMP_Text periodText;
        [SerializeField] private TMP_Text companyText;
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text storyText;

        [Header("Copy")]
        [SerializeField] private string period;
        [SerializeField] private string company;
        [SerializeField] private string role;
        [SerializeField, TextArea(2, 5)] private string story;

        [Header("Player Input")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private InputActionAsset inputActions;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.65f;
        [SerializeField, Min(0.1f)] private float holdDuration = 4f;
        [SerializeField, Min(0.1f)] private float fadeOutDuration = 0.85f;
        [SerializeField, Range(0.8f, 1f)] private float startScale = 0.94f;
        [SerializeField, Range(1f, 1.2f)] private float endScale = 1.03f;

        private Coroutine introRoutine;
        private InputAction interactAction;
        private bool movementWasEnabled;
        private bool interactorWasEnabled;
        private bool interactActionWasEnabled;
        private bool isPlaying;
        private bool hasCompleted;

        public bool IsPlaying => isPlaying;
        public bool HasCompleted => hasCompleted;

        // Unity가 첫 프레임 전에 게임 진행 상태를 초기화합니다.
        private void Start()
        {
            if (!hasCompleted)
            {
                introRoutine = StartCoroutine(PlayIntro());
            }
        }

        // Unity가 컴포넌트를 비활성화할 때 입력과 이벤트 연결을 정리합니다.
        private void OnDisable()
        {
            if (!isPlaying)
            {
                return;
            }

            if (introRoutine != null)
            {
                StopCoroutine(introRoutine);
                introRoutine = null;
            }

            FinishIntro(false);
        }

        // PlayIntro 관련 게임 로직을 수행합니다.
        private IEnumerator PlayIntro()
        {
            isPlaying = true;
            CaptureAndLockPlayerInput();
            ApplyCopy();

            if (fadePanel != null)
            {
                fadePanel.gameObject.SetActive(true);
                fadePanel.alpha = 1f;
                fadePanel.interactable = true;
                fadePanel.blocksRaycasts = true;
            }

            if (textGroup != null)
            {
                textGroup.alpha = 0f;
            }

            if (textRoot != null)
            {
                textRoot.localScale = Vector3.one * startScale;
            }

            float totalDuration = fadeInDuration + holdDuration + fadeOutDuration;
            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                if (WasSkipPressed())
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;

                float scaleProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / totalDuration));
                if (textRoot != null)
                {
                    textRoot.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, endScale, scaleProgress);
                }

                if (elapsed < fadeInDuration)
                {
                    float fadeIn = fadeInDuration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
                    if (textGroup != null)
                    {
                        textGroup.alpha = fadeIn;
                    }
                }
                else
                {
                    if (textGroup != null)
                    {
                        textGroup.alpha = 1f;
                    }

                    float fadeOutStart = fadeInDuration + holdDuration;
                    if (elapsed > fadeOutStart && fadePanel != null)
                    {
                        float fadeOut = Mathf.SmoothStep(0f, 1f, (elapsed - fadeOutStart) / fadeOutDuration);
                        fadePanel.alpha = 1f - fadeOut;
                    }
                }

                yield return null;
            }

            FinishIntro(true);
        }

        // WasSkipPressed 관련 게임 로직을 수행합니다.
        private static bool WasSkipPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null &&
                (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame);
        }

        // ApplyCopy 관련 게임 로직을 수행합니다.
        private void ApplyCopy()
        {
            if (periodText != null) periodText.text = period;
            if (companyText != null) companyText.text = company;
            if (roleText != null) roleText.text = role;
            if (storyText != null) storyText.text = story;
        }

        // CaptureAndLockPlayerInput 관련 게임 로직을 수행합니다.
        private void CaptureAndLockPlayerInput()
        {
            movementWasEnabled = playerMovement != null && playerMovement.enabled;
            interactorWasEnabled = playerInteractor != null && playerInteractor.enabled;

            interactAction = inputActions != null
                ? inputActions.FindAction("Player/Interact", false)
                : null;
            interactActionWasEnabled = interactAction != null && interactAction.enabled;

            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            if (playerInteractor != null)
            {
                playerInteractor.enabled = false;
            }

            interactAction?.Disable();
        }

        // RestorePlayerInput 관련 게임 로직을 수행합니다.
        private void RestorePlayerInput()
        {
            if (playerMovement != null)
            {
                playerMovement.enabled = movementWasEnabled;
            }

            if (playerInteractor != null)
            {
                playerInteractor.enabled = interactorWasEnabled;
            }

            if (interactActionWasEnabled)
            {
                interactAction?.Enable();
            }
            else
            {
                interactAction?.Disable();
            }
        }

        // FinishIntro 관련 게임 로직을 수행합니다.
        private void FinishIntro(bool completed)
        {
            RestorePlayerInput();

            if (fadePanel != null)
            {
                fadePanel.alpha = 0f;
                fadePanel.interactable = false;
                fadePanel.blocksRaycasts = false;
                fadePanel.gameObject.SetActive(false);
            }

            if (textGroup != null)
            {
                textGroup.alpha = 1f;
            }

            if (textRoot != null)
            {
                textRoot.localScale = Vector3.one;
            }

            isPlaying = false;
            hasCompleted = completed;
            introRoutine = null;
        }
    }
}
