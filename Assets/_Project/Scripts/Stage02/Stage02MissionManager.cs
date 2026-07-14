using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TheFusionEngineer.Stage02
{
    public sealed class Stage02MissionManager : MonoBehaviour
    {
        [SerializeField] private Stage02Terminal missionA;
        [SerializeField] private Stage02Terminal missionB;
        [SerializeField] private RoleBadgeController roleBadge;
        [SerializeField] private Text missionText;
        [SerializeField] private Text careerCoreText;
        [SerializeField] private Text centerMessage;
        [SerializeField] private GameObject careerCoreObject;

        [Header("Localized Text")]
        [SerializeField] private string missionAText = "MISSION A\nSYNCHRONIZE SSM MONITORING SYSTEM";
        [SerializeField] private string missionBText = "MISSION B\nRESTORE LINUX ANALYSIS SERVER";
        [SerializeField] private string missionACompleteText = "SSM SYSTEM SYNCHRONIZED\nANNUAL COST SAVING: KRW 170,000,000";
        [SerializeField] private string missionBCompleteText = "MEMORY POOL RESTORED\nANNUAL COST SAVING: KRW 155,000,000";
        [SerializeField] private string acquiredText = "CAREER CORE 02: ACQUIRED";
        [SerializeField] private string finalMessageText = "FULL-STACK + BACKEND\nARCHITECTURE COMPLETE";

        private Coroutine messageRoutine;

        private void Start()
        {
            missionA?.SetAvailable(true);
            missionB?.SetAvailable(false);
            roleBadge?.ShowMissionARole();
            SetMissionText(missionAText);

            if (careerCoreText != null)
            {
                careerCoreText.text = "CAREER CORE 02: LOCKED";
            }

            if (centerMessage != null)
            {
                centerMessage.gameObject.SetActive(false);
            }

            careerCoreObject?.SetActive(false);
        }

        public void Configure(
            Stage02Terminal terminalA,
            Stage02Terminal terminalB,
            RoleBadgeController badge,
            Text missionLabel,
            Text coreLabel,
            Text messageLabel,
            GameObject coreObject)
        {
            missionA = terminalA;
            missionB = terminalB;
            roleBadge = badge;
            missionText = missionLabel;
            careerCoreText = coreLabel;
            centerMessage = messageLabel;
            careerCoreObject = coreObject;
        }

        public void CompleteTerminal(Stage02Terminal terminal)
        {
            if (terminal == missionA && missionA.IsCompleted)
            {
                ShowMessage(missionACompleteText, 3f);
                missionB?.SetAvailable(true);
                roleBadge?.ShowMissionBRole();
                SetMissionText(missionBText);
                return;
            }

            if (terminal == missionB && missionA != null && missionA.IsCompleted && missionB.IsCompleted)
            {
                if (careerCoreText != null)
                {
                    careerCoreText.text = acquiredText;
                    careerCoreText.color = new Color(0.25f, 1f, 0.8f);
                }

                careerCoreObject?.SetActive(true);
                SetMissionText(finalMessageText);

                if (messageRoutine != null)
                {
                    StopCoroutine(messageRoutine);
                }

                messageRoutine = StartCoroutine(ShowFinalMessages());
                Debug.Log("[Stage 2 Complete] Career Core 02 acquired.");
            }
        }

        private IEnumerator ShowFinalMessages()
        {
            yield return ShowMessageRoutine(missionBCompleteText, 3f);
            yield return ShowMessageRoutine(finalMessageText, 4f);
            messageRoutine = null;
        }

        private void ShowMessage(string message, float duration)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(ShowMessageRoutine(message, duration));
        }

        private IEnumerator ShowMessageRoutine(string message, float duration)
        {
            if (centerMessage == null)
            {
                yield break;
            }

            centerMessage.text = message;
            centerMessage.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            centerMessage.gameObject.SetActive(false);
        }

        private void SetMissionText(string value)
        {
            if (missionText != null)
            {
                missionText.text = value;
            }
        }
    }
}
