using UnityEngine;

namespace TheFusionEngineer.UI
{
    /// <summary>
    /// 월드 공간 UI가 카메라를 향하되 Y축으로만 회전하도록 유지합니다.
    /// 캐릭터나 설비 위에 표시되는 이름표에 사용합니다.
    /// </summary>
    public sealed class BillboardYController : MonoBehaviour
    {
        [SerializeField] private Transform targetCamera;

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(Transform cameraTransform)
        {
            targetCamera = cameraTransform;
        }

        // 다른 오브젝트의 이동이 끝난 뒤 후속 위치와 회전을 갱신합니다.
        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                Camera mainCamera = Camera.main;
                targetCamera = mainCamera != null ? mainCamera.transform : null;
            }

            if (targetCamera == null)
            {
                return;
            }

            Vector3 lookDirection = transform.position - targetCamera.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
        }
    }
}
