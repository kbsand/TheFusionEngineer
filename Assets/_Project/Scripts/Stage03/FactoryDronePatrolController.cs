using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheFusionEngineer.Stage03
{
    /// <summary>
    /// 공장 AGV 웨이포인트를 공중 고도로 순환하는 무인 드론 이동을 담당합니다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FactoryDronePatrolController : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints = System.Array.Empty<Transform>();
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.2f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 4.5f;
        [SerializeField, Min(0f)] private float waypointWaitDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float arrivalDistance = 0.25f;
        [SerializeField] private float flightAltitude = 7.9f;
        [SerializeField, Min(0f)] private float hoverHeight = 0.18f;
        [SerializeField, Min(0.1f)] private float hoverSpeed = 2.2f;

        private Rigidbody droneBody;
        private Renderer droneRenderer;
        private GameObject labelRoot;
        private Camera targetCamera;
        private Vector3 routePosition;
        private int waypointIndex;
        private float waitTimer;
        private float hoverPhase;

        private void Reset()
        {
            ConfigurePhysics();
        }

        private void Awake()
        {
            ResolveWaypoints();
            droneRenderer = GetComponentInChildren<Renderer>(true);
            ConfigurePhysics();

            routePosition = transform.position;
            routePosition.y = flightAltitude;
            hoverPhase = Random.Range(0f, Mathf.PI * 2f);
            waypointIndex = FindNearestWaypointIndex();
            CreateDroneLabel();
        }

        private void ResolveWaypoints()
        {
            if (waypoints != null)
            {
                foreach (Transform waypoint in waypoints)
                {
                    if (waypoint != null)
                    {
                        return;
                    }
                }
            }

            AGVPatrolController agv = FindAnyObjectByType<AGVPatrolController>();
            waypoints = agv != null
                ? agv.Waypoints
                : System.Array.Empty<Transform>();
        }

        private void OnDestroy()
        {
            if (labelRoot != null)
            {
                Destroy(labelRoot);
            }
        }

        private void FixedUpdate()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= Time.fixedDeltaTime;
                ApplyHoverPosition();
                return;
            }

            Transform target = waypoints[waypointIndex];
            if (target == null)
            {
                AdvanceWaypoint();
                return;
            }

            Vector3 targetPosition = new(
                target.position.x,
                flightAltitude,
                target.position.z);
            Vector3 offset = targetPosition - routePosition;
            offset.y = 0f;
            if (offset.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                routePosition = targetPosition;
                waitTimer = waypointWaitDuration;
                AdvanceWaypoint();
                ApplyHoverPosition();
                return;
            }

            Vector3 direction = offset.normalized;
            routePosition = Vector3.MoveTowards(
                routePosition,
                targetPosition,
                moveSpeed * Time.fixedDeltaTime);
            ApplyHoverPosition();

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                Quaternion rotation = Quaternion.Slerp(
                    droneBody.rotation,
                    targetRotation,
                    turnSpeed * Time.fixedDeltaTime);
                droneBody.MoveRotation(rotation);
            }
        }

        private void LateUpdate()
        {
            if (labelRoot == null || droneRenderer == null)
            {
                return;
            }

            Bounds bounds = droneRenderer.bounds;
            labelRoot.transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + 0.42f,
                bounds.center.z);

            targetCamera ??= Camera.main;
            if (targetCamera != null)
            {
                Vector3 forward = labelRoot.transform.position - targetCamera.transform.position;
                if (forward.sqrMagnitude > 0.001f)
                {
                    labelRoot.transform.rotation = Quaternion.LookRotation(
                        forward.normalized,
                        Vector3.up);
                }
            }
        }

        private void ConfigurePhysics()
        {
            droneBody = GetComponent<Rigidbody>();
            if (droneBody == null)
            {
                return;
            }

            droneBody.isKinematic = true;
            droneBody.useGravity = false;
            droneBody.interpolation = RigidbodyInterpolation.Interpolate;
            droneBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private void ApplyHoverPosition()
        {
            float hoverOffset = Mathf.Sin(
                Time.fixedTime * hoverSpeed + hoverPhase) * hoverHeight;
            droneBody.MovePosition(routePosition + Vector3.up * hoverOffset);
        }

        private void AdvanceWaypoint()
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }

        private int FindNearestWaypointIndex()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                return 0;
            }

            int nearestIndex = 0;
            float nearestDistance = float.PositiveInfinity;
            Vector2 currentPosition = new(transform.position.x, transform.position.z);
            for (int index = 0; index < waypoints.Length; index++)
            {
                Transform waypoint = waypoints[index];
                if (waypoint == null)
                {
                    continue;
                }

                Vector2 waypointPosition = new(waypoint.position.x, waypoint.position.z);
                float distance = Vector2.SqrMagnitude(waypointPosition - currentPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }

        private void CreateDroneLabel()
        {
            labelRoot = new GameObject(
                "Drone Label",
                typeof(RectTransform),
                typeof(Canvas));
            SceneManager.MoveGameObjectToScene(labelRoot, gameObject.scene);

            RectTransform labelRect = labelRoot.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(220f, 64f);
            labelRect.localScale = Vector3.one * 0.0045f;

            Canvas canvas = labelRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 25;

            GameObject backgroundObject = new(
                "Background",
                typeof(RectTransform),
                typeof(Image));
            backgroundObject.transform.SetParent(labelRoot.transform, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0.015f, 0.05f, 0.08f, 0.88f);
            background.raycastTarget = false;

            GameObject textObject = new(
                "Drone Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(backgroundRect, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "Drone";
            text.fontSize = 42f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.3f, 1f, 0.95f);
            text.raycastTarget = false;
        }
    }
}
