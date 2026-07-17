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
    /// 인트로가 끝난 뒤 상호작용과 랜덤 댄스 조작법을 한 번만 안내합니다.
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
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject host = new("Gameplay Control Hint");
            instance = host.AddComponent<GameplayControlHint>();
            DontDestroyOnLoad(host);
        }

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

        private void Update()
        {
            ApplySafeArea();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
            {
                instance = null;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (hasShown ||
                scene.name.StartsWith("Ending", StringComparison.OrdinalIgnoreCase))
            {
                StopShowRoutine();
                canvasRoot?.SetActive(false);
                return;
            }

            StopShowRoutine();
            showRoutine = StartCoroutine(ShowWhenPlayerIsReady());
        }

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
            yield return Fade(0f, 1f, 0.2f);
            yield return new WaitForSecondsRealtime(VisibleDuration);
            yield return Fade(1f, 0f, 0.28f);
            canvasRoot.SetActive(false);
            showRoutine = null;
        }

        private void CreateUI()
        {
            if (canvasRoot != null)
            {
                return;
            }

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
            panelRect.sizeDelta = new Vector2(760f, 86f);

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
            text.text =
                "<color=#52E8FF>조작 팁</color>  E 키 또는 사용 버튼을 길게 눌러 상호작용하세요\n" +
                "<color=#FFB35A>재미 팁</color>  B 키, 방향 패드 ↓ 또는 댄스 버튼으로 랜덤 댄스를 즐겨보세요!";
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

        private void StopShowRoutine()
        {
            if (showRoutine == null)
            {
                return;
            }

            StopCoroutine(showRoutine);
            showRoutine = null;
        }

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

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            return target.GetComponent<RectTransform>();
        }
    }
}
