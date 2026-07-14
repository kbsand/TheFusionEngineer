using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [SerializeField, Min(0.1f)] private float interactionDistance = 3f;

        private InputAction interactAction;
        private MaterialPropertyBlock visualProperties;
        private bool isUnlocked;
        private bool hasEntered;
        private bool wasInRange;

        public bool IsUnlocked => isUnlocked;
        public bool HasEntered => hasEntered;

        private void Awake()
        {
            interactAction = inputActions?.FindAction("Player/Interact", true);
            visualProperties = new MaterialPropertyBlock();
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
                SetPromptVisible(isInRange);
                wasInRange = isInRange;

                if (isInRange)
                {
                    guidance?.MarkPortalReached();
                }
            }

            if (isInRange && interactAction != null && interactAction.WasPressedThisFrame())
            {
                EnterStageTwo();
            }
        }

        public void UnlockPortal()
        {
            if (isUnlocked)
            {
                return;
            }

            isUnlocked = true;
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

            SetPortalColor(new Color(0.05f, 0.85f, 1f), new Color(0.05f, 0.85f, 1f) * 3f);
            Debug.Log("[Stage 1 Portal] Stage 2 portal unlocked.");
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
            unlockMessage = unlockedMessage;
            interactionDistance = distance;
        }

        public void ConfigureGuidance(PortalGuidanceController portalGuidance)
        {
            guidance = portalGuidance;
        }

        private void ApplyLockedState()
        {
            isUnlocked = false;
            hasEntered = false;
            wasInRange = false;

            if (interactionCollider != null)
            {
                interactionCollider.enabled = false;
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

        private void EnterStageTwo()
        {
            if (hasEntered)
            {
                return;
            }

            hasEntered = true;
            SetPromptVisible(false);
            StartCoroutine(ShowUnlockMessage());
            Debug.Log("[Stage 1 Portal] STAGE 2 UNLOCKED. Scene transition is not connected yet.");
        }

        private IEnumerator ShowUnlockMessage()
        {
            if (unlockMessage == null)
            {
                yield break;
            }

            unlockMessage.SetActive(true);
            yield return new WaitForSeconds(3f);
            unlockMessage.SetActive(false);
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt != null)
            {
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
