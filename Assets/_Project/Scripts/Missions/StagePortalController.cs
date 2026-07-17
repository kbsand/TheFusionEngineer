using System.Collections;
using TheFusionEngineer.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Missions
{
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

        private void Awake()
        {
            if (enterPortalClip == null)
            {
                enterPortalClip = GameSfxLibrary.LoadPortalEnter();
            }
            interactAction = inputActions?.FindAction("Player/Interact", true);
            visualProperties = new MaterialPropertyBlock();
            sceneTransition ??= FindFirstObjectByType<SceneTransitionController>();
            interactionPromptText = interactionPrompt != null ? interactionPrompt.text : string.Empty;
            if (holdInteraction != null)
            {
                holdInteraction.Completed += EnterPortal;
            }
            ApplyLockedState();
        }

        private void OnEnable()
        {
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
            SetPromptVisible(false);
        }

        private void OnDestroy()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed -= EnterPortal;
            }
        }

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
                    // guidance?.MarkPortalReached();    가까이 가도 사라지지 않도록
                }
            }

            if (holdInteraction == null && isInRange && interactAction != null && interactAction.WasPressedThisFrame())
            {
                EnterPortal();
            }
        }

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

        public void ConfigureGuidance(PortalGuidanceController portalGuidance)
        {
            guidance = portalGuidance;
        }

        public void ConfigureHoldInteraction(HoldInteractionController interaction)
        {
            holdInteraction = interaction;
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
        }

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

        private IEnumerator EnterPortalRoutine()
        {
            if (unlockMessage != null)
            {
                unlockMessage.SetActive(true);
            }

            if (unlockMessageDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(unlockMessageDuration);
            }

            if (unlockMessage != null)
            {
                unlockMessage.SetActive(false);
            }

            sceneTransition ??= FindFirstObjectByType<SceneTransitionController>();
            if (sceneTransition == null)
            {
                hasEntered = false;
                Debug.LogError($"[Stage Portal] SceneTransitionController is missing. Cannot load '{targetSceneName}'.", this);
                yield break;
            }

            sceneTransition.LoadScene(targetSceneName);
        }

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
