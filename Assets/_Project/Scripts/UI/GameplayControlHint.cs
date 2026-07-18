using System;
using System.Collections;
using TMPro;
using TheFusionEngineer.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheFusionEngineer.UI
{
    /// <summary>
    /// Stage1 인트로가 끝난 뒤 진행 방법과 랜덤 댄스 조작법을 한 번만 안내합니다.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public sealed class GameplayControlHint : MonoBehaviour
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float InitialDelay = 1.8f;
        private const float VisibleDuration = 10f;

        private static GameplayControlHint instance;

        private GameObject canvasRoot;
        private RectTransform safeAreaRoot;
        private CanvasGroup hintGroup;
        private Coroutine showRoutine;
        private bool hasShown;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // Bootstrap 관련 게임 로직을 수행합니다.
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            // [런타임 자동 생성] 씬 전환 후에도 유지되는 조작 안내 관리자입니다.
            GameObject host = new("Gameplay Control Hint");
            instance = host.AddComponent<GameplayControlHint>();
            DontDestroyOnLoad(host);
        }

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
        private void Update()
        {
            ApplySafeArea();
        }

        // 오브젝트가 제거될 때 남아 있는 이벤트와 임시 리소스를 정리합니다.
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
            {
                instance = null;
            }
        }

        // 입력 또는 게임 이벤트가 발생했을 때 후속 동작을 처리합니다.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool isStageOne = scene.name.Equals(
                "Stage01_Origin",
                StringComparison.OrdinalIgnoreCase);
            if (hasShown || !isStageOne)
            {
                StopShowRoutine();
                canvasRoot?.SetActive(false);
                return;
            }

            StopShowRoutine();
            showRoutine = StartCoroutine(ShowWhenPlayerIsReady());
        }

        // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
        private IEnumerator ShowWhenPlayerIsReady()
        {
            // sceneLoaded는 각 컴포넌트의 Start보다 먼저 올 수 있으므로 한 프레임 기다립니다.
            yield return null;

            PlayerMovement playerMovement = null;
            while (playerMovement == null)
            {
                playerMovement = FindAnyObjectByType<PlayerMovement>();
                yield return null;
            }

            // 인트로가 PlayerMovement를 잠시 끄므로, 연속으로 조작 가능한 상태인지 확인합니다.
            float readyDuration = 0f;
            while (playerMovement != null && readyDuration < InitialDelay)
            {
                readyDuration = playerMovement.isActiveAndEnabled
                    ? readyDuration + Time.unscaledDeltaTime
                    : 0f;
                yield return null;
            }

            if (playerMovement == null)
            {
                showRoutine = null;
                yield break;
            }

            hasShown = true;
            CreateUI();
            canvasRoot.SetActive(true);
            ApplySafeArea(true);
            // Fade 관련 게임 로직을 수행합니다.
            yield return Fade(0f, 1f, 0.2f);
            // WaitForSecondsRealtime 관련 게임 로직을 수행합니다.
            yield return new WaitForSecondsRealtime(VisibleDuration);
            // Fade 관련 게임 로직을 수행합니다.
            yield return Fade(1f, 0f, 0.28f);
            canvasRoot.SetActive(false);
            showRoutine = null;
        }

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private void CreateUI()
        {
            if (canvasRoot != null)
            {
                return;
            }

            // [런타임 자동 생성] Stage1에서 한 번만 표시할 안내 Canvas 계층입니다.
            canvasRoot = new GameObject(
                "Gameplay Control Hint Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 7;

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
                "Control Hint Panel",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(Outline));
            panelObject.transform.SetParent(safeAreaRoot, false);

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -292f);
            bool isMobile = MobileWebGLControls.TouchInterfaceActive;
            panelRect.sizeDelta = new Vector2(900f, isMobile ? 124f : 90f);

            Image panel = panelObject.GetComponent<Image>();
            panel.color = new Color(0.015f, 0.025f, 0.045f, 0.92f);
            panel.raycastTarget = false;

            Outline outline = panelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.15f, 0.8f, 1f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;

            hintGroup = panelObject.GetComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            hintGroup.interactable = false;
            hintGroup.blocksRaycasts = false;

            GameObject textObject = new(
                "Control Hint Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panelObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20f, 8f);
            textRect.offsetMax = new Vector2(-20f, -8f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            string progressInstruction = isMobile
                ? "사용 버튼을 길게 누르세요."
                : "E 키를 길게 누르세요.";
            string danceInstruction = isMobile ? "댄스 버튼으로" : "B 키로";
            string landscapeHint = isMobile
                ? "<color=#80FF72>모바일 안내</color>  원활한 플레이를 위해 화면을 가로로 돌려주세요.\n"
                : string.Empty;
            text.text =
                landscapeHint +
                $"<color=#52E8FF>진행 팁</color>  미션을 수행하거나 다음 포탈로 이동할 때 {progressInstruction}\n" +
                $"<color=#FFB35A>^^</color>  {danceInstruction} 랜덤 댄스를 즐겨보세요!";
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = 17f;
            text.fontSizeMax = 22f;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;

            ApplySafeArea(true);
            canvasRoot.SetActive(false);
        }

        // 더 이상 필요하지 않은 화면 요소와 진행 중 작업을 정리합니다.
        private void StopShowRoutine()
        {
            if (showRoutine == null)
            {
                return;
            }

            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        // Fade 관련 게임 로직을 수행합니다.
        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            hintGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                hintGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            hintGroup.alpha = to;
        }

        // ApplySafeArea 관련 게임 로직을 수행합니다.
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
        }

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }
    }
}
