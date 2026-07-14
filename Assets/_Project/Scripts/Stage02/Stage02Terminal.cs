using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheFusionEngineer.Missions;

namespace TheFusionEngineer.Stage02
{
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
        [SerializeField] private string promptMessage = "Press E to Execute";
        [SerializeField] private string lockedPromptMessage = "Complete Mission A First";
        [SerializeField] private HoldInteractionController holdInteraction;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;

        private MaterialPropertyBlock statusProperties;
        private bool isAvailable;
        private bool isCompleted;
        private bool wasInRange;

        public bool IsCompleted => isCompleted;

        private void Awake()
        {
            statusProperties = new MaterialPropertyBlock();
            SetStatusColor(new Color(0.9f, 0.04f, 0.03f), new Color(1f, 0.02f, 0.01f) * 2f);
            SetLinkedStatusColors(new Color(0.9f, 0.04f, 0.03f), new Color(1f, 0.02f, 0.01f) * 2f);
            SetPromptVisible(false);

            foreach (GameObject target in activateOnComplete)
            {
                target?.SetActive(false);
            }

            if (holdInteraction != null)
            {
                holdInteraction.Completed += Complete;
            }
        }

        private void OnDestroy()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed -= Complete;
            }
        }

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

        public void SetAvailable(bool available)
        {
            isAvailable = available && !isCompleted;
            wasInRange = false;
            SetPromptVisible(false);
            holdInteraction?.SetAvailable(isAvailable, lockedPromptMessage);
        }

        public void ConfigureHoldInteraction(HoldInteractionController interaction, string unavailablePrompt)
        {
            holdInteraction = interaction;
            lockedPromptMessage = unavailablePrompt;
            SetPromptVisible(false);
        }

        public void ConfigureInteractionPoints(Transform[] points)
        {
            interactionPoints = points ?? System.Array.Empty<Transform>();
        }

        public void ConfigureLinkedStatusIndicators(Renderer[] indicators)
        {
            linkedStatusIndicators = indicators ?? System.Array.Empty<Renderer>();
        }

        public void ConfigureActivationObjects(GameObject[] targets)
        {
            activateOnComplete = targets ?? System.Array.Empty<GameObject>();
        }

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
                target?.SetActive(true);
            }

            missionManager?.CompleteTerminal(this);
        }

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

        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.text = promptMessage;
                interactionPrompt.gameObject.SetActive(visible && isAvailable && !isCompleted);
            }
        }

        private void SetStatusColor(Color baseColor, Color emissionColor)
        {
            SetRendererColor(statusIndicator, baseColor, emissionColor);
        }

        private void SetLinkedStatusColors(Color baseColor, Color emissionColor)
        {
            foreach (Renderer indicator in linkedStatusIndicators)
            {
                SetRendererColor(indicator, baseColor, emissionColor);
            }
        }

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
