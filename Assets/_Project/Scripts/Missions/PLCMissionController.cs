using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TheFusionEngineer.Core;
using TheFusionEngineer.Player;

namespace TheFusionEngineer.Missions
{
    public sealed class PLCMissionController : MonoBehaviour
    {
        [SerializeField] private ConveyorController conveyor;
        [SerializeField] private Renderer warningIndicator;
        [SerializeField] private Text interactionPrompt;
        [SerializeField] private Text careerCoreText;
        [SerializeField] private GameObject completionMessage;
        [SerializeField] private StagePortalController stagePortal;
        [SerializeField] private Light plcFaultLight;
        [SerializeField] private HoldInteractionController holdInteraction;
        [SerializeField] private AudioClip stageCompleteClip;
        [SerializeField, Range(0f, 1f)] private float completionVolume = 0.85f;

        [Header("Mission Guidance")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private string movementGuidanceMessage = "방향키를 조작하여 움직여 보세요.";
        [SerializeField] private string guidanceMessage = "PLC를 조작하세요.";
        [FormerlySerializedAs("guidanceDelay")]
        [SerializeField, Min(0f)] private float movementInactivityDelay = 2f;

        private bool isCompleted;
        private MaterialPropertyBlock warningProperties;
        private GameObject guidanceRoot;
        private Text guidanceLabel;
        private Coroutine guidanceRoutine;
        private bool movementInputConfirmed;

        public bool IsCompleted => isCompleted;

        private void Awake()
        {
            if (stageCompleteClip == null)
            {
                stageCompleteClip = GameSfxLibrary.LoadStageComplete();
            }
            warningProperties = new MaterialPropertyBlock();
            SetWarningColor(new Color(0.9f, 0.04f, 0.03f));
            SetFaultLightColor(new Color(1f, 0.05f, 0.03f));
            SetPlayerInRange(false);

            if (completionMessage != null)
            {
                completionMessage.SetActive(false);
            }

            if (holdInteraction != null)
            {
                holdInteraction.Completed += TryCompleteMission;
            }
        }

        private void Start()
        {
            if (playerMovement == null)
            {
                playerMovement = FindAnyObjectByType<PlayerMovement>();
            }
            if (playerMovement != null)
            {
                playerMovement.MovementInputDetected += HandleMovementInputDetected;
            }

            guidanceRoutine = StartCoroutine(ShowMovementGuidanceAfterInactivity());
        }

        private void OnDestroy()
        {
            if (holdInteraction != null)
            {
                holdInteraction.Completed -= TryCompleteMission;
            }

            if (playerMovement != null)
            {
                playerMovement.MovementInputDetected -= HandleMovementInputDetected;
            }

            HideGuidance();
        }

        public void SetPlayerInRange(bool inRange)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(inRange && !isCompleted);
            }
        }

        public void TryCompleteMission()
        {
            if (isCompleted)
            {
                return;
            }

            isCompleted = true;
            HideGuidance();
            holdInteraction?.SetAvailable(false);
            SetPlayerInRange(false);
            SetWarningColor(new Color(0.05f, 0.9f, 0.2f));
            SetFaultLightColor(new Color(0.05f, 0.9f, 0.2f));
            conveyor?.StartConveyor();
            stagePortal?.UnlockPortal();
            PersistentSfxPlayer.Play(stageCompleteClip, completionVolume);

            if (careerCoreText != null)
            {
                careerCoreText.text = "CAREER CORE 01: ACQUIRED";
                careerCoreText.color = new Color(0.35f, 1f, 0.45f);
            }

            if (completionMessage != null)
            {
                StartCoroutine(ShowCompletionMessage());
            }

            Debug.Log("[Stage 1 Mission Complete] PLC bottleneck fixed. CL17 line restored. Career Core 01 acquired.");
        }

        public void Configure(
            ConveyorController conveyorController,
            Renderer indicator,
            Text prompt,
            Text coreText,
            GameObject completedMessage)
        {
            conveyor = conveyorController;
            warningIndicator = indicator;
            interactionPrompt = prompt;
            careerCoreText = coreText;
            completionMessage = completedMessage;
        }

        public void ConfigureStageExit(StagePortalController portal, Light faultLight)
        {
            stagePortal = portal;
            plcFaultLight = faultLight;
        }

        public void ConfigureHoldInteraction(HoldInteractionController interaction)
        {
            holdInteraction = interaction;
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
        }

        private IEnumerator ShowCompletionMessage()
        {
            completionMessage.SetActive(true);
            yield return new WaitForSeconds(3f);
            completionMessage.SetActive(false);
        }

        private IEnumerator ShowMovementGuidanceAfterInactivity()
        {
            while (playerMovement != null && !playerMovement.isActiveAndEnabled && !isCompleted)
            {
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < movementInactivityDelay && !movementInputConfirmed && !isCompleted)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            guidanceRoutine = null;
            if (!movementInputConfirmed && !isCompleted)
            {
                CreateGuidanceUI(movementGuidanceMessage);
            }
        }

        private void HandleMovementInputDetected()
        {
            if (movementInputConfirmed || isCompleted)
            {
                return;
            }

            movementInputConfirmed = true;
            if (guidanceRoutine != null)
            {
                StopCoroutine(guidanceRoutine);
                guidanceRoutine = null;
            }

            CreateGuidanceUI(guidanceMessage);
            if (guidanceLabel != null)
            {
                guidanceLabel.text = guidanceMessage;
            }
        }

        private void CreateGuidanceUI(string message)
        {
            if (guidanceRoot != null)
            {
                return;
            }

            guidanceRoot = new GameObject("PLCMissionGuidanceUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = guidanceRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = guidanceRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new("GuidancePanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(guidanceRoot.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -90f);
            panelRect.sizeDelta = new Vector2(620f, 92f);

            Image panel = panelObject.GetComponent<Image>();
            panel.color = new Color(0.015f, 0.025f, 0.035f, 0.92f);

            GameObject textObject = new("GuidanceText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 10f);
            textRect.offsetMax = new Vector2(-24f, -10f);

            guidanceLabel = textObject.GetComponent<Text>();
            guidanceLabel.text = message;
            guidanceLabel.font = CreateGuidanceFont();
            guidanceLabel.fontSize = 34;
            guidanceLabel.fontStyle = FontStyle.Bold;
            guidanceLabel.alignment = TextAnchor.MiddleCenter;
            guidanceLabel.color = new Color(1f, 0.84f, 0.18f);
            guidanceLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            guidanceLabel.verticalOverflow = VerticalWrapMode.Overflow;
            guidanceLabel.raycastTarget = false;
        }

        private static Font CreateGuidanceFont()
        {
            Font systemFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" },
                34);
            return systemFont != null
                ? systemFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void HideGuidance()
        {
            if (guidanceRoutine != null)
            {
                StopCoroutine(guidanceRoutine);
                guidanceRoutine = null;
            }

            if (guidanceRoot != null)
            {
                Destroy(guidanceRoot);
                guidanceRoot = null;
                guidanceLabel = null;
            }
        }

        private void SetWarningColor(Color color)
        {
            if (warningIndicator == null)
            {
                return;
            }

            warningIndicator.GetPropertyBlock(warningProperties);
            warningProperties.SetColor("_BaseColor", color);
            warningProperties.SetColor("_Color", color);
            warningIndicator.SetPropertyBlock(warningProperties);
        }

        private void SetFaultLightColor(Color color)
        {
            if (plcFaultLight != null)
            {
                plcFaultLight.color = color;
            }
        }
    }
}
