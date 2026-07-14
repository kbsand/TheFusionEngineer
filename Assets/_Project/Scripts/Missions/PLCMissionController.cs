using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TheFusionEngineer.Missions
{
    public sealed class PLCMissionController : MonoBehaviour
    {
        [SerializeField] private ConveyorController conveyor;
        [SerializeField] private Renderer warningIndicator;
        [SerializeField] private Text interactionPrompt;
        [SerializeField] private Text careerCoreText;
        [SerializeField] private GameObject completionMessage;
        [SerializeField] private StagePortalController stagePortal;
        [SerializeField] private Light plcFaultLight;

        private bool isCompleted;
        private MaterialPropertyBlock warningProperties;

        public bool IsCompleted => isCompleted;

        private void Awake()
        {
            warningProperties = new MaterialPropertyBlock();
            SetWarningColor(new Color(0.9f, 0.04f, 0.03f));
            SetFaultLightColor(new Color(1f, 0.05f, 0.03f));
            SetPlayerInRange(false);

            if (completionMessage != null)
            {
                completionMessage.SetActive(false);
            }
        }

        public void SetPlayerInRange(bool inRange)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(inRange && !isCompleted);
            }
        }

        public void TryCompleteMission()
        {
            if (isCompleted)
            {
                return;
            }

            isCompleted = true;
            SetPlayerInRange(false);
            SetWarningColor(new Color(0.05f, 0.9f, 0.2f));
            SetFaultLightColor(new Color(0.05f, 0.9f, 0.2f));
            conveyor?.StartConveyor();
            stagePortal?.UnlockPortal();

            if (careerCoreText != null)
            {
                careerCoreText.text = "CAREER CORE 01: ACQUIRED";
                careerCoreText.color = new Color(0.35f, 1f, 0.45f);
            }

            if (completionMessage != null)
            {
                StartCoroutine(ShowCompletionMessage());
            }

            Debug.Log("[Stage 1 Mission Complete] PLC bottleneck fixed. CL17 line restored. Career Core 01 acquired.");
        }

        public void Configure(
            ConveyorController conveyorController,
            Renderer indicator,
            Text prompt,
            Text coreText,
            GameObject completedMessage)
        {
            conveyor = conveyorController;
            warningIndicator = indicator;
            interactionPrompt = prompt;
            careerCoreText = coreText;
            completionMessage = completedMessage;
        }

        public void ConfigureStageExit(StagePortalController portal, Light faultLight)
        {
            stagePortal = portal;
            plcFaultLight = faultLight;
        }

        private IEnumerator ShowCompletionMessage()
        {
            completionMessage.SetActive(true);
            yield return new WaitForSeconds(3f);
            completionMessage.SetActive(false);
        }

        private void SetWarningColor(Color color)
        {
            if (warningIndicator == null)
            {
                return;
            }

            warningIndicator.GetPropertyBlock(warningProperties);
            warningProperties.SetColor("_BaseColor", color);
            warningProperties.SetColor("_Color", color);
            warningIndicator.SetPropertyBlock(warningProperties);
        }

        private void SetFaultLightColor(Color color)
        {
            if (plcFaultLight != null)
            {
                plcFaultLight.color = color;
            }
        }
    }
}
