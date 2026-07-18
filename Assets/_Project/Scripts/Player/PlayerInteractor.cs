using TheFusionEngineer.Missions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheFusionEngineer.Player
{
    /// <summary>
    /// 플레이어 주변의 상호작용 대상을 탐색하고 사용 입력을 전달합니다.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PLCMissionController plcMission;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;

        // Unity가 컴포넌트를 비활성화할 때 입력과 이벤트 연결을 정리합니다.
        private void OnDisable()
        {
            plcMission?.SetPlayerInRange(false);
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(InputActionAsset actions, PLCMissionController mission, float distance)
        {
            inputActions = actions;
            plcMission = mission;
            interactionDistance = distance;
        }
    }
}
