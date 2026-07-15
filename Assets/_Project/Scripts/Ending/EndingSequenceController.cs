using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace TheFusionEngineer.Ending
{
    public sealed class EndingSequenceController : MonoBehaviour
    {
        [Header("Career Cores")]
        [SerializeField] private Transform[] careerCores = System.Array.Empty<Transform>();
        [SerializeField] private Transform[] coreRings = System.Array.Empty<Transform>();
        [SerializeField] private Vector3 fusionCenter = new(0f, 2.1f, 0f);

        [Header("Fusion Result")]
        [SerializeField] private Transform fusionCore;
        [SerializeField] private Transform playerSilhouette;
        [SerializeField] private Renderer[] silhouetteRenderers = System.Array.Empty<Renderer>();
        [SerializeField] private Light silhouetteGlow;
        [SerializeField] private Vector3 silhouetteStart = Vector3.zero;
        [SerializeField] private Vector3 gateInsidePosition = new(0f, 0f, 3.8f);
        [SerializeField] private CanvasGroup fusionText;

        [Header("Gate")]
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private GameObject gateLightPanel;
        [SerializeField] private Light gateLight;
        [SerializeField, Min(0f)] private float doorOpenDistance = 3.1f;
        [SerializeField, Min(0.1f)] private float gateOpenDuration = 0.9f;
        [SerializeField, Min(0.1f)] private float walkDuration = 3.2f;
        [SerializeField, Min(0f)] private float gateLightMaxIntensity = 1.25f;

        [Header("Character Glow")]
        [SerializeField, Min(0f)] private float revealGlowIntensity = 1.4f;
        [SerializeField, Min(0f)] private float rimGlowIntensity = 0.45f;

        [Header("UI")]
        [SerializeField] private CanvasGroup flashOverlay;
        [SerializeField] private CanvasGroup finalUI;
        [SerializeField] private GameObject skipHint;

        private Vector3[] coreStartPositions;
        private Vector3 leftDoorClosedPosition;
        private Vector3 rightDoorClosedPosition;
        private Material[] silhouetteMaterials;
        private Color[] silhouetteColors;
        private Animator silhouetteAnimator;
        private Coroutine sequenceRoutine;
        private bool isMerging;
        private bool sequenceFinished;

        private void Awake()
        {
            coreStartPositions = new Vector3[careerCores.Length];
            for (int index = 0; index < careerCores.Length; index++)
            {
                if (careerCores[index] != null)
                {
                    coreStartPositions[index] = careerCores[index].localPosition;
                }
            }

            leftDoorClosedPosition = leftDoor != null ? leftDoor.localPosition : Vector3.zero;
            rightDoorClosedPosition = rightDoor != null ? rightDoor.localPosition : Vector3.zero;
            silhouetteAnimator = playerSilhouette != null
                ? playerSilhouette.GetComponentInChildren<Animator>(true)
                : null;
            PrepareSilhouetteMaterials();
            ApplyInitialState();
        }

        private void Start()
        {
            sequenceRoutine = StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (!sequenceFinished &&
                keyboard != null &&
                (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
            {
                SkipToFinalState();
            }

            if (!isMerging && !sequenceFinished)
            {
                float time = Time.unscaledTime;
                for (int index = 0; index < careerCores.Length; index++)
                {
                    Transform core = careerCores[index];
                    if (core != null && core.gameObject.activeSelf)
                    {
                        Vector3 position = coreStartPositions[index];
                        position.y += Mathf.Sin(time * 1.25f + index * 1.8f) * 0.14f;
                        core.localPosition = position;
                    }
                }
            }

            for (int index = 0; index < coreRings.Length; index++)
            {
                Transform ring = coreRings[index];
                if (ring == null || !ring.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 axis = index % 2 == 0 ? Vector3.up : Vector3.right;
                ring.Rotate(axis, (18f + index * 2f) * Time.unscaledDeltaTime, Space.Self);
            }
        }

        public void Configure(
            Transform[] cores,
            Transform[] rings,
            Transform oldFusionCore,
            Transform silhouette,
            Renderer[] silhouetteParts,
            Light glow,
            CanvasGroup fusionLabel,
            Transform gateLeftDoor,
            Transform gateRightDoor,
            GameObject lightPanel,
            Light activationLight,
            CanvasGroup flash,
            CanvasGroup finalPanel,
            GameObject skipLabel,
            Vector3 center,
            Vector3 characterStart,
            Vector3 characterEnd)
        {
            careerCores = cores ?? System.Array.Empty<Transform>();
            coreRings = rings ?? System.Array.Empty<Transform>();
            fusionCore = oldFusionCore;
            playerSilhouette = silhouette;
            silhouetteRenderers = silhouetteParts ?? System.Array.Empty<Renderer>();
            silhouetteGlow = glow;
            fusionText = fusionLabel;
            leftDoor = gateLeftDoor;
            rightDoor = gateRightDoor;
            gateLightPanel = lightPanel;
            gateLight = activationLight;
            flashOverlay = flash;
            finalUI = finalPanel;
            skipHint = skipLabel;
            fusionCenter = center;
            silhouetteStart = characterStart;
            gateInsidePosition = characterEnd;
        }

        private IEnumerator PlaySequence()
        {
            yield return Wait(0.35f);
            foreach (Transform core in careerCores)
            {
                if (core == null)
                {
                    continue;
                }

                core.gameObject.SetActive(true);
                core.localScale = Vector3.zero;
                yield return Scale(core, Vector3.zero, Vector3.one, 0.4f);
                yield return Wait(0.1f);
            }

            yield return Wait(1f);
            isMerging = true;
            yield return MergeCores(1.15f);
            yield return Fade(flashOverlay, 0f, 1f, 0.14f);

            foreach (Transform core in careerCores)
            {
                core?.gameObject.SetActive(false);
            }

            fusionCore?.gameObject.SetActive(false);
            if (playerSilhouette != null)
            {
                playerSilhouette.gameObject.SetActive(true);
                playerSilhouette.localPosition = silhouetteStart;
                playerSilhouette.localRotation = Quaternion.identity;
                playerSilhouette.localScale = Vector3.zero;
                SetSilhouetteAlpha(0f);
                if (silhouetteGlow != null)
                {
                    silhouetteGlow.enabled = true;
                    silhouetteGlow.intensity = revealGlowIntensity;
                }

                yield return RevealSilhouette(0.48f);
            }

            SetGroup(fusionText, 1f, false);
            yield return Fade(flashOverlay, 1f, 0f, 0.3f);
            yield return Wait(1.5f);
            yield return Fade(fusionText, 1f, 0f, 0.3f);

            gateLightPanel?.SetActive(true);
            if (gateLight != null)
            {
                gateLight.enabled = true;
                gateLight.intensity = 0.5f;
            }

            yield return OpenGate(gateOpenDuration);
            yield return WalkToGate(walkDuration);
            yield return FadeSilhouette(0.75f);
            yield return Fade(finalUI, 0f, 1f, 0.6f);

            SetGroup(finalUI, 1f, true);
            skipHint?.SetActive(false);
            sequenceFinished = true;
            sequenceRoutine = null;
        }

        private IEnumerator MergeCores(float duration)
        {
            Vector3[] startPositions = new Vector3[careerCores.Length];
            Vector3[] startScales = new Vector3[careerCores.Length];
            for (int index = 0; index < careerCores.Length; index++)
            {
                if (careerCores[index] != null)
                {
                    startPositions[index] = careerCores[index].localPosition;
                    startScales[index] = careerCores[index].localScale;
                }
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Smooth(elapsed / duration);
                for (int index = 0; index < careerCores.Length; index++)
                {
                    Transform core = careerCores[index];
                    if (core == null)
                    {
                        continue;
                    }

                    core.localPosition = Vector3.Lerp(startPositions[index], fusionCenter, progress);
                    core.localScale = Vector3.Lerp(startScales[index], Vector3.one * 0.5f, progress);
                }

                yield return null;
            }
        }

        private IEnumerator RevealSilhouette(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Smooth(elapsed / duration);
                playerSilhouette.localScale = Vector3.one * progress;
                SetSilhouetteAlpha(progress);
                if (silhouetteGlow != null)
                {
                    silhouetteGlow.intensity = Mathf.Lerp(
                        revealGlowIntensity,
                        rimGlowIntensity,
                        progress);
                }

                yield return null;
            }

            playerSilhouette.localScale = Vector3.one;
            SetSilhouetteAlpha(1f);
        }

        private IEnumerator OpenGate(float duration)
        {
            Vector3 leftOpen = leftDoorClosedPosition + Vector3.left * doorOpenDistance;
            Vector3 rightOpen = rightDoorClosedPosition + Vector3.right * doorOpenDistance;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Smooth(elapsed / duration);
                if (leftDoor != null)
                {
                    leftDoor.localPosition = Vector3.Lerp(leftDoorClosedPosition, leftOpen, progress);
                }

                if (rightDoor != null)
                {
                    rightDoor.localPosition = Vector3.Lerp(rightDoorClosedPosition, rightOpen, progress);
                }

                if (gateLight != null)
                {
                    gateLight.intensity = Mathf.Lerp(0.25f, gateLightMaxIntensity, progress);
                }

                yield return null;
            }

            if (leftDoor != null)
            {
                leftDoor.localPosition = leftOpen;
            }

            if (rightDoor != null)
            {
                rightDoor.localPosition = rightOpen;
            }

            if (gateLight != null)
            {
                gateLight.intensity = gateLightMaxIntensity;
            }
        }

        private IEnumerator WalkToGate(float duration)
        {
            if (silhouetteAnimator != null)
            {
                silhouetteAnimator.SetFloat("Speed", 1f);
            }

            Vector3 start = silhouetteStart;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float linear = Mathf.Clamp01(elapsed / duration);
                Vector3 position = Vector3.Lerp(start, gateInsidePosition, linear);
                position.y += Mathf.Sin(linear * Mathf.PI * 10f) * 0.045f;
                if (playerSilhouette != null)
                {
                    playerSilhouette.localPosition = position;
                }

                yield return null;
            }

            if (playerSilhouette != null)
            {
                playerSilhouette.localPosition = gateInsidePosition;
            }

            if (silhouetteAnimator != null)
            {
                silhouetteAnimator.SetFloat("Speed", 0f);
            }
        }

        private IEnumerator FadeSilhouette(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / duration);
                SetSilhouetteAlpha(alpha);
                if (silhouetteGlow != null)
                {
                    silhouetteGlow.intensity = Mathf.Lerp(0f, rimGlowIntensity, alpha);
                }

                yield return null;
            }

            SetSilhouetteAlpha(0f);
            playerSilhouette?.gameObject.SetActive(false);
        }

        private void SkipToFinalState()
        {
            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            if (silhouetteAnimator != null)
            {
                silhouetteAnimator.SetFloat("Speed", 0f);
            }

            ApplyFinalState();
        }

        private void ApplyInitialState()
        {
            isMerging = false;
            sequenceFinished = false;
            for (int index = 0; index < careerCores.Length; index++)
            {
                Transform core = careerCores[index];
                if (core == null)
                {
                    continue;
                }

                core.localPosition = coreStartPositions[index];
                core.localScale = Vector3.zero;
                core.gameObject.SetActive(false);
            }

            fusionCore?.gameObject.SetActive(false);
            if (playerSilhouette != null)
            {
                playerSilhouette.localPosition = silhouetteStart;
                playerSilhouette.localRotation = Quaternion.identity;
                playerSilhouette.localScale = Vector3.zero;
                playerSilhouette.gameObject.SetActive(false);
            }

            SetSilhouetteAlpha(0f);
            if (silhouetteGlow != null)
            {
                silhouetteGlow.enabled = false;
            }

            if (leftDoor != null)
            {
                leftDoor.localPosition = leftDoorClosedPosition;
            }

            if (rightDoor != null)
            {
                rightDoor.localPosition = rightDoorClosedPosition;
            }

            gateLightPanel?.SetActive(false);
            if (gateLight != null)
            {
                gateLight.enabled = false;
            }

            SetGroup(flashOverlay, 0f, false);
            SetGroup(fusionText, 0f, false);
            SetGroup(finalUI, 0f, false);
            skipHint?.SetActive(true);
        }

        private void ApplyFinalState()
        {
            isMerging = true;
            sequenceFinished = true;
            foreach (Transform core in careerCores)
            {
                core?.gameObject.SetActive(false);
            }

            fusionCore?.gameObject.SetActive(false);
            if (playerSilhouette != null)
            {
                playerSilhouette.localPosition = gateInsidePosition;
                playerSilhouette.gameObject.SetActive(false);
            }

            SetSilhouetteAlpha(0f);
            if (leftDoor != null)
            {
                leftDoor.localPosition = leftDoorClosedPosition + Vector3.left * doorOpenDistance;
            }

            if (rightDoor != null)
            {
                rightDoor.localPosition = rightDoorClosedPosition + Vector3.right * doorOpenDistance;
            }

            gateLightPanel?.SetActive(true);
            if (gateLight != null)
            {
                gateLight.enabled = true;
                gateLight.intensity = gateLightMaxIntensity;
            }

            SetGroup(flashOverlay, 0f, false);
            SetGroup(fusionText, 0f, false);
            SetGroup(finalUI, 1f, true);
            skipHint?.SetActive(false);
        }

        private void PrepareSilhouetteMaterials()
        {
            silhouetteMaterials = new Material[silhouetteRenderers.Length];
            silhouetteColors = new Color[silhouetteRenderers.Length];
            for (int index = 0; index < silhouetteRenderers.Length; index++)
            {
                Renderer target = silhouetteRenderers[index];
                if (target == null)
                {
                    continue;
                }

                Material material = target.material;
                silhouetteMaterials[index] = material;
                Color color = material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : material.color;
                silhouetteColors[index] = color;
                ConfigureTransparent(material);
            }
        }

        private void SetSilhouetteAlpha(float alpha)
        {
            if (silhouetteMaterials == null)
            {
                return;
            }

            for (int index = 0; index < silhouetteMaterials.Length; index++)
            {
                Material material = silhouetteMaterials[index];
                if (material == null)
                {
                    continue;
                }

                Color color = silhouetteColors[index];
                color.a = Mathf.Clamp01(alpha);
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }
            }
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(from, to, Smooth(elapsed / duration));
                yield return null;
            }

            target.localScale = to;
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            group.alpha = to;
        }

        private static IEnumerator Wait(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static float Smooth(float value)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
        }

        private static void SetGroup(CanvasGroup group, float alpha, bool interactive)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }
    }
}
