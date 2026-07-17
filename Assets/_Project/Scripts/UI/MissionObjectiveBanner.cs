using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheFusionEngineer.UI
{
    /// <summary>
    /// 현재 스테이지에서 가장 먼저 해결해야 할 목표를 화면 중앙 상단에 표시합니다.
    /// 씬의 기존 HUD는 유지하고, 런타임에 비대화형 Overlay Canvas를 생성합니다.
    /// </summary>
    public sealed class MissionObjectiveBanner : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float ExpandedHoldDuration = 5f;
        private const float CompactTransitionDuration = 0.55f;
        private const float CompactScale = 0.78f;
        private const float CompactTopMargin = 18f;
        private static readonly Vector2 DefaultTopPosition = new(0f, -118f);
        private static readonly Vector2 ExpandedCenterPosition = new(0f, -150f);

        private GameObject canvasRoot;
        private RectTransform safeAreaRoot;
        private RectTransform panelRect;
        private CanvasGroup bannerGroup;
        private TMP_Text progressLabel;
        private TMP_Text titleLabel;
        private TMP_Text hintLabel;
        private Coroutine presentationRoutine;
        private bool autoCompact;
        private bool isCompact;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public static MissionObjectiveBanner AttachTo(Component owner)
        {
            MissionObjectiveBanner existing = owner.GetComponentInChildren<MissionObjectiveBanner>(true);
            if (existing != null)
            {
                return existing;
            }

            GameObject bannerObject = new("Mission Objective Banner");
            bannerObject.transform.SetParent(owner.transform, false);
            return bannerObject.AddComponent<MissionObjectiveBanner>();
        }

        /// <summary>
        /// 활성화하면 미션을 크게 안내한 뒤 상단의 작은 배너로 자동 전환합니다.
        /// </summary>
        public void SetAutoCompact(bool enabled)
        {
            autoCompact = enabled;
        }

        private void Awake()
        {
            CreateUI();
        }

        private void Update()
        {
            ApplySafeArea();
        }

        private void OnDestroy()
        {
            if (presentationRoutine != null)
            {
                StopCoroutine(presentationRoutine);
            }

            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
            }
        }

        public void Show(string progress, string title, string hint)
        {
            CreateUI();
            progressLabel.text = progress;
            titleLabel.text = title;
            hintLabel.text = MobileWebGLControls.ResolveInteractionPrompt(hint);
            canvasRoot.SetActive(false);

            if (presentationRoutine != null)
            {
                StopCoroutine(presentationRoutine);
            }

            presentationRoutine = StartCoroutine(Present());
        }

        public void Hide()
        {
            if (presentationRoutine != null)
            {
                StopCoroutine(presentationRoutine);
                presentationRoutine = null;
            }

            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
            }
        }

        private void CreateUI()
        {
            if (canvasRoot != null)
            {
                return;
            }

            canvasRoot = new GameObject(
                "Current Mission Banner Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            // UI Canvas 아래에 중첩하면 Rect가 100x100으로 고정될 수 있어 씬 루트로 분리합니다.
            SceneManager.MoveGameObjectToScene(canvasRoot, gameObject.scene);

            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Stage2/3처럼 다른 Canvas 아래에 생성되어도 인트로(10)보다 낮은 순서를 보장합니다.
            canvas.overrideSorting = true;
            canvas.sortingOrder = 5;

            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            safeAreaRoot = CreateRect("Safe Area", canvasRoot.transform);
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            GameObject panelObject = new(
                "Objective Panel",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            panelObject.transform.SetParent(safeAreaRoot, false);

            panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = DefaultTopPosition;
            panelRect.sizeDelta = new Vector2(900f, 154f);

            Image panel = panelObject.GetComponent<Image>();
            panel.color = new Color(0.08f, 0.82f, 1f, 0.68f);
            panel.raycastTarget = false;

            RectTransform background = CreateImage(
                "Panel Background",
                panelObject.transform,
                new Color(0.015f, 0.025f, 0.045f, 0.94f));
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = new Vector2(2f, 2f);
            background.offsetMax = new Vector2(-2f, -2f);

            bannerGroup = panelObject.GetComponent<CanvasGroup>();
            bannerGroup.interactable = false;
            bannerGroup.blocksRaycasts = false;

            RectTransform accent = CreateImage(
                "Top Accent",
                panelObject.transform,
                new Color(1f, 0.55f, 0.08f, 1f));
            accent.anchorMin = new Vector2(0f, 1f);
            accent.anchorMax = new Vector2(1f, 1f);
            accent.pivot = new Vector2(0.5f, 1f);
            accent.anchoredPosition = Vector2.zero;
            accent.sizeDelta = new Vector2(0f, 5f);

            progressLabel = CreateText(
                "Progress",
                panelObject.transform,
                19,
                FontStyles.Bold,
                new Color(1f, 0.65f, 0.16f),
                TextAlignmentOptions.Center,
                new Vector2(20f, 120f),
                new Vector2(-20f, -8f));

            titleLabel = CreateText(
                "Mission Title",
                panelObject.transform,
                35,
                FontStyles.Bold,
                Color.white,
                TextAlignmentOptions.Center,
                new Vector2(24f, 67f),
                new Vector2(-24f, -36f));
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 24;
            titleLabel.fontSizeMax = 35;

            hintLabel = CreateText(
                "Mission Hint",
                panelObject.transform,
                21,
                FontStyles.Normal,
                new Color(0.55f, 0.93f, 1f),
                TextAlignmentOptions.Center,
                new Vector2(24f, 15f),
                new Vector2(-24f, -91f));

            ApplySafeArea(true);
            canvasRoot.SetActive(false);
        }

        private IEnumerator Present()
        {
            IntroSequenceController intro = FindIntroInCurrentScene();
            if (intro != null && intro.isActiveAndEnabled && !intro.HasCompleted)
            {
                // 다른 컴포넌트의 Start가 모두 실행된 뒤 실제 인트로 완료 상태를 기다립니다.
                yield return null;
                while (intro != null && intro.isActiveAndEnabled && intro.IsPlaying)
                {
                    yield return null;
                }
            }

            PreparePresentationLayout();
            bannerGroup.alpha = 0f;
            canvasRoot.SetActive(true);
            ApplySafeArea(true);
            yield return Reveal();

            if (autoCompact)
            {
                yield return new WaitForSecondsRealtime(ExpandedHoldDuration);
                yield return MoveToCompactLayout();
            }

            presentationRoutine = null;
        }

        private IEnumerator Reveal()
        {
            bannerGroup.alpha = 0f;
            float elapsed = 0f;
            const float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                bannerGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                yield return null;
            }

            bannerGroup.alpha = 1f;
        }

        private void PreparePresentationLayout()
        {
            isCompact = false;
            panelRect.localScale = Vector3.one;

            if (autoCompact)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = ExpandedCenterPosition;
                return;
            }

            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = DefaultTopPosition;
        }

        private IEnumerator MoveToCompactLayout()
        {
            Canvas.ForceUpdateCanvases();
            Vector2 startPosition = panelRect.anchoredPosition;
            Vector2 targetPosition = GetCompactPosition();
            Vector3 startScale = panelRect.localScale;
            Vector3 targetScale = Vector3.one * CompactScale;
            float elapsed = 0f;

            while (elapsed < CompactTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / CompactTransitionDuration));
                panelRect.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    progress);
                panelRect.localScale = Vector3.LerpUnclamped(
                    startScale,
                    targetScale,
                    progress);
                yield return null;
            }

            panelRect.anchoredPosition = targetPosition;
            panelRect.localScale = targetScale;
            isCompact = true;
            ApplySafeArea(true);
        }

        private Vector2 GetCompactPosition()
        {
            float safeHeight = safeAreaRoot != null && safeAreaRoot.rect.height > 0f
                ? safeAreaRoot.rect.height
                : ReferenceHeight;
            float scaledPanelHeight = panelRect.rect.height * CompactScale;
            float y = safeHeight * 0.5f - CompactTopMargin - scaledPanelHeight * 0.5f;
            return new Vector2(0f, y);
        }

        private IntroSequenceController FindIntroInCurrentScene()
        {
            IntroSequenceController[] intros =
                FindObjectsByType<IntroSequenceController>(FindObjectsInactive.Exclude);
            foreach (IntroSequenceController intro in intros)
            {
                if (intro.gameObject.scene == gameObject.scene)
                {
                    return intro;
                }
            }

            return null;
        }

        private void ApplySafeArea(bool force = false)
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            safeAreaRoot.anchorMin = safeArea.position / screenSize;
            safeAreaRoot.anchorMax = (safeArea.position + safeArea.size) / screenSize;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            if (isCompact)
            {
                Canvas.ForceUpdateCanvases();
                panelRect.anchoredPosition = GetCompactPosition();
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }

        private static RectTransform CreateImage(string name, Transform parent, Color color)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            Image image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return target.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            int fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
