using UnityEngine;

namespace TheFusionEngineer.Missions
{
    public sealed class ConveyorController : MonoBehaviour
    {
        [SerializeField] private Transform[] cargo = System.Array.Empty<Transform>();
        [SerializeField, Min(0f)] private float cargoSpeed = 2.5f;
        [SerializeField] private float loopStartZ = -5.5f;
        [SerializeField] private float loopEndZ = 5.5f;

        private bool isRunning;

        public bool IsRunning => isRunning;

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

        public void StartConveyor()
        {
            isRunning = true;
        }

        public void Configure(Transform[] cargoItems, float startZ, float endZ)
        {
            cargo = cargoItems ?? System.Array.Empty<Transform>();
            loopStartZ = startZ;
            loopEndZ = endZ;
        }
    }
}
