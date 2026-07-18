using UnityEngine;

namespace TheFusionEngineer.Core
{
    /// <summary>
    /// 플레이어가 맵 아래로 추락하면 마지막 안전 위치 또는 지정 위치로 복귀시킵니다.
    /// </summary>
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

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            fallbackPosition = transform.position;
            fallbackRotation = transform.rotation;
        }

        // 다른 오브젝트의 이동이 끝난 뒤 후속 위치와 회전을 갱신합니다.
        private void LateUpdate()
        {
            if (transform.position.y >= fallThreshold || Time.unscaledTime < nextRecoveryTime)
            {
                return;
            }

            Recover();
        }

        // Recover 관련 게임 로직을 수행합니다.
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
