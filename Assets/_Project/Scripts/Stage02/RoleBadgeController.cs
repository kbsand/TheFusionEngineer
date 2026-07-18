using UnityEngine;
using UnityEngine.UI;

namespace TheFusionEngineer.Stage02
{
    /// <summary>
    /// Stage2에서 현재 획득한 직무 정보를 HUD 배지에 표시합니다.
    /// </summary>
    public sealed class RoleBadgeController : MonoBehaviour
    {
        [SerializeField] private Text badgeText;
        [SerializeField] private string missionARole = "직무: FULL-STACK DEVELOPER";
        [SerializeField] private string missionBRole = "직무: BACKEND DEVELOPER";

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(Text target, string roleA, string roleB)
        {
            badgeText = target;
            missionARole = roleA;
            missionBRole = roleB;
        }

        /// <summary>
        /// 미션 A 완료로 획득한 직무를 HUD에 표시합니다.
        /// </summary>
        public void ShowMissionARole()
        {
            if (badgeText != null)
            {
                badgeText.text = missionARole;
            }
        }

        /// <summary>
        /// 미션 B 완료로 확장된 직무를 HUD에 표시합니다.
        /// </summary>
        public void ShowMissionBRole()
        {
            if (badgeText != null)
            {
                badgeText.text = missionBRole;
            }
        }
    }
}
