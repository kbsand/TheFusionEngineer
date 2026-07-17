using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheFusionEngineer.Missions;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Stage03
{
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
        private bool wasInRange;

        public bool IsCompleted => isCompleted;

        private void Awake()
        {
            visualProperties = new MaterialPropertyBlock();
            SetIndicators(new Color(0.9f, 0.03f, 0.04f), new Color(1f, 0.01f, 0.03f) * 2f);
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
                target?.SetActive(true);
            }

            missionManager?.CompleteTerminal(this);
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.text = MobileWebGLControls.ResolveInteractionPrompt(promptMessage);
                interactionPrompt.gameObject.SetActive(visible && isAvailable && !isCompleted);
            }
        }

        private void SetIndicators(Color baseColor, Color emissionColor)
        {
            SetRendererColor(statusIndicator, baseColor, emissionColor);
            foreach (Renderer indicator in linkedIndicators)
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

            target.GetPropertyBlock(visualProperties);
            visualProperties.SetColor("_BaseColor", baseColor);
            visualProperties.SetColor("_Color", baseColor);
            visualProperties.SetColor("_EmissionColor", emissionColor);
            target.SetPropertyBlock(visualProperties);
        }
    }
}
