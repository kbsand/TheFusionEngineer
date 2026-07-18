using UnityEngine;

namespace TheFusionEngineer.Missions
{
    /// <summary>
    /// 미션 상태에 따라 컨베이어 벨트의 이동 및 시각 효과를 제어합니다.
    /// </summary>
    public sealed class ConveyorController : MonoBehaviour
    {
        [SerializeField] private Transform[] cargo = System.Array.Empty<Transform>();
        [SerializeField, Min(0f)] private float cargoSpeed = 2.5f;
        [SerializeField] private float loopStartZ = -5.5f;
        [SerializeField] private float loopEndZ = 5.5f;

        private bool isRunning;

        public bool IsRunning => isRunning;

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            float movement = cargoSpeed * Time.deltaTime;
            foreach (Transform item in cargo)
            {
                if (item == null)
                {
                    continue;
                }

                Vector3 position = item.localPosition;
                position.z += movement;
                if (position.z > loopEndZ)
                {
                    position.z = loopStartZ + (position.z - loopEndZ);
                }

                item.localPosition = position;
            }
        }

        /// <summary>
        /// PLC 미션 완료 후 컨베이어와 화물을 실제 가동 상태로 전환합니다.
        /// </summary>
        public void StartConveyor()
        {
            isRunning = true;
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(Transform[] cargoItems, float startZ, float endZ)
        {
            cargo = cargoItems ?? System.Array.Empty<Transform>();
            loopStartZ = startZ;
            loopEndZ = endZ;
        }
    }
}
