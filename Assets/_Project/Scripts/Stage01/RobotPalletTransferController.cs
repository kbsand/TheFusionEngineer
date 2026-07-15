using System.Collections;
using System.Collections.Generic;
using TheFusionEngineer.Missions;
using UnityEngine;

namespace TheFusionEngineer.Stage01
{
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

        private void OnEnable()
        {
            if (monitorRoutine == null)
            {
                monitorRoutine = StartCoroutine(MonitorTransfers());
            }
        }

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
                        yield return TransferCargo(candidate, palletSlots[nextSlotIndex]);
                    }
                }

                yield return wait;
            }
        }

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
            yield return RotateRobotTowards(pickPoint.position, rotateDuration);
            yield return PoseJoints(true, rotateDuration);
            yield return MoveControlledCargo(gripper.position, gripper.rotation, pickupDuration);

            cargo.SetParent(gripper, true);
            controlledPosition = gripper.position;
            controlledRotation = gripper.rotation;
            SetIndicatorColor(new Color(0.05f, 1f, 0.55f));

            yield return MoveControlledCargo(carryPoint.position, carryPoint.rotation, pickupDuration);
            yield return RotateRobotTowards(slot.position, rotateDuration);
            yield return MoveControlledCargo(slot.position, slot.rotation, placeDuration);

            cargo.SetParent(slot, true);
            cargo.SetPositionAndRotation(slot.position, slot.rotation);
            placedCargo[cargo] = slot;
            nextSlotIndex++;
            completedSingleCycle = true;
            activeCargo = null;

            SetIndicatorColor(Color.white);
            yield return PoseJoints(false, rotateDuration);
            yield return RotateRobotTo(initialRobotRotation, rotateDuration);

            transferInProgress = false;
            if (cycleDelay > 0f)
            {
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        private IEnumerator RotateRobotTowards(Vector3 target, float duration)
        {
            Vector3 direction = target - robotRoot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                yield break;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            yield return RotateRobotTo(targetRotation, duration);
        }

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
