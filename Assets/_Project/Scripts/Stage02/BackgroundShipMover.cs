using UnityEngine;

namespace TheFusionEngineer.Stage02
{
    public sealed class BackgroundShipMover : MonoBehaviour
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField, Min(0.01f)] private float speed = 0.35f;
        [SerializeField, Min(0.01f)] private float turnSpeed = 0.6f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.25f;
        [SerializeField] private bool startTowardB = true;

        private Transform currentTarget;
        private float waterlineY;

        private void OnEnable()
        {
            waterlineY = transform.position.y;
            currentTarget = startTowardB ? pointB : pointA;
        }

        private void Update()
        {
            if (pointA == null || pointB == null || currentTarget == null)
            {
                return;
            }

            Vector3 targetPosition = currentTarget.position;
            targetPosition.y = waterlineY;

            Vector3 direction = targetPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                currentTarget = currentTarget == pointA ? pointB : pointA;
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime);

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime);
            }
        }
    }
}
