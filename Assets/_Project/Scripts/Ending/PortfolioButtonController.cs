using TheFusionEngineer.Core;
using UnityEngine;

namespace TheFusionEngineer.Ending
{
    /// <summary>
    /// 엔딩 화면의 포트폴리오, 메일 보내기, 게임 재시작 버튼 동작을 연결합니다.
    /// </summary>
    public sealed class PortfolioButtonController : MonoBehaviour
    {
        [SerializeField] private string resumeUrl = "";
        [SerializeField] private string githubUrl = "";
        [SerializeField] private string emailAddress = "";
        [SerializeField] private SceneTransitionController sceneTransition;

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(
            SceneTransitionController transition,
            string resume,
            string github,
            string email)
        {
            sceneTransition = transition;
            resumeUrl = resume;
            githubUrl = github;
            emailAddress = email;
        }

        // OpenResume 관련 게임 로직을 수행합니다.
        public void OpenResume()
        {
            OpenUrl(resumeUrl, "Resume URL");
        }

        // OpenGithub 관련 게임 로직을 수행합니다.
        public void OpenGithub()
        {
            OpenUrl(githubUrl, "GitHub URL");
        }

        // OpenContact 관련 게임 로직을 수행합니다.
        public void OpenContact()
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                Debug.LogWarning("[Ending Portfolio] Contact email is empty.", this);
                return;
            }

            string target = emailAddress.StartsWith("mailto:")
                ? emailAddress
                : $"mailto:{emailAddress}";
            Application.OpenURL(target);
        }

        // ReplayCareer 관련 게임 로직을 수행합니다.
        public void ReplayCareer()
        {
            if (sceneTransition == null)
            {
                Debug.LogError("[Ending Portfolio] SceneTransitionController reference is missing.", this);
                return;
            }

            sceneTransition.LoadScene("Stage01_Origin");
        }

        // OpenUrl 관련 게임 로직을 수행합니다.
        private void OpenUrl(string url, string label)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning($"[Ending Portfolio] {label} is empty.", this);
                return;
            }

            Application.OpenURL(url);
        }
    }
}
