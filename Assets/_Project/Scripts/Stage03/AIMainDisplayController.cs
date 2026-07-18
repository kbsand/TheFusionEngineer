using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheFusionEngineer.Stage03
{
    /// <summary>
    /// G-BRAIN 미션 완료 시 AI 메인 디스플레이의 TV 부팅 효과와 상태 화면을 재생합니다.
    /// 재질 색상과 월드 스페이스 UI만 사용하므로 모바일 WebGL에서도 가볍게 동작합니다.
    /// </summary>
    public sealed class AIMainDisplayController : MonoBehaviour
    {
        private const float StatusWidth = 840f;
        private const float StatusHeight = 480f;

        [SerializeField] private Renderer displayRenderer;
        [SerializeField] private Color poweredOffColor = new(0.008f, 0.012f, 0.018f, 1f);
        [SerializeField] private Color onlineColor = new(0.04f, 0.85f, 1f, 1f);
        [SerializeField, Min(0f)] private float onlineEmissionIntensity = 4.5f;
        [SerializeField, Min(0.1f)] private float startupDuration = 0.9f;

        private MaterialPropertyBlock visualProperties;
        private Material[] originalMaterials;
        private Material[] runtimeMaterials;
        private GameObject statusRoot;
        private CanvasGroup statusGroup;
        private TMP_Text statusText;
        private RectTransform scanLine;
        private Coroutine startupRoutine;
        private bool isPoweredOn;

        public bool IsPoweredOn => isPoweredOn;

        private void Awake()
        {
            if (displayRenderer == null)
            {
                displayRenderer = GetComponent<Renderer>();
            }

            visualProperties = new MaterialPropertyBlock();
            PrepareRuntimeMaterials();
            CreateStatusDisplay();
            SetPoweredOff();
        }

        private void OnDestroy()
        {
            if (startupRoutine != null)
            {
                StopCoroutine(startupRoutine);
            }

            if (displayRenderer != null &&
                originalMaterials != null &&
                originalMaterials.Length > 0)
            {
                displayRenderer.sharedMaterials = originalMaterials;
            }

            if (runtimeMaterials != null)
            {
                foreach (Material material in runtimeMaterials)
                {
                    if (material != null)
                    {
                        Destroy(material);
                    }
                }
            }

            if (statusRoot != null)
            {
                Destroy(statusRoot);
            }
        }

        /// <summary>
        /// 짧은 백색 플래시와 화면 깜빡임 뒤 G-BRAIN 온라인 화면을 표시합니다.
        /// </summary>
        public void TurnOn()
        {
            if (isPoweredOn)
            {
                return;
            }

            isPoweredOn = true;
            if (startupRoutine != null)
            {
                StopCoroutine(startupRoutine);
            }

            startupRoutine = StartCoroutine(PlayStartup());
        }

        public void SetPoweredOff()
        {
            if (startupRoutine != null)
            {
                StopCoroutine(startupRoutine);
                startupRoutine = null;
            }

            isPoweredOn = false;
            SetVisual(poweredOffColor, Color.black);
            if (statusRoot != null)
            {
                statusRoot.SetActive(false);
            }
        }

        private IEnumerator PlayStartup()
        {
            if (statusRoot != null)
            {
                statusRoot.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.text = "INITIALIZING RAG INDEX...";
            }

            if (scanLine != null)
            {
                scanLine.gameObject.SetActive(true);
            }

            SetVisual(Color.white, Color.white * 5f);
            SetStatusAlpha(0.92f);
            yield return new WaitForSecondsRealtime(0.07f);

            SetVisual(poweredOffColor, Color.black);
            SetStatusAlpha(0f);
            yield return new WaitForSecondsRealtime(0.06f);

            float elapsed = 0f;
            while (elapsed < startupDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / startupDuration);
                float ramp = Mathf.SmoothStep(0f, 1f, progress);
                float noise = Mathf.PerlinNoise(0.37f, elapsed * 18f);
                float flicker = noise > 0.24f ? 0.35f + noise * 0.65f : 0.04f;
                float brightness = ramp * flicker;

                Color baseColor = Color.Lerp(poweredOffColor, onlineColor, brightness);
                Color emissionColor = onlineColor * (onlineEmissionIntensity * brightness);
                emissionColor.a = 1f;
                SetVisual(baseColor, emissionColor);
                SetStatusAlpha(Mathf.Clamp01(brightness * 1.25f));

                if (scanLine != null)
                {
                    float scanProgress = Mathf.Repeat(elapsed * 1.8f, 1f);
                    scanLine.anchoredPosition = new Vector2(
                        0f,
                        Mathf.Lerp(190f, -190f, scanProgress));
                }

                yield return null;
            }

            Color finalEmissionColor = onlineColor * onlineEmissionIntensity;
            finalEmissionColor.a = 1f;
            SetVisual(onlineColor, finalEmissionColor);
            SetStatusAlpha(1f);
            if (statusText != null)
            {
                statusText.text = "G-BRAIN ONLINE\nRAG INDEX READY";
            }

            if (scanLine != null)
            {
                scanLine.gameObject.SetActive(false);
            }

            startupRoutine = null;
        }

        private void PrepareRuntimeMaterials()
        {
            if (displayRenderer == null)
            {
                return;
            }

            originalMaterials = displayRenderer.sharedMaterials;
            runtimeMaterials = new Material[originalMaterials.Length];
            for (int index = 0; index < originalMaterials.Length; index++)
            {
                Material source = originalMaterials[index];
                if (source == null)
                {
                    continue;
                }

                Material runtimeMaterial = new(source)
                {
                    name = $"{source.name} - AI Display Runtime",
                    hideFlags = HideFlags.DontSave
                };
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterials[index] = runtimeMaterial;
            }

            displayRenderer.sharedMaterials = runtimeMaterials;
            displayRenderer.receiveShadows = false;
        }

        private void SetVisual(Color baseColor, Color emissionColor)
        {
            if (displayRenderer == null)
            {
                return;
            }

            displayRenderer.GetPropertyBlock(visualProperties);
            visualProperties.SetColor("_BaseColor", baseColor);
            visualProperties.SetColor("_Color", baseColor);
            visualProperties.SetColor("_EmissionColor", emissionColor);
            displayRenderer.SetPropertyBlock(visualProperties);
        }

        private void CreateStatusDisplay()
        {
            if (displayRenderer == null || statusRoot != null)
            {
                return;
            }

            statusRoot = new GameObject(
                "AI Main Display Status",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            SceneManager.MoveGameObjectToScene(statusRoot, gameObject.scene);

            RectTransform rootRect = statusRoot.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(StatusWidth, StatusHeight);

            Vector3 faceNormal = displayRenderer.transform.right.normalized;
            Bounds bounds = displayRenderer.bounds;
            float faceOffset = ProjectedExtent(bounds.extents, faceNormal) + 0.012f;
            // Unity UI의 앞면이 실내(+X) 쪽을 바라보도록 Canvas의 forward를 반대로 둡니다.
            Quaternion facingRotation = Quaternion.LookRotation(-faceNormal, Vector3.up);
            Vector3 canvasRight = facingRotation * Vector3.right;
            Vector3 canvasUp = facingRotation * Vector3.up;
            float worldWidth = ProjectedExtent(bounds.extents, canvasRight) * 2f;
            float worldHeight = ProjectedExtent(bounds.extents, canvasUp) * 2f;
            float canvasScale = Mathf.Min(
                worldWidth * 0.94f / StatusWidth,
                worldHeight * 0.88f / StatusHeight);

            rootRect.SetPositionAndRotation(
                bounds.center + faceNormal * faceOffset,
                facingRotation);
            rootRect.localScale = Vector3.one * canvasScale;

            Canvas canvas = statusRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            statusGroup = statusRoot.GetComponent<CanvasGroup>();
            statusGroup.interactable = false;
            statusGroup.blocksRaycasts = false;

            RectTransform border = CreateImage(
                "Screen Border",
                rootRect,
                new Color(0.08f, 0.95f, 1f, 0.9f));
            Stretch(border, Vector2.zero, Vector2.zero);

            RectTransform background = CreateImage(
                "Screen Background",
                border,
                new Color(0.005f, 0.025f, 0.04f, 0.96f));
            Stretch(background, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            RectTransform topAccent = CreateImage(
                "Top Accent",
                background,
                new Color(0.2f, 1f, 0.95f, 0.9f));
            topAccent.anchorMin = new Vector2(0f, 1f);
            topAccent.anchorMax = new Vector2(1f, 1f);
            topAccent.pivot = new Vector2(0.5f, 1f);
            topAccent.anchoredPosition = new Vector2(0f, -24f);
            topAccent.sizeDelta = new Vector2(-56f, 5f);

            statusText = CreateText("Status Text", background);
            RectTransform textRect = statusText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(38f, 48f);
            textRect.offsetMax = new Vector2(-38f, -54f);

            scanLine = CreateImage(
                "Startup Scan Line",
                background,
                new Color(0.35f, 1f, 1f, 0.55f));
            scanLine.anchorMin = new Vector2(0f, 0.5f);
            scanLine.anchorMax = new Vector2(1f, 0.5f);
            scanLine.pivot = new Vector2(0.5f, 0.5f);
            scanLine.sizeDelta = new Vector2(-28f, 8f);

            statusRoot.SetActive(false);
        }

        private void SetStatusAlpha(float alpha)
        {
            if (statusGroup != null)
            {
                statusGroup.alpha = alpha;
            }
        }

        private static TMP_Text CreateText(string objectName, Transform parent)
        {
            GameObject textObject = new(
                objectName,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "INITIALIZING RAG INDEX...";
            text.fontSize = 54f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.42f, 1f, 0.98f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 34f;
            text.fontSizeMax = 54f;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateImage(
            string objectName,
            Transform parent,
            Color color)
        {
            GameObject imageObject = new(
                objectName,
                typeof(RectTransform),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image.rectTransform;
        }

        private static void Stretch(
            RectTransform target,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.offsetMin = offsetMin;
            target.offsetMax = offsetMax;
        }

        private static float ProjectedExtent(Vector3 extents, Vector3 direction)
        {
            Vector3 absoluteDirection = new(
                Mathf.Abs(direction.x),
                Mathf.Abs(direction.y),
                Mathf.Abs(direction.z));
            return Vector3.Dot(extents, absoluteDirection);
        }
    }
}
