using System.Collections;
using System.Collections.Generic;
using TheFusionEngineer.Missions;
using UnityEngine;

namespace TheFusionEngineer.Stage01
{
    /// <summary>
    /// Stage1 로봇이 팔레트를 집어 지정 위치로 옮기는 반복 연출을 제어합니다.
    /// </summary>
    public sealed class RobotPalletTransferController : MonoBehaviour
    {
        [Header("Robot")]
        [SerializeField] private Transform robotRoot;
        [SerializeField] private Transform baseJoint;
        [SerializeField] private Transform shoulderJoint;
        [SerializeField] private Transform elbowJoint;
        [SerializeField] private Transform wristJoint;
        [SerializeField] private Transform gripper;

        [Header("Transfer Points")]
        [SerializeField] private Transform pickPoint;
        [SerializeField] private Transform carryPoint;
        [SerializeField] private Transform[] palletSlots = System.Array.Empty<Transform>();
        [SerializeField] private Transform[] cargoCandidates = System.Array.Empty<Transform>();
        [SerializeField, Min(0.1f)] private float pickupRadius = 0.9f;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float rotateDuration = 0.6f;
        [SerializeField, Min(0.01f)] private float pickupDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float placeDuration = 0.7f;
        [SerializeField, Min(0f)] private float cycleDelay = 0.8f;
        [SerializeField] private bool repeatCycle = true;

        [Header("Mission and Indicator")]
        [SerializeField] private PLCMissionController plcMission;
        [SerializeField] private Renderer gripperIndicator;
        [SerializeField, Min(0.05f)] private float scanInterval = 0.2f;

        private readonly HashSet<Transform> processedCargo = new HashSet<Transform>();
        private readonly Dictionary<Transform, Transform> placedCargo = new Dictionary<Transform, Transform>();
        private MaterialPropertyBlock indicatorProperties;
        private Coroutine monitorRoutine;
        private Transform activeCargo;
        private Vector3 controlledPosition;
        private Quaternion controlledRotation;
        private Quaternion initialRobotRotation;
        private Quaternion initialBaseRotation;
        private Quaternion initialShoulderRotation;
        private Quaternion initialElbowRotation;
        private Quaternion initialWristRotation;
        private int nextSlotIndex;
        private bool transferInProgress;
        private bool completedSingleCycle;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            if (robotRoot != null) initialRobotRotation = robotRoot.rotation;
            if (baseJoint != null) initialBaseRotation = baseJoint.localRotation;
            if (shoulderJoint != null) initialShoulderRotation = shoulderJoint.localRotation;
            if (elbowJoint != null) initialElbowRotation = elbowJoint.localRotation;
            if (wristJoint != null) initialWristRotation = wristJoint.localRotation;
            indicatorProperties = new MaterialPropertyBlock();
            SetIndicatorColor(Color.white);
        }

        // Unity가 컴포넌트를 활성화할 때 입력과 이벤트 연결을 시작합니다.
        private void OnEnable()
        {
            if (monitorRoutine == null)
            {
                monitorRoutine = StartCoroutine(MonitorTransfers());
            }
        }

        // Unity가 컴포넌트를 비활성화할 때 입력과 이벤트 연결을 정리합니다.
        private void OnDisable()
        {
            if (monitorRoutine != null)
            {
                StopCoroutine(monitorRoutine);
                monitorRoutine = null;
            }

            transferInProgress = false;
            activeCargo = null;
            SetIndicatorColor(Color.white);
        }

        // 다른 오브젝트의 이동이 끝난 뒤 후속 위치와 회전을 갱신합니다.
        private void LateUpdate()
        {
            if (activeCargo != null && transferInProgress)
            {
                activeCargo.SetPositionAndRotation(controlledPosition, controlledRotation);
            }

            foreach (KeyValuePair<Transform, Transform> pair in placedCargo)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    pair.Key.SetPositionAndRotation(pair.Value.position, pair.Value.rotation);
                }
            }
        }

        // MonitorTransfers 관련 게임 로직을 수행합니다.
        private IEnumerator MonitorTransfers()
        {
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.05f, scanInterval));
            while (true)
            {
                if (!transferInProgress && IsReadyForTransfer())
                {
                    Transform candidate = FindClosestCargo();
                    if (candidate != null)
                    {
                        // TransferCargo 관련 게임 로직을 수행합니다.
                        yield return TransferCargo(candidate, palletSlots[nextSlotIndex]);
                    }
                }

                yield return wait;
            }
        }

        // 필요한 실행 조건을 검사하고 조건을 만족할 때만 동작을 수행합니다.
        private bool IsReadyForTransfer()
        {
            if (plcMission == null || !plcMission.IsCompleted || pickPoint == null || carryPoint == null)
            {
                return false;
            }

            if (palletSlots == null || nextSlotIndex >= palletSlots.Length)
            {
                return false;
            }

            return repeatCycle || !completedSingleCycle;
        }

        // FindClosestCargo 관련 게임 로직을 수행합니다.
        private Transform FindClosestCargo()
        {
            Transform closest = null;
            float closestSqrDistance = pickupRadius * pickupRadius;
            foreach (Transform candidate in cargoCandidates)
            {
                if (candidate == null || !candidate.gameObject.activeInHierarchy || processedCargo.Contains(candidate))
                {
                    continue;
                }

                float sqrDistance = (candidate.position - pickPoint.position).sqrMagnitude;
                if (sqrDistance <= closestSqrDistance)
                {
                    closest = candidate;
                    closestSqrDistance = sqrDistance;
                }
            }

            return closest;
        }

        // TransferCargo 관련 게임 로직을 수행합니다.
        private IEnumerator TransferCargo(Transform cargo, Transform slot)
        {
            if (cargo == null || slot == null || gripper == null || robotRoot == null)
            {
                yield break;
            }

            transferInProgress = true;
            activeCargo = cargo;
            processedCargo.Add(cargo);
            controlledPosition = cargo.position;
            controlledRotation = cargo.rotation;

            SetIndicatorColor(new Color(1f, 0.22f, 0.02f));
            // RotateRobotTowards 관련 게임 로직을 수행합니다.
            yield return RotateRobotTowards(pickPoint.position, rotateDuration);
            // PoseJoints 관련 게임 로직을 수행합니다.
            yield return PoseJoints(true, rotateDuration);
            // MoveControlledCargo 관련 게임 로직을 수행합니다.
            yield return MoveControlledCargo(gripper.position, gripper.rotation, pickupDuration);

            cargo.SetParent(gripper, true);
            controlledPosition = gripper.position;
            controlledRotation = gripper.rotation;
            SetIndicatorColor(new Color(0.05f, 1f, 0.55f));

            // MoveControlledCargo 관련 게임 로직을 수행합니다.
            yield return MoveControlledCargo(carryPoint.position, carryPoint.rotation, pickupDuration);
            // RotateRobotTowards 관련 게임 로직을 수행합니다.
            yield return RotateRobotTowards(slot.position, rotateDuration);
            // MoveControlledCargo 관련 게임 로직을 수행합니다.
            yield return MoveControlledCargo(slot.position, slot.rotation, placeDuration);

            cargo.SetParent(slot, true);
            cargo.SetPositionAndRotation(slot.position, slot.rotation);
            placedCargo[cargo] = slot;
            nextSlotIndex++;
            completedSingleCycle = true;
            activeCargo = null;

            SetIndicatorColor(Color.white);
            // PoseJoints 관련 게임 로직을 수행합니다.
            yield return PoseJoints(false, rotateDuration);
            // RotateRobotTo 관련 게임 로직을 수행합니다.
            yield return RotateRobotTo(initialRobotRotation, rotateDuration);

            transferInProgress = false;
            if (cycleDelay > 0f)
            {
                // WaitForSeconds 관련 게임 로직을 수행합니다.
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        // RotateRobotTowards 관련 게임 로직을 수행합니다.
        private IEnumerator RotateRobotTowards(Vector3 target, float duration)
        {
            Vector3 direction = target - robotRoot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                yield break;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            // RotateRobotTo 관련 게임 로직을 수행합니다.
            yield return RotateRobotTo(targetRotation, duration);
        }

        // RotateRobotTo 관련 게임 로직을 수행합니다.
        private IEnumerator RotateRobotTo(Quaternion targetRotation, float duration)
        {
            Quaternion start = robotRoot.rotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                robotRoot.rotation = Quaternion.Slerp(start, targetRotation, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            robotRoot.rotation = targetRotation;
        }

        // PoseJoints 관련 게임 로직을 수행합니다.
        private IEnumerator PoseJoints(bool picking, float duration)
        {
            Quaternion baseTarget = initialBaseRotation * Quaternion.Euler(0f, picking ? 12f : 0f, 0f);
            Quaternion shoulderTarget = initialShoulderRotation * Quaternion.Euler(picking ? 28f : 0f, 0f, 0f);
            Quaternion elbowTarget = initialElbowRotation * Quaternion.Euler(picking ? -42f : 0f, 0f, 0f);
            Quaternion wristTarget = initialWristRotation * Quaternion.Euler(picking ? 18f : 0f, 0f, 0f);
            Quaternion baseStart = baseJoint != null ? baseJoint.localRotation : Quaternion.identity;
            Quaternion shoulderStart = shoulderJoint != null ? shoulderJoint.localRotation : Quaternion.identity;
            Quaternion elbowStart = elbowJoint != null ? elbowJoint.localRotation : Quaternion.identity;
            Quaternion wristStart = wristJoint != null ? wristJoint.localRotation : Quaternion.identity;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (baseJoint != null) baseJoint.localRotation = Quaternion.Slerp(baseStart, baseTarget, t);
                if (shoulderJoint != null) shoulderJoint.localRotation = Quaternion.Slerp(shoulderStart, shoulderTarget, t);
                if (elbowJoint != null) elbowJoint.localRotation = Quaternion.Slerp(elbowStart, elbowTarget, t);
                if (wristJoint != null) wristJoint.localRotation = Quaternion.Slerp(wristStart, wristTarget, t);
                yield return null;
            }
        }

        // MoveControlledCargo 관련 게임 로직을 수행합니다.
        private IEnumerator MoveControlledCargo(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            Vector3 startPosition = controlledPosition;
            Quaternion startRotation = controlledRotation;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                controlledPosition = Vector3.Lerp(startPosition, targetPosition, t);
                controlledRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            controlledPosition = targetPosition;
            controlledRotation = targetRotation;
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetIndicatorColor(Color color)
        {
            if (gripperIndicator == null || indicatorProperties == null)
            {
                return;
            }

            gripperIndicator.GetPropertyBlock(indicatorProperties);
            indicatorProperties.SetColor("_BaseColor", color);
            indicatorProperties.SetColor("_Color", color);
            gripperIndicator.SetPropertyBlock(indicatorProperties);
        }
    }
}
