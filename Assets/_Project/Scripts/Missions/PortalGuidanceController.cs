using UnityEngine;

namespace TheFusionEngineer.Missions
{
    public sealed class PortalGuidanceController : MonoBehaviour
    {
        [SerializeField] private GameObject objectiveUI;
        [SerializeField] private Transform guidanceArrow;
        [SerializeField, Min(0f)] private float bobHeight = 0.25f;
        [SerializeField, Min(0.1f)] private float bobSpeed = 2f;

        private Vector3 arrowStartPosition;
        private bool isVisible;
        private bool hasBeenAcknowledged;

        public bool IsVisible => isVisible;
        public bool HasBeenAcknowledged => hasBeenAcknowledged;

        private void Awake()
        {
            if (guidanceArrow != null)
            {
                arrowStartPosition = guidanceArrow.localPosition;
            }

            SetVisible(false);
        }

        private void Update()
        {
            if (!isVisible || guidanceArrow == null)
            {
                return;
            }

            Vector3 position = arrowStartPosition;
            position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            guidanceArrow.localPosition = position;
        }

        public void Configure(GameObject objective, Transform arrow, float height, float speed)
        {
            objectiveUI = objective;
            guidanceArrow = arrow;
            bobHeight = height;
            bobSpeed = speed;
            arrowStartPosition = guidanceArrow != null ? guidanceArrow.localPosition : Vector3.zero;
            SetVisible(false);
        }

        public void ShowGuidance()
        {
            if (hasBeenAcknowledged)
            {
                return;
            }

            SetVisible(true);
        }

        public void MarkPortalReached()
        {
            if (hasBeenAcknowledged)
            {
                return;
            }

            hasBeenAcknowledged = true;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            if (objectiveUI != null)
            {
                objectiveUI.SetActive(visible);
            }

            if (guidanceArrow != null)
            {
                guidanceArrow.gameObject.SetActive(visible);
                if (!visible)
                {
                    guidanceArrow.localPosition = arrowStartPosition;
                }
            }
        }
    }
}
