using TheFusionEngineer.Core;
using UnityEngine;

namespace TheFusionEngineer.Ending
{
    public sealed class PortfolioButtonController : MonoBehaviour
    {
        [SerializeField] private string resumeUrl = "";
        [SerializeField] private string githubUrl = "";
        [SerializeField] private string emailAddress = "";
        [SerializeField] private SceneTransitionController sceneTransition;

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

        public void OpenResume()
        {
            OpenUrl(resumeUrl, "Resume URL");
        }

        public void OpenGithub()
        {
            OpenUrl(githubUrl, "GitHub URL");
        }

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

        public void ReplayCareer()
        {
            if (sceneTransition == null)
            {
                Debug.LogError("[Ending Portfolio] SceneTransitionController reference is missing.", this);
                return;
            }

            sceneTransition.LoadScene("Stage01_Origin");
        }

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
