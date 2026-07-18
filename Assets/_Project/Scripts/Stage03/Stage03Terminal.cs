using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheFusionEngineer.Missions;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Stage03
{
    /// <summary>
    /// Stage3 AI 단말기의 접근 감지와 길게 누르기 미션 완료를 처리합니다.
    /// </summary>
    public sealed class Stage03Terminal : MonoBehaviour
    {
        [SerializeField] private Stage03MissionManager missionManager;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform player;
        [SerializeField] private Text interactionPrompt;
        [SerializeField] private Renderer statusIndicator;
        [SerializeField] private Renderer[] linkedIndicators = System.Array.Empty<Renderer>();
        [SerializeField] private GameObject[] activateOnComplete = System.Array.Empty<GameObject>();
        [SerializeField] private string promptMessage = "E 키를 길게 눌러 실행하세요";
        [SerializeField] private string lockedPromptMessage = "먼저 미션 A를 완료하세요";
        [SerializeField] private HoldInteractionController holdInteraction;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.7f;

        private MaterialPropertyBlock visualProperties;
        private bool isAvailable;
        private bool isCompleted;

        public bool IsCompleted => isCompleted;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            visualProperties = new MaterialPropertyBlock();
            SetIndicators(new Color(0.9f, 0.03f, 0.04f), new Color(1f, 0.01f, 0.03f) * 2f);
            SetPromptVisible(false);

            foreach (GameObject target in activateOnComplete)
            {
                // Unity의 Inspector 미할당 참조는 ?. 연산자로 안전하게 걸러지지 않을 수 있습니다.
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
            Stage03MissionManager manager,
            InputActionAsset actions,
            Transform playerTransform,
            Text prompt,
            Renderer indicator,
            Renderer[] relatedIndicators,
            GameObject[] completionObjects,
            string promptText,
            float distance)
        {
            missionManager = manager;
            inputActions = actions;
            player = playerTransform;
            interactionPrompt = prompt;
            statusIndicator = indicator;
            linkedIndicators = relatedIndicators ?? System.Array.Empty<Renderer>();
            activateOnComplete = completionObjects ?? System.Array.Empty<GameObject>();
            promptMessage = promptText;
            interactionDistance = distance;
        }

        /// <summary>
        /// 미션 순서에 따라 이 AI 단말기의 사용 가능 상태와 잠금 안내를 갱신합니다.
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
            SetIndicators(new Color(0.03f, 0.9f, 0.3f), new Color(0.02f, 1f, 0.45f) * 2f);

            foreach (GameObject target in activateOnComplete)
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            }

            missionManager?.CompleteTerminal(this);
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
        private void SetIndicators(Color baseColor, Color emissionColor)
        {
            SetRendererColor(statusIndicator, baseColor, emissionColor);
            foreach (Renderer indicator in linkedIndicators)
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

            target.GetPropertyBlock(visualProperties);
            visualProperties.SetColor("_BaseColor", baseColor);
            visualProperties.SetColor("_Color", baseColor);
            visualProperties.SetColor("_EmissionColor", emissionColor);
            target.SetPropertyBlock(visualProperties);
        }
    }
}
