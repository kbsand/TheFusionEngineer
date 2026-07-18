using System.Collections;
using UnityEngine;

namespace TheFusionEngineer.Stage03
{
    public sealed class AGVPatrolController : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float moveSpeed = 0.8f;
        [SerializeField] private float turnSpeed = 4f;
        [SerializeField] private float waitDuration = 1.2f;
        [SerializeField] private float arrivalDistance = 0.12f;

        private int waypointIndex;
        private bool waiting;

        public Transform[] Waypoints => waypoints;

        private void Update()
        {
            if (waiting || waypoints == null || waypoints.Length == 0) return;
            Transform target = waypoints[waypointIndex];
            if (target == null) return;

            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                StartCoroutine(WaitForNextWaypoint());
                return;
            }

            Vector3 direction = offset.normalized;
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }

        private IEnumerator WaitForNextWaypoint()
        {
            waiting = true;
            yield return new WaitForSeconds(waitDuration);
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            waiting = false;
        }
    }
}
