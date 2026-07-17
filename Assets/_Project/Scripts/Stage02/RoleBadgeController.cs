using UnityEngine;
using UnityEngine.UI;

namespace TheFusionEngineer.Stage02
{
    public sealed class RoleBadgeController : MonoBehaviour
    {
        [SerializeField] private Text badgeText;
        [SerializeField] private string missionARole = "직무: FULL-STACK DEVELOPER";
        [SerializeField] private string missionBRole = "직무: BACKEND DEVELOPER";

        public void Configure(Text target, string roleA, string roleB)
        {
            badgeText = target;
            missionARole = roleA;
            missionBRole = roleB;
        }

        public void ShowMissionARole()
        {
            if (badgeText != null)
            {
                badgeText.text = missionARole;
            }
        }

        public void ShowMissionBRole()
        {
            if (badgeText != null)
            {
                badgeText.text = missionBRole;
            }
        }
    }
}
