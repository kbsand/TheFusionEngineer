using TheFusionEngineer.Missions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheFusionEngineer.Player
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PLCMissionController plcMission;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;

        private void OnDisable()
        {
            plcMission?.SetPlayerInRange(false);
        }

        public void Configure(InputActionAsset actions, PLCMissionController mission, float distance)
        {
            inputActions = actions;
            plcMission = mission;
            interactionDistance = distance;
        }
    }
}
