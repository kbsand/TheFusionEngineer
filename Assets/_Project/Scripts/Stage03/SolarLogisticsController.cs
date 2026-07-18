using UnityEngine;

namespace TheFusionEngineer.Stage03
{
    /// <summary>
    /// 태양광 물류 설비의 반복 이동과 회전 연출을 제어합니다.
    /// </summary>
    public sealed class SolarLogisticsController : MonoBehaviour
    {
        [SerializeField] private Transform[] cargo = System.Array.Empty<Transform>();
        [SerializeField] private GameObject[] serverIndicators = System.Array.Empty<GameObject>();
        [SerializeField] private Transform failoverSwitch;
        [SerializeField, Min(0f)] private float cargoSpeed = 2.2f;
        [SerializeField] private float loopStartZ = -3.5f;
        [SerializeField] private float loopEndZ = 7.5f;
        [SerializeField] private Vector3 failoverEnabledRotation = new(0f, 0f, -35f);

        private bool isRunning;

        public bool IsRunning => isRunning;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            foreach (GameObject indicator in serverIndicators)
            {
                if (indicator != null)
                {
                    indicator.SetActive(false);
                }
            }
        }

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            foreach (Transform item in cargo)
            {
                if (item == null)
                {
                    continue;
                }

                Vector3 position = item.localPosition;
                position.z += cargoSpeed * Time.deltaTime;
                if (position.z > loopEndZ)
                {
                    position.z = loopStartZ + position.z - loopEndZ;
                }

                item.localPosition = position;
            }
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(
            Transform[] cargoItems,
            GameObject[] indicators,
            Transform switchTransform,
            float startZ,
            float endZ)
        {
            cargo = cargoItems ?? System.Array.Empty<Transform>();
            serverIndicators = indicators ?? System.Array.Empty<GameObject>();
            failoverSwitch = switchTransform;
            loopStartZ = startZ;
            loopEndZ = endZ;
        }

        /// <summary>
        /// 태양광 물류 시스템을 가동 상태로 전환합니다.
        /// 이미 실행 중이면 중복 호출을 무시하고 서버 표시등과 Failover 스위치를 함께 변경합니다.
        /// </summary>
        public void StartLogistics()
        {
            // 미션 이벤트가 여러 번 전달돼도 같은 연출을 다시 시작하지 않도록 보호합니다.
            if (isRunning)
            {
                return;
            }

            isRunning = true;

            // 연결된 서버가 가동됐다는 상태를 플레이어에게 시각적으로 보여줍니다.
            foreach (GameObject indicator in serverIndicators)
            {
                if (indicator != null)
                {
                    indicator.SetActive(true);
                }
            }

            // 선택적으로 연결된 Failover 스위치를 활성화 방향으로 전환합니다.
            if (failoverSwitch != null)
            {
                failoverSwitch.localRotation = Quaternion.Euler(failoverEnabledRotation);
            }
        }
    }
}
