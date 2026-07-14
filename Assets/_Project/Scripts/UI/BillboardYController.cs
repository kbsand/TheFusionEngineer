using UnityEngine;

namespace TheFusionEngineer.UI
{
    public sealed class BillboardYController : MonoBehaviour
    {
        [SerializeField] private Transform targetCamera;

        public void Configure(Transform cameraTransform)
        {
            targetCamera = cameraTransform;
        }

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
