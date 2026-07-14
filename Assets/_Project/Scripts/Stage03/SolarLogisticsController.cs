using UnityEngine;

namespace TheFusionEngineer.Stage03
{
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

        private void Awake()
        {
            foreach (GameObject indicator in serverIndicators)
            {
                indicator?.SetActive(false);
            }
        }

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

        public void StartLogistics()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            foreach (GameObject indicator in serverIndicators)
            {
                indicator?.SetActive(true);
            }

            if (failoverSwitch != null)
            {
                failoverSwitch.localRotation = Quaternion.Euler(failoverEnabledRotation);
            }
        }
    }
}
