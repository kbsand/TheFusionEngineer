using UnityEngine;

namespace TheFusionEngineer.Missions
{
    /// <summary>
    /// 포탈이 활성화되었을 때 목표 UI와 안내 화살표의 표시 상태 및 움직임을 관리합니다.
    /// </summary>
    public sealed class PortalGuidanceController : MonoBehaviour
    {
        // 포탈로 이동하라는 목표를 보여 주는 UI 오브젝트입니다.
        [SerializeField] private GameObject objectiveUI;

        // 포탈 위에서 위아래로 움직일 안내 화살표의 Transform입니다.
        [SerializeField] private Transform guidanceArrow;

        // 화살표가 시작 위치를 기준으로 위아래로 움직이는 최대 높이입니다.
        [SerializeField, Min(0f)] private float bobHeight = 0.25f;

        // 화살표가 위아래로 움직이는 속도입니다.
        [SerializeField, Min(0.1f)] private float bobSpeed = 2f;

        // 숨김 처리 후 화살표를 원래 위치로 되돌리기 위한 초기 로컬 위치입니다.
        private Vector3 arrowStartPosition;

        // 현재 목표 UI와 안내 화살표가 표시 중인지 나타냅니다.
        private bool isVisible;

        // 플레이어가 포탈 안내를 이미 확인했는지 나타냅니다.
        private bool hasBeenAcknowledged;

        // 외부에서 현재 안내 표시 상태를 읽을 수 있도록 제공합니다.
        public bool IsVisible => isVisible;

        // 외부에서 포탈 안내 확인 여부를 읽을 수 있도록 제공합니다.
        public bool HasBeenAcknowledged => hasBeenAcknowledged;

        // 오브젝트가 생성될 때 화살표의 초기 위치를 저장하고 안내를 숨깁니다.
        private void Awake()
        {
            if (guidanceArrow != null)
            {
                arrowStartPosition = guidanceArrow.localPosition;
            }

            // 포탈이 실제로 해금되기 전에는 안내가 보이지 않도록 초기화합니다.
            SetVisible(false);
        }

        // 안내가 표시되는 동안 매 프레임 화살표의 상하 움직임을 갱신합니다.
        private void Update()
        {
            // 안내가 숨겨져 있거나 화살표가 연결되지 않았다면 갱신하지 않습니다.
            if (!isVisible || guidanceArrow == null)
            {
                return;
            }

            // Sin 파형을 사용하여 초기 위치를 중심으로 부드럽게 왕복시킵니다.
            Vector3 position = arrowStartPosition;
            position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            guidanceArrow.localPosition = position;
        }

        /// <summary>
        /// 런타임에서 목표 UI, 화살표 및 상하 움직임 설정을 주입합니다.
        /// 설정 직후 안내는 숨김 상태로 초기화됩니다.
        /// </summary>
        public void Configure(GameObject objective, Transform arrow, float height, float speed)
        {
            objectiveUI = objective;
            guidanceArrow = arrow;
            bobHeight = height;
            bobSpeed = speed;
            arrowStartPosition = guidanceArrow != null ? guidanceArrow.localPosition : Vector3.zero;
            SetVisible(false);
        }

        /// <summary>
        /// 아직 확인하지 않은 포탈 안내를 화면에 표시합니다.
        /// </summary>
        public void ShowGuidance()
        {
            // 한 번 확인한 안내는 다시 표시하지 않습니다.
            if (hasBeenAcknowledged)
            {
                return;
            }

            SetVisible(true);
        }

        /// <summary>
        /// 플레이어가 포탈 안내를 확인한 것으로 기록하고 안내를 숨깁니다.
        /// </summary>
        public void MarkPortalReached()
        {
            // 이미 처리된 경우 중복으로 상태를 변경하지 않습니다.
            if (hasBeenAcknowledged)
            {
                return;
            }

            hasBeenAcknowledged = true;
            SetVisible(false);
        }

        // 목표 UI와 안내 화살표의 활성 상태를 한 번에 변경합니다.
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
                    // 다음 표시 시 움직임이 항상 같은 위치에서 시작되도록 복원합니다.
                    guidanceArrow.localPosition = arrowStartPosition;
                }
            }
        }
    }
}
