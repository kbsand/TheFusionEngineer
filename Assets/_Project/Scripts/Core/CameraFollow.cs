using UnityEngine;

namespace TheFusionEngineer.Core
{
    /// <summary>
    /// 카메라가 플레이어를 일정한 거리와 각도로 부드럽게 따라가도록 제어합니다.
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(10f, 12f, -10f);
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.18f;
        [SerializeField, Min(0f)] private float rotationSharpness = 12f;
        [SerializeField, Min(0f)] private float lookHeight = 1f;

        private Vector3 followVelocity;

        // Unity가 첫 프레임 전에 게임 진행 상태를 초기화합니다.
        private void Start()
        {
            SnapToTarget();
        }

        // 다른 오브젝트의 이동이 끝난 뒤 후속 위치와 회전을 갱신합니다.
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref followVelocity,
                positionSmoothTime);

            Vector3 lookDirection = target.position + Vector3.up * lookHeight - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
            }
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(Transform followTarget, Vector3 cameraOffset)
        {
            target = followTarget;
            offset = cameraOffset;
        }

        // SnapToTarget 관련 게임 로직을 수행합니다.
        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
