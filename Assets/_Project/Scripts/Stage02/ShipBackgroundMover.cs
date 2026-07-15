using UnityEngine;

namespace TheFusionEngineer.Stage02
{
    public sealed class ShipBackgroundMover : MonoBehaviour
    {
        [SerializeField] private Vector3 movementAxis = Vector3.right;
        [SerializeField, Min(0f)] private float travelDistance = 3f;
        [SerializeField, Min(0f)] private float movementSpeed = 0.12f;

        private Vector3 origin;

        private void Awake()
        {
            origin = transform.localPosition;
        }

        private void Update()
        {
            Vector3 axis = movementAxis.sqrMagnitude > 0.001f
                ? movementAxis.normalized
                : Vector3.right;

            float offset = Mathf.Sin(Time.time * movementSpeed) * travelDistance;
            transform.localPosition = origin + axis * offset;
        }
    }
}
