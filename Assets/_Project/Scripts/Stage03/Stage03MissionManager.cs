using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TheFusionEngineer.Stage03
{
    public sealed class Stage03MissionManager : MonoBehaviour
    {
        [SerializeField] private Stage03Terminal missionA;
        [SerializeField] private Stage03Terminal missionB;
        [SerializeField] private SolarLogisticsController solarLogistics;
        [SerializeField] private Text missionText;
        [SerializeField] private Text roleBadgeText;
        [SerializeField] private Text careerCoreText;
        [SerializeField] private Text centerMessage;
        [SerializeField] private GameObject careerCoreObject;

        [Header("Localized Text")]
        [SerializeField] private string missionAText = "MISSION A\nACTIVATE G-BRAIN RAG SYSTEM";
        [SerializeField] private string missionBText = "MISSION B\nSYNCHRONIZE SOLAR LOGISTICS AND SCS";
        [SerializeField] private string missionARole = "ROLE: AI SYSTEM ARCHITECT";
        [SerializeField] private string missionBRole = "ROLE: SMART FACTORY CONTROL LEAD";
        [SerializeField] private string missionACompleteText = "G-BRAIN RAG SYSTEM ONLINE\nHALLUCINATION-SAFE SEARCH ACTIVATED";
        [SerializeField] private string missionBCompleteText = "SOLAR LOGISTICS SYNCHRONIZED\nSCS FAILOVER ENABLED";
        [SerializeField] private string acquiredText = "CAREER CORE 03: ACQUIRED";
        [SerializeField] private string finalMessageText = "OT + IT + AI\nFUSION ENGINEER COMPLETE";

        private Coroutine messageRoutine;
        private bool isStageComplete;

        public bool IsStageComplete => isStageComplete;

        private void Start()
        {
            missionA?.SetAvailable(true);
            missionB?.SetAvailable(false);
            SetText(missionText, missionAText);
            SetText(roleBadgeText, missionARole);
            SetText(careerCoreText, "CAREER CORE 03: LOCKED");
            centerMessage?.gameObject.SetActive(false);
            careerCoreObject?.SetActive(false);
        }

        public void Configure(
            Stage03Terminal terminalA,
            Stage03Terminal terminalB,
            SolarLogisticsController logistics,
            Text missionLabel,
            Text roleLabel,
            Text coreLabel,
            Text messageLabel,
            GameObject coreObject)
        {
            missionA = terminalA;
            missionB = terminalB;
            solarLogistics = logistics;
            missionText = missionLabel;
            roleBadgeText = roleLabel;
            careerCoreText = coreLabel;
            centerMessage = messageLabel;
            careerCoreObject = coreObject;
        }

        public void CompleteTerminal(Stage03Terminal terminal)
        {
            if (terminal == missionA && missionA.IsCompleted)
            {
                ShowMessage(missionACompleteText, 3f);
                missionB?.SetAvailable(true);
                SetText(missionText, missionBText);
                SetText(roleBadgeText, missionBRole);
                return;
            }

            if (terminal != missionB || missionA == null || !missionA.IsCompleted || !missionB.IsCompleted)
            {
                return;
            }

            solarLogistics?.StartLogistics();
            isStageComplete = true;
            SetText(missionText, finalMessageText);
            SetText(careerCoreText, acquiredText);
            if (careerCoreText != null)
            {
                careerCoreText.color = new Color(0.35f, 1f, 0.9f);
            }

            careerCoreObject?.SetActive(true);
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(ShowFinalMessages());
            Debug.Log("[Stage 3 Complete] Career Core 03 acquired.");
        }

        private IEnumerator ShowFinalMessages()
        {
            yield return ShowMessageRoutine(missionBCompleteText, 3f);
            yield return ShowMessageRoutine(finalMessageText, 4f);
            messageRoutine = null;
        }

        private void ShowMessage(string value, float duration)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(ShowMessageRoutine(value, duration));
        }

        private IEnumerator ShowMessageRoutine(string value, float duration)
        {
            if (centerMessage == null)
            {
                yield break;
            }

            centerMessage.text = value;
            centerMessage.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            centerMessage.gameObject.SetActive(false);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
