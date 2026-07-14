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

        private InputAction interactAction;
        private bool wasInRange;

        private void Awake()
        {
            interactAction = inputActions?.FindAction("Player/Interact", true);
        }

        private void OnEnable()
        {
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
            plcMission?.SetPlayerInRange(false);
        }

        private void Update()
        {
            if (plcMission == null || plcMission.IsCompleted)
            {
                if (wasInRange)
                {
                    plcMission?.SetPlayerInRange(false);
                    wasInRange = false;
                }

                return;
            }

            Vector3 offset = plcMission.transform.position - transform.position;
            offset.y = 0f;
            bool isInRange = offset.sqrMagnitude <= interactionDistance * interactionDistance;

            if (isInRange != wasInRange)
            {
                plcMission.SetPlayerInRange(isInRange);
                wasInRange = isInRange;
            }

            if (isInRange && interactAction != null && interactAction.WasPressedThisFrame())
            {
                plcMission.TryCompleteMission();
            }
        }

        public void Configure(InputActionAsset actions, PLCMissionController mission, float distance)
        {
            inputActions = actions;
            plcMission = mission;
            interactionDistance = distance;
        }
    }
}
