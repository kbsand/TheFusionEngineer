using System.Runtime.InteropServices;
using TMPro;
using TheFusionEngineer.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheFusionEngineer.UI
{
    /// <summary>
    /// 터치 환경에서 현재 Input Actions의 게임패드 바인딩을 재사용하는 온스크린 조작 UI입니다.
    /// 에디터에서는 마우스로 배치를 확인할 수 있고, WebGL 빌드에서는 터치 기기에서만 표시됩니다.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class MobileWebGLControls : MonoBehaviour
    {
        private const int ReferenceWidth = 1920;
        private const int ReferenceHeight = 1080;

        private static MobileWebGLControls instance;

        private GameObject controlsRoot;
        private RectTransform safeAreaRoot;
        private EventSystem createdEventSystem;
        private PlayerMovement playerMovement;
        private Sprite circleSprite;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public static bool TouchInterfaceActive { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int TFE_IsTouchDevice();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            GameObject host = new("Mobile WebGL Controls");
            instance = host.AddComponent<MobileWebGLControls>();
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
            TouchInterfaceActive = ShouldShowTouchControls();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
            {
                instance = null;
                TouchInterfaceActive = false;
            }
        }

        private void Update()
        {
            TouchInterfaceActive = ShouldShowTouchControls();

            if (controlsRoot == null && playerMovement != null && TouchInterfaceActive)
            {
                EnsureEventSystem();
                EnsureControls();
                ApplySafeArea(true);
            }

            if (controlsRoot == null)
            {
                return;
            }

            bool shouldBeVisible = playerMovement != null &&
                                   playerMovement.isActiveAndEnabled &&
                                   TouchInterfaceActive;
            if (controlsRoot.activeSelf != shouldBeVisible)
            {
                controlsRoot.SetActive(shouldBeVisible);
            }

            if (shouldBeVisible)
            {
                ApplySafeArea();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerMovement = FindAnyObjectByType<PlayerMovement>();
            ConfigureSceneUiInputModules();
            TouchInterfaceActive = ShouldShowTouchControls();

            if (playerMovement == null || !TouchInterfaceActive)
            {
                controlsRoot?.SetActive(false);
                RemoveCreatedEventSystem();
                return;
            }

            EnsureEventSystem();
            EnsureControls();
            controlsRoot.SetActive(playerMovement.isActiveAndEnabled);
            ApplySafeArea(true);
        }

        /// <summary>
        /// 같은 안내 문구를 키보드와 터치 UI에서 자연스럽게 재사용합니다.
        /// </summary>
        public static string ResolveInteractionPrompt(string value)
        {
            if (!TouchInterfaceActive || string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Replace("E 키", "사용 버튼");
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new("Mobile Controls EventSystem");
            eventSystemObject.transform.SetParent(transform, false);
            createdEventSystem = eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            AssignUiActions(inputModule);
        }

        private static void ConfigureSceneUiInputModules()
        {
            InputSystemUIInputModule[] inputModules = FindObjectsByType<InputSystemUIInputModule>();
            foreach (InputSystemUIInputModule inputModule in inputModules)
            {
                AssignUiActions(inputModule);
            }
        }

        private static void AssignUiActions(InputSystemUIInputModule inputModule)
        {
            if (InputSystem.actions != null)
            {
                // 프로젝트 전역 InputSystem_Actions의 UI Map을 모든 씬에서 공통으로 사용합니다.
                inputModule.actionsAsset = InputSystem.actions;
                return;
            }

            inputModule.AssignDefaultActions();
        }

        private void RemoveCreatedEventSystem()
        {
            if (createdEventSystem == null)
            {
                return;
            }

            Destroy(createdEventSystem.gameObject);
            createdEventSystem = null;
        }

        private void EnsureControls()
        {
            if (controlsRoot != null)
            {
                return;
            }

            circleSprite = CreateCircleSprite();

            controlsRoot = new GameObject("Touch Controls", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            controlsRoot.transform.SetParent(transform, false);

            Canvas canvas = controlsRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // 진행도 UI와 화면 전환 Fade가 조작 UI 위를 덮을 수 있도록 Stage UI 중간 계층에 둡니다.
            canvas.sortingOrder = 800;

            CanvasScaler scaler = controlsRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            safeAreaRoot = CreateRect("Safe Area", controlsRoot.transform);
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            CreateMovementStick();
            CreateActionButton("점프", "<Gamepad>/buttonSouth", new Vector2(-145f, 165f), 132f,
                new Color(0.08f, 0.75f, 1f, 0.72f));
            CreateActionButton("달리기", "<Gamepad>/leftStickPress", new Vector2(-315f, 110f), 118f,
                new Color(1f, 0.58f, 0.12f, 0.7f));
            CreateActionButton("사용", "<Gamepad>/buttonNorth", new Vector2(-140f, 345f), 118f,
                new Color(0.23f, 0.95f, 0.55f, 0.72f));
            CreateActionButton("댄스", "<Gamepad>/dpad/down", new Vector2(-315f, 285f), 110f,
                new Color(0.72f, 0.32f, 1f, 0.72f));
        }

        private void CreateMovementStick()
        {
            RectTransform background = CreateImage("Move Stick Background", safeAreaRoot,
                new Color(0.02f, 0.08f, 0.14f, 0.48f), circleSprite);
            SetBottomLeft(background, new Vector2(165f, 165f), new Vector2(230f, 230f));
            background.GetComponent<Image>().raycastTarget = false;

            RectTransform touchArea = CreateImage("Move Stick", safeAreaRoot, Color.clear, circleSprite);
            SetBottomLeft(touchArea, new Vector2(165f, 165f), new Vector2(230f, 230f));

            OnScreenStick stick = touchArea.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 72f;
            stick.behaviour = OnScreenStick.Behaviour.RelativePositionWithStaticOrigin;

            RectTransform knob = CreateImage("Knob", touchArea, new Color(0.25f, 0.85f, 1f, 0.82f), circleSprite);
            knob.anchorMin = new Vector2(0.5f, 0.5f);
            knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);
            knob.anchoredPosition = Vector2.zero;
            knob.sizeDelta = new Vector2(92f, 92f);
            knob.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateActionButton(string label, string controlPath, Vector2 position, float size, Color color)
        {
            RectTransform button = CreateImage($"{label} Button", safeAreaRoot, color, circleSprite);
            SetBottomRight(button, position, new Vector2(size, size));

            OnScreenButton onScreenButton = button.gameObject.AddComponent<OnScreenButton>();
            onScreenButton.controlPath = controlPath;

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(button, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = 24;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
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

        private static bool ShouldShowTouchControls()
        {
#if UNITY_EDITOR
            // 에디터와 데스크톱에서는 터치스크린 유무와 관계없이 모바일 UI를 숨깁니다.
            return false;
#elif UNITY_WEBGL
            try
            {
                return TFE_IsTouchDevice() != 0 || Touchscreen.current != null;
            }
            catch
            {
                return Touchscreen.current != null;
            }
#elif UNITY_ANDROID || UNITY_IOS
            return true;
#else
            return false;
#endif
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return gameObject.GetComponent<RectTransform>();
        }

        private static void SetBottomLeft(RectTransform target, Vector2 position, Vector2 size)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.zero;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = position;
            target.sizeDelta = size;
        }

        private static void SetBottomRight(RectTransform target, Vector2 position, Vector2 size)
        {
            target.anchorMin = new Vector2(1f, 0f);
            target.anchorMax = new Vector2(1f, 0f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = position;
            target.sizeDelta = size;
        }

        private static Sprite CreateCircleSprite()
        {
            const int textureSize = 64;
            Texture2D texture = new(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "Runtime Touch Control Circle",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[textureSize * textureSize];
            Vector2 center = new((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
            float radius = textureSize * 0.5f - 1f;
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Runtime Touch Control Circle";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
