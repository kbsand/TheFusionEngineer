using System.Collections;
using TheFusionEngineer.Missions;
using UnityEngine;

namespace TheFusionEngineer.Stage02
{
    /// <summary>
    /// 플레이어가 사다리 구역에 있을 때 수직 이동과 진입·이탈 상태를 처리합니다.
    /// </summary>
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

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed += BeginClimb;
            }
        }

        // 오브젝트가 제거될 때 남아 있는 이벤트와 임시 리소스를 정리합니다.
        private void OnDestroy()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed -= BeginClimb;
            }
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
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

        /// <summary>
        /// 선행 미션 결과에 따라 사다리 이동 가능 여부와 잠금 안내를 갱신합니다.
        /// </summary>
        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            holdInteraction?.SetAvailable(unlocked, "먼저 미션 A와 B를 완료하세요");
        }

        // BeginClimb 관련 게임 로직을 수행합니다.
        private void BeginClimb()
        {
            if (!isClimbing)
            {
                StartCoroutine(ClimbRoutine());
            }
        }

        // ClimbRoutine 관련 게임 로직을 수행합니다.
        private IEnumerator ClimbRoutine()
        {
            isClimbing = true;
            // Fade 관련 게임 로직을 수행합니다.
            yield return Fade(0f, 1f);

            if (player == null || destination == null)
            {
                Debug.LogError("[Stage02 Ladder] Player or destination reference is missing.", this);
                // Fade 관련 게임 로직을 수행합니다.
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
            // Fade 관련 게임 로직을 수행합니다.
            yield return Fade(1f, 0f);
            isClimbing = false;
            if (isUnlocked)
            {
                holdInteraction?.ResetForReuse();
            }
        }

        // Fade 관련 게임 로직을 수행합니다.
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
