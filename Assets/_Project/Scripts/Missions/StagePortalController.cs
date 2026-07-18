using System.Collections;
using TheFusionEngineer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Missions
{
    /// <summary>
    /// 스테이지 미션 완료 여부를 확인한 뒤 길게 누르기 입력으로 다음 씬에 진입시킵니다.
    /// </summary>
    public sealed class StagePortalController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform player;
        [SerializeField] private Renderer[] portalRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private Light portalLight;
        [SerializeField] private Text careerLabel;
        [SerializeField] private Text interactionPrompt;
        [SerializeField] private GameObject unlockMessage;
        [SerializeField] private PortalGuidanceController guidance;
        [SerializeField] private HoldInteractionController holdInteraction;
        [SerializeField] private SceneTransitionController sceneTransition;
        [SerializeField] private string targetSceneName = "Stage02_Architect";
        [SerializeField] private Color unlockedBaseColor = new(0.05f, 0.85f, 1f);
        [SerializeField] private Color unlockedEmissionColor = new(0.15f, 2.55f, 3f);
        [SerializeField, Min(0f)] private float unlockMessageDuration = 0.35f;
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
        [SerializeField] private AudioClip enterPortalClip;
        [SerializeField, Range(0f, 1f)] private float enterPortalVolume = 0.85f;

        private InputAction interactAction;
        private MaterialPropertyBlock visualProperties;
        private bool isUnlocked;
        private bool hasEntered;
        private bool wasInRange;
        private string interactionPromptText;

        public bool IsUnlocked => isUnlocked;
        public bool HasEntered => hasEntered;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            if (enterPortalClip == null)
            {
                enterPortalClip = GameSfxLibrary.LoadPortalEnter();
            }
            interactAction = inputActions?.FindAction("Player/Interact", true);
            visualProperties = new MaterialPropertyBlock();
            sceneTransition ??= FindAnyObjectByType<SceneTransitionController>();
            interactionPromptText = interactionPrompt != null ? interactionPrompt.text : string.Empty;
            if (holdInteraction != null)
            {
                holdInteraction.Completed += EnterPortal;
            }
            ApplyLockedState();
        }

        // Unity가 컴포넌트를 활성화할 때 입력과 이벤트 연결을 시작합니다.
        private void OnEnable()
        {
            interactAction?.Enable();
        }

        // Unity가 컴포넌트를 비활성화할 때 입력과 이벤트 연결을 정리합니다.
        private void OnDisable()
        {
            interactAction?.Disable();
            SetPromptVisible(false);
        }

        // 오브젝트가 제거될 때 남아 있는 이벤트와 임시 리소스를 정리합니다.
        private void OnDestroy()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed -= EnterPortal;
            }
        }

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
        private void Update()
        {
            if (!isUnlocked || hasEntered)
            {
                return;
            }

            if (player == null)
            {
                return;
            }

            Vector3 offset = transform.position - player.position;
            offset.y = 0f;
            bool isInRange = offset.sqrMagnitude <= interactionDistance * interactionDistance;

            if (isInRange != wasInRange)
            {
                if (holdInteraction == null)
                {
                    SetPromptVisible(isInRange);
                }
                wasInRange = isInRange;

                if (isInRange)
                {
                }
            }

            if (holdInteraction == null && isInRange && interactAction != null && interactAction.WasPressedThisFrame())
            {
                EnterPortal();
            }
        }

        /// <summary>
        /// 필수 미션 완료 후 포탈 사용, 안내 화살표, 시각 효과를 활성화합니다.
        /// </summary>
        public void UnlockPortal()
        {
            if (isUnlocked)
            {
                return;
            }

            isUnlocked = true;
            holdInteraction?.SetAvailable(true);
            if (interactionCollider != null)
            {
                interactionCollider.enabled = true;
            }

            if (portalLight != null)
            {
                portalLight.enabled = true;
            }

            if (careerLabel != null)
            {
                careerLabel.gameObject.SetActive(true);
            }

            guidance?.ShowGuidance();

            SetPortalColor(unlockedBaseColor, unlockedEmissionColor);
            Debug.Log($"[Stage Portal] Portal unlocked for {targetSceneName}.");
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(
            InputActionAsset actions,
            Transform playerTransform,
            Renderer[] renderers,
            Collider portalCollider,
            Light activationLight,
            Text label,
            Text prompt,
            GameObject unlockedMessage,
            float distance)
        {
            inputActions = actions;
            player = playerTransform;
            portalRenderers = renderers ?? System.Array.Empty<Renderer>();
            interactionCollider = portalCollider;
            portalLight = activationLight;
            careerLabel = label;
            interactionPrompt = prompt;
            interactionPromptText = prompt != null ? prompt.text : string.Empty;
            unlockMessage = unlockedMessage;
            interactionDistance = distance;
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureGuidance(PortalGuidanceController portalGuidance)
        {
            guidance = portalGuidance;
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureHoldInteraction(HoldInteractionController interaction)
        {
            holdInteraction = interaction;
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureTransition(
            SceneTransitionController transition,
            string sceneName,
            Color baseColor,
            Color emissionColor,
            float messageDuration = 0.35f)
        {
            sceneTransition = transition;
            targetSceneName = sceneName;
            unlockedBaseColor = baseColor;
            unlockedEmissionColor = emissionColor;
            unlockMessageDuration = Mathf.Max(0f, messageDuration);
        }

        // ApplyLockedState 관련 게임 로직을 수행합니다.
        private void ApplyLockedState()
        {
            isUnlocked = false;
            hasEntered = false;
            wasInRange = false;
            holdInteraction?.SetAvailable(false, "먼저 CAREER CORE를 획득하세요");

            if (interactionCollider != null)
            {
                interactionCollider.enabled = holdInteraction != null;
            }

            if (portalLight != null)
            {
                portalLight.enabled = false;
            }

            if (careerLabel != null)
            {
                careerLabel.gameObject.SetActive(false);
            }

            if (unlockMessage != null)
            {
                unlockMessage.SetActive(false);
            }

            SetPromptVisible(false);
            SetPortalColor(new Color(0.035f, 0.06f, 0.08f), Color.black);
        }

        // EnterPortal 관련 게임 로직을 수행합니다.
        private void EnterPortal()
        {
            if (hasEntered)
            {
                return;
            }

            hasEntered = true;
            guidance?.MarkPortalReached(); // 실제 포탈 진입 시 화살표 제거
            PersistentSfxPlayer.Play(enterPortalClip, enterPortalVolume);
            SetPromptVisible(false);
            StartCoroutine(EnterPortalRoutine());
        }

        // EnterPortalRoutine 관련 게임 로직을 수행합니다.
        private IEnumerator EnterPortalRoutine()
        {
            if (unlockMessage != null)
            {
                unlockMessage.SetActive(true);
            }

            if (unlockMessageDuration > 0f)
            {
                // WaitForSecondsRealtime 관련 게임 로직을 수행합니다.
                yield return new WaitForSecondsRealtime(unlockMessageDuration);
            }

            if (unlockMessage != null)
            {
                unlockMessage.SetActive(false);
            }

            sceneTransition ??= FindAnyObjectByType<SceneTransitionController>();
            if (sceneTransition == null)
            {
                hasEntered = false;
                Debug.LogError($"[Stage Portal] SceneTransitionController is missing. Cannot load '{targetSceneName}'.", this);
                yield break;
            }

            sceneTransition.LoadScene(targetSceneName);
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetPromptVisible(bool visible)
        {
            if (holdInteraction != null)
            {
                return;
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.text =
                    MobileWebGLControls.ResolveInteractionPrompt(interactionPromptText);
                interactionPrompt.gameObject.SetActive(visible && isUnlocked && !hasEntered);
            }
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetPortalColor(Color baseColor, Color emissionColor)
        {
            foreach (Renderer portalRenderer in portalRenderers)
            {
                if (portalRenderer == null)
                {
                    continue;
                }

                portalRenderer.GetPropertyBlock(visualProperties);
                visualProperties.SetColor("_BaseColor", baseColor);
                visualProperties.SetColor("_Color", baseColor);
                visualProperties.SetColor("_EmissionColor", emissionColor);
                portalRenderer.SetPropertyBlock(visualProperties);
            }
        }
    }
}
