using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheFusionEngineer.Core
{
    /// <summary>
    /// 화면 페이드와 씬 로딩을 한곳에서 처리하고 중복 전환을 방지합니다.
    /// </summary>
    public sealed class SceneTransitionController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;

        private bool isTransitioning;

        public bool IsTransitioning => isTransitioning;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
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

        // Unity가 첫 프레임 전에 게임 진행 상태를 초기화합니다.
        private IEnumerator Start()
        {
            if (fadePanel != null)
            {
                // Fade 관련 게임 로직을 수행합니다.
                yield return Fade(1f, 0f);
                fadePanel.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// 중복 요청을 막은 뒤 화면을 페이드아웃하고 지정한 씬을 비동기로 불러옵니다.
        /// </summary>
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

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(CanvasGroup panel, float duration)
        {
            fadePanel = panel;
            fadeDuration = Mathf.Max(0.01f, duration);
        }

        // LoadSceneRoutine 관련 게임 로직을 수행합니다.
        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isTransitioning = true;

            if (fadePanel != null)
            {
                fadePanel.blocksRaycasts = true;
                // Fade 관련 게임 로직을 수행합니다.
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

        // Fade 관련 게임 로직을 수행합니다.
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
