using UnityEngine;

namespace TheFusionEngineer.Core
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerFallRecovery : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float fallThreshold = -5f;
        [SerializeField, Min(0f)] private float recoveryCooldown = 0.75f;

        private CharacterController characterController;
        private Vector3 fallbackPosition;
        private Quaternion fallbackRotation;
        private float nextRecoveryTime;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            fallbackPosition = transform.position;
            fallbackRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (transform.position.y >= fallThreshold || Time.unscaledTime < nextRecoveryTime)
            {
                return;
            }

            Recover();
        }

        private void Recover()
        {
            nextRecoveryTime = Time.unscaledTime + recoveryCooldown;

            Vector3 safePosition = respawnPoint != null ? respawnPoint.position : fallbackPosition;
            Quaternion safeRotation = respawnPoint != null ? respawnPoint.rotation : fallbackRotation;

            characterController.enabled = false;
            transform.SetPositionAndRotation(safePosition, safeRotation);
            characterController.enabled = true;

            Debug.LogWarning($"Player fell below Y {fallThreshold:0.##} and was returned to the Stage 01 respawn point.", this);
        }
    }
}
