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
        // TFE_IsTouchDevice 관련 게임 로직을 수행합니다.
        private static extern int TFE_IsTouchDevice();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // Bootstrap 관련 게임 로직을 수행합니다.
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            // [런타임 자동 생성] 모든 씬에서 공통으로 사용할 모바일 조작 UI 관리자입니다.
            GameObject host = new("Mobile WebGL Controls");
            instance = host.AddComponent<MobileWebGLControls>();
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
            TouchInterfaceActive = ShouldShowTouchControls();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        // 오브젝트가 제거될 때 남아 있는 이벤트와 임시 리소스를 정리합니다.
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
            {
                instance = null;
                TouchInterfaceActive = false;
            }
        }

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
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

        // 입력 또는 게임 이벤트가 발생했을 때 후속 동작을 처리합니다.
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

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new("Mobile Controls EventSystem");
            eventSystemObject.transform.SetParent(transform, false);
            // [런타임 자동 생성] 씬에 EventSystem이 없을 때만 터치 UI 입력 시스템을 보완합니다.
            createdEventSystem = eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            AssignUiActions(inputModule);
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        private static void ConfigureSceneUiInputModules()
        {
            InputSystemUIInputModule[] inputModules = FindObjectsByType<InputSystemUIInputModule>();
            foreach (InputSystemUIInputModule inputModule in inputModules)
            {
                AssignUiActions(inputModule);
            }
        }

        // AssignUiActions 관련 게임 로직을 수행합니다.
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

        // RemoveCreatedEventSystem 관련 게임 로직을 수행합니다.
        private void RemoveCreatedEventSystem()
        {
            if (createdEventSystem == null)
            {
                return;
            }

            Destroy(createdEventSystem.gameObject);
            createdEventSystem = null;
        }

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private void EnsureControls()
        {
            if (controlsRoot != null)
            {
                return;
            }

            circleSprite = CreateCircleSprite();

            // [런타임 자동 생성] 터치 기기에서만 표시되는 Canvas와 조작 버튼 계층입니다.
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
                // Color 관련 게임 로직을 수행합니다.
                new Color(0.08f, 0.75f, 1f, 0.72f));
            CreateActionButton("달리기", "<Gamepad>/leftStickPress", new Vector2(-315f, 110f), 118f,
                // Color 관련 게임 로직을 수행합니다.
                new Color(1f, 0.58f, 0.12f, 0.7f));
            CreateActionButton("사용", "<Gamepad>/buttonNorth", new Vector2(-140f, 345f), 118f,
                // Color 관련 게임 로직을 수행합니다.
                new Color(0.23f, 0.95f, 0.55f, 0.72f));
            CreateActionButton("댄스", "<Gamepad>/dpad/down", new Vector2(-315f, 285f), 110f,
                // Color 관련 게임 로직을 수행합니다.
                new Color(0.72f, 0.32f, 1f, 0.72f));
        }

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private void CreateMovementStick()
        {
            RectTransform background = CreateImage("Move Stick Background", safeAreaRoot,
                // Color 관련 게임 로직을 수행합니다.
                new Color(0.02f, 0.08f, 0.14f, 0.48f), circleSprite);
            SetBottomLeft(background, new Vector2(165f, 165f), new Vector2(230f, 230f));
            background.GetComponent<Image>().raycastTarget = false;

            RectTransform touchArea = CreateImage("Move Stick", safeAreaRoot, Color.clear, circleSprite);
            SetBottomLeft(touchArea, new Vector2(165f, 165f), new Vector2(230f, 230f));

            // [런타임 자동 생성] Input System의 가상 게임패드 스틱 컴포넌트입니다.
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

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private void CreateActionButton(string label, string controlPath, Vector2 position, float size, Color color)
        {
            RectTransform button = CreateImage($"{label} Button", safeAreaRoot, color, circleSprite);
            SetBottomRight(button, position, new Vector2(size, size));

            // [런타임 자동 생성] 실제 Input Action 바인딩으로 입력을 전달하는 가상 버튼입니다.
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

        // ShouldShowTouchControls 관련 게임 로직을 수행합니다.
        private static bool ShouldShowTouchControls()
        {
#if UNITY_EDITOR
            // 에디터와 데스크톱에서는 터치스크린 유무와 관계없이 모바일 UI를 숨깁니다.
            return false;
#elif UNITY_WEBGL
            try
            {
                // TFE_IsTouchDevice 관련 게임 로직을 수행합니다.
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

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private static RectTransform CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return gameObject.GetComponent<RectTransform>();
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private static void SetBottomLeft(RectTransform target, Vector2 position, Vector2 size)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.zero;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = position;
            target.sizeDelta = size;
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private static void SetBottomRight(RectTransform target, Vector2 position, Vector2 size)
        {
            target.anchorMin = new Vector2(1f, 0f);
            target.anchorMax = new Vector2(1f, 0f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = position;
            target.sizeDelta = size;
        }

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
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
                // Vector2 관련 게임 로직을 수행합니다.
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Runtime Touch Control Circle";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
