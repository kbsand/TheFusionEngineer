using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheFusionEngineer.Missions;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Stage02
{
    /// <summary>
    /// Stage2 미션 단말기의 플레이어 접근 감지와 길게 누르기 완료 처리를 담당합니다.
    /// </summary>
    public sealed class Stage02Terminal : MonoBehaviour
    {
        [SerializeField] private Stage02MissionManager missionManager;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform player;
        [SerializeField] private Text interactionPrompt;
        [SerializeField] private Renderer statusIndicator;
        [SerializeField] private Transform[] interactionPoints = System.Array.Empty<Transform>();
        [SerializeField] private Renderer[] linkedStatusIndicators = System.Array.Empty<Renderer>();
        [SerializeField] private GameObject[] activateOnComplete = System.Array.Empty<GameObject>();
        [SerializeField] private string promptMessage = "E 키를 길게 눌러 실행하세요";
        [SerializeField] private string lockedPromptMessage = "먼저 미션 A를 완료하세요";
        [SerializeField] private HoldInteractionController holdInteraction;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;

        private MaterialPropertyBlock statusProperties;
        private bool isAvailable;
        private bool isCompleted;

        public bool IsCompleted => isCompleted;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            statusProperties = new MaterialPropertyBlock();
            SetStatusColor(new Color(0.9f, 0.04f, 0.03f), new Color(1f, 0.02f, 0.01f) * 2f);
            SetLinkedStatusColors(new Color(0.9f, 0.04f, 0.03f), new Color(1f, 0.02f, 0.01f) * 2f);
            SetPromptVisible(false);

            foreach (GameObject target in activateOnComplete)
            {
                if (target != null)
                {
                    target.SetActive(false);
                }
            }

            if (holdInteraction != null)
            {
                holdInteraction.Completed += Complete;
            }
        }

        // 오브젝트가 제거될 때 남아 있는 이벤트와 임시 리소스를 정리합니다.
        private void OnDestroy()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed -= Complete;
            }
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(
            Stage02MissionManager manager,
            InputActionAsset actions,
            Transform playerTransform,
            Text prompt,
            Renderer indicator,
            GameObject[] completionObjects,
            string promptText,
            float distance)
        {
            missionManager = manager;
            inputActions = actions;
            player = playerTransform;
            interactionPrompt = prompt;
            statusIndicator = indicator;
            activateOnComplete = completionObjects ?? System.Array.Empty<GameObject>();
            promptMessage = promptText;
            interactionDistance = distance;
        }

        /// <summary>
        /// 미션 순서에 따라 이 단말기의 사용 가능 상태와 잠금 안내를 갱신합니다.
        /// </summary>
        public void SetAvailable(bool available)
        {
            isAvailable = available && !isCompleted;
            SetPromptVisible(false);
            holdInteraction?.SetAvailable(isAvailable, lockedPromptMessage);
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureHoldInteraction(HoldInteractionController interaction, string unavailablePrompt)
        {
            holdInteraction = interaction;
            lockedPromptMessage = unavailablePrompt;
            SetPromptVisible(false);
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureInteractionPoints(Transform[] points)
        {
            interactionPoints = points ?? System.Array.Empty<Transform>();
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureLinkedStatusIndicators(Renderer[] indicators)
        {
            linkedStatusIndicators = indicators ?? System.Array.Empty<Renderer>();
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureActivationObjects(GameObject[] targets)
        {
            activateOnComplete = targets ?? System.Array.Empty<GameObject>();
        }

        // Complete 관련 게임 로직을 수행합니다.
        private void Complete()
        {
            if (!isAvailable || isCompleted)
            {
                return;
            }

            isCompleted = true;
            isAvailable = false;
            holdInteraction?.SetAvailable(false, lockedPromptMessage);
            SetPromptVisible(false);
            SetStatusColor(new Color(0.04f, 0.9f, 0.22f), new Color(0.04f, 1f, 0.25f) * 2f);
            SetLinkedStatusColors(new Color(0.04f, 0.9f, 0.22f), new Color(0.04f, 1f, 0.25f) * 2f);

            foreach (GameObject target in activateOnComplete)
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            }

            missionManager?.CompleteTerminal(this);
        }

        // 필요한 실행 조건을 검사하고 조건을 만족할 때만 동작을 수행합니다.
        private bool IsPlayerInRange()
        {
            float maximumSqrDistance = interactionDistance * interactionDistance;

            if (interactionPoints.Length == 0)
            {
                Vector3 terminalOffset = transform.position - player.position;
                terminalOffset.y = 0f;
                return terminalOffset.sqrMagnitude <= maximumSqrDistance;
            }

            foreach (Transform point in interactionPoints)
            {
                if (point == null)
                {
                    continue;
                }

                Vector3 offset = point.position - player.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= maximumSqrDistance)
                {
                    return true;
                }
            }

            return false;
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.text = MobileWebGLControls.ResolveInteractionPrompt(promptMessage);
                interactionPrompt.gameObject.SetActive(visible && isAvailable && !isCompleted);
            }
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetStatusColor(Color baseColor, Color emissionColor)
        {
            SetRendererColor(statusIndicator, baseColor, emissionColor);
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetLinkedStatusColors(Color baseColor, Color emissionColor)
        {
            foreach (Renderer indicator in linkedStatusIndicators)
            {
                SetRendererColor(indicator, baseColor, emissionColor);
            }
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetRendererColor(Renderer target, Color baseColor, Color emissionColor)
        {
            if (target == null)
            {
                return;
            }

            target.GetPropertyBlock(statusProperties);
            statusProperties.SetColor("_BaseColor", baseColor);
            statusProperties.SetColor("_Color", baseColor);
            statusProperties.SetColor("_EmissionColor", emissionColor);
            target.SetPropertyBlock(statusProperties);
        }
    }
}
