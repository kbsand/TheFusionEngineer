using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheFusionEngineer.Core
{
    public sealed class SceneTransitionController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;

        private bool isTransitioning;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            if (fadePanel == null)
            {
                Debug.LogError($"[{nameof(SceneTransitionController)}] Fade Panel reference is missing.", this);
                return;
            }

            fadePanel.alpha = 1f;
            fadePanel.blocksRaycasts = true;
        }

        private IEnumerator Start()
        {
            if (fadePanel != null)
            {
                yield return Fade(1f, 0f);
                fadePanel.blocksRaycasts = false;
            }
        }

        public void LoadScene(string sceneName)
        {
            if (isTransitioning)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[{nameof(SceneTransitionController)}] Scene cannot be loaded: '{sceneName}'. Check Build Settings and the scene name.", this);
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        public void Configure(CanvasGroup panel, float duration)
        {
            fadePanel = panel;
            fadeDuration = Mathf.Max(0.01f, duration);
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isTransitioning = true;

            if (fadePanel != null)
            {
                fadePanel.blocksRaycasts = true;
                yield return Fade(fadePanel.alpha, 1f);
            }

            AsyncOperation loadOperation;
            try
            {
                loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(SceneTransitionController)}] Failed to load scene '{sceneName}': {exception.Message}", this);
                isTransitioning = false;
                yield break;
            }

            if (loadOperation == null)
            {
                Debug.LogError($"[{nameof(SceneTransitionController)}] Unity did not create a load operation for scene '{sceneName}'.", this);
                isTransitioning = false;
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadePanel.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            fadePanel.alpha = to;
        }
    }
}
