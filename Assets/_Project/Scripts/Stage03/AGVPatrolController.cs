using System.Collections;
using UnityEngine;

namespace TheFusionEngineer.Stage03
{
    /// <summary>
    /// Stage3 AGV가 지정 경로 지점을 순환하도록 이동시킵니다.
    /// </summary>
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

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
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

        // WaitForNextWaypoint 관련 게임 로직을 수행합니다.
        private IEnumerator WaitForNextWaypoint()
        {
            waiting = true;
            // WaitForSeconds 관련 게임 로직을 수행합니다.
            yield return new WaitForSeconds(waitDuration);
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            waiting = false;
        }
    }
}
