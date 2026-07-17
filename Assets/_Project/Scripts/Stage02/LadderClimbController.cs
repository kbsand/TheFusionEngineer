using System.Collections;
using TheFusionEngineer.Missions;
using UnityEngine;

namespace TheFusionEngineer.Stage02
{
    public sealed class LadderClimbController : MonoBehaviour
    {
        [SerializeField] private HoldInteractionController holdInteraction;
        [SerializeField] private Transform player;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform destination;
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.25f;

        private bool isClimbing;
        private bool isUnlocked;

        private void Awake()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed += BeginClimb;
            }
        }

        private void OnDestroy()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed -= BeginClimb;
            }
        }

        public void Configure(
            HoldInteractionController interaction,
            Transform playerTransform,
            CharacterController controller,
            Transform climbDestination,
            CanvasGroup blackFadePanel,
            float duration)
        {
            holdInteraction = interaction;
            player = playerTransform;
            characterController = controller;
            destination = climbDestination;
            fadePanel = blackFadePanel;
            fadeDuration = Mathf.Max(0.05f, duration);
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            holdInteraction?.SetAvailable(unlocked, "먼저 미션 A와 B를 완료하세요");
        }

        private void BeginClimb()
        {
            if (!isClimbing)
            {
                StartCoroutine(ClimbRoutine());
            }
        }

        private IEnumerator ClimbRoutine()
        {
            isClimbing = true;
            yield return Fade(0f, 1f);

            if (player == null || destination == null)
            {
                Debug.LogError("[Stage02 Ladder] Player or destination reference is missing.", this);
                yield return Fade(1f, 0f);
                isClimbing = false;
                if (isUnlocked)
                {
                    holdInteraction?.ResetForReuse();
                }
                yield break;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            player.SetPositionAndRotation(destination.position, destination.rotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            yield return null;
            yield return Fade(1f, 0f);
            isClimbing = false;
            if (isUnlocked)
            {
                holdInteraction?.ResetForReuse();
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadePanel == null)
            {
                yield break;
            }

            fadePanel.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            fadePanel.alpha = to;
            fadePanel.blocksRaycasts = to > 0f;
        }
    }
}
