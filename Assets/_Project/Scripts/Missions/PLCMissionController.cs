using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TheFusionEngineer.Core;
using TheFusionEngineer.Player;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Missions
{
    /// <summary>
    /// Stage1 PLC 미션의 진행 상태, 안내 UI, 완료 효과를 총괄합니다.
    /// </summary>
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
        [SerializeField] private HoldInteractionController portalHoldInteraction;
        [SerializeField] private AudioClip stageCompleteClip;
        [SerializeField, Range(0f, 1f)] private float completionVolume = 0.85f;

        [Header("Mission Guidance")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private TMP_FontAsset guidanceFont;
        [SerializeField] private Material guidanceFontMaterial;
        [SerializeField] private string movementGuidanceMessage = "방향키를 조작하여 움직여 보세요.";
        [SerializeField] private string guidanceMessage = "PLC를 조작하세요.";
        [FormerlySerializedAs("guidanceDelay")]
        [SerializeField, Min(0f)] private float movementInactivityDelay = 2f;

        private bool isCompleted;
        private MaterialPropertyBlock warningProperties;
        private GameObject guidanceRoot;
        private TMP_Text guidanceLabel;
        private Coroutine guidanceRoutine;
        private bool movementInputConfirmed;
        private MissionObjectiveBanner objectiveBanner;

        public bool IsCompleted => isCompleted;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
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

        // Unity가 첫 프레임 전에 게임 진행 상태를 초기화합니다.
        private void Start()
        {
            // StagePortalController의 공통 기본 문구가 설정된 뒤 Stage 1용 한글 문구로 덮어씁니다.
            portalHoldInteraction?.SetAvailable(false, "먼저 CAREER CORE를 획득하세요");

            objectiveBanner = MissionObjectiveBanner.AttachTo(this);
            objectiveBanner.Show(
                "현재 미션  ·  1 / 1",
                "CL17 Dense Packing 라인 복구",
                "PLC 제어 패널을 찾아 E 키를 길게 눌러 라인을 복구하세요.");
        }

        // 오브젝트가 제거될 때 남아 있는 이벤트와 임시 리소스를 정리합니다.
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

        /// <summary>
        /// 플레이어가 PLC 조작 범위에 들어왔는지 기록하고 상황에 맞는 안내를 갱신합니다.
        /// </summary>
        public void SetPlayerInRange(bool inRange)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(inRange && !isCompleted);
            }
        }

        /// <summary>
        /// PLC 완료 조건을 다시 확인한 뒤 성공 상태와 후속 설비 연출을 한 번만 실행합니다.
        /// </summary>
        public void TryCompleteMission()
        {
            // 완료 이벤트가 중복 전달돼도 설비와 보상이 두 번 실행되지 않도록 막습니다.
            if (isCompleted)
            {
                return;
            }

            // 미션 상태를 먼저 확정한 뒤 UI, 설비, 포탈을 완료 상태로 일괄 전환합니다.
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
                careerCoreText.text = "CAREER CORE 01: 획득";
                careerCoreText.color = new Color(0.35f, 1f, 0.45f);
            }

            if (completionMessage != null)
            {
                StartCoroutine(ShowCompletionMessage());
            }

            Debug.Log("[Stage 1 Mission Complete] PLC bottleneck fixed. CL17 line restored. Career Core 01 acquired.");
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
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

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureStageExit(StagePortalController portal, Light faultLight)
        {
            stagePortal = portal;
            plcFaultLight = faultLight;
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

        // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
        private IEnumerator ShowCompletionMessage()
        {
            completionMessage.SetActive(true);
            // WaitForSeconds 관련 게임 로직을 수행합니다.
            yield return new WaitForSeconds(3f);
            completionMessage.SetActive(false);
        }

        // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
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

        // 입력 또는 게임 이벤트가 발생했을 때 후속 동작을 처리합니다.
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

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private void CreateGuidanceUI(string message)
        {
            if (guidanceRoot != null)
            {
                return;
            }

            // [런타임 자동 생성] 플레이어가 멈춰 있을 때만 나타나는 PLC 이동 안내 UI입니다.
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

            GameObject textObject = new("GuidanceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 10f);
            textRect.offsetMax = new Vector2(-24f, -10f);

            guidanceLabel = textObject.GetComponent<TextMeshProUGUI>();
            guidanceLabel.text = message;
            if (guidanceFont != null)
            {
                guidanceLabel.font = guidanceFont;
            }

            if (guidanceFontMaterial != null)
            {
                guidanceLabel.fontSharedMaterial = guidanceFontMaterial;
            }

            guidanceLabel.fontSize = 34;
            guidanceLabel.fontStyle = FontStyles.Bold;
            guidanceLabel.alignment = TextAlignmentOptions.Center;
            guidanceLabel.color = new Color(1f, 0.84f, 0.18f);
            guidanceLabel.overflowMode = TextOverflowModes.Overflow;
            guidanceLabel.raycastTarget = false;
        }

        // 더 이상 필요하지 않은 화면 요소와 진행 중 작업을 정리합니다.
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

            objectiveBanner?.Hide();
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
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

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetFaultLightColor(Color color)
        {
            if (plcFaultLight != null)
            {
                plcFaultLight.color = color;
            }
        }
    }
}
