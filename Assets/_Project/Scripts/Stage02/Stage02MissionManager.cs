using System.Collections;
using TheFusionEngineer.Missions;
using UnityEngine;
using UnityEngine.UI;
using TheFusionEngineer.Core;

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
        [SerializeField] private StagePortalController stagePortal;
        [SerializeField] private LadderClimbController ladder;
        [SerializeField] private AudioClip firstMissionCompleteClip;
        [SerializeField] private AudioClip stageCompleteClip;
        [SerializeField, Range(0f, 1f)] private float completionVolume = 0.85f;

        [Header("Localized Text")]
        [SerializeField] private string missionAText = "MISSION A\nSYNCHRONIZE SSM MONITORING SYSTEM";
        [SerializeField] private string missionBText = "MISSION B\nRESTORE LINUX ANALYSIS SERVER";
        [SerializeField] private string missionACompleteText = "SSM SYSTEM SYNCHRONIZED\nANNUAL COST SAVING: KRW 170,000,000";
        [SerializeField] private string missionBCompleteText = "MEMORY POOL RESTORED\nANNUAL COST SAVING: KRW 155,000,000";
        [SerializeField] private string acquiredText = "CAREER CORE 02: ACQUIRED";
        [SerializeField] private string finalMessageText = "FULL-STACK + BACKEND\nARCHITECTURE COMPLETE";

        private Coroutine messageRoutine;
        private bool isStageComplete;

        public bool IsStageComplete => isStageComplete;

        private void Start()
        {
            if (firstMissionCompleteClip == null)
            {
                firstMissionCompleteClip = GameSfxLibrary.LoadFirstMissionComplete();
            }

            if (stageCompleteClip == null)
            {
                stageCompleteClip = GameSfxLibrary.LoadStageComplete();
            }
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
            isStageComplete = false;
            ladder?.SetUnlocked(false);
        }

        public void ConfigurePortal(StagePortalController portal)
        {
            stagePortal = portal;
        }

        public void ConfigureLadder(LadderClimbController climbLadder)
        {
            ladder = climbLadder;
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
                PersistentSfxPlayer.Play(firstMissionCompleteClip, completionVolume);
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
                isStageComplete = true;
                PersistentSfxPlayer.Play(stageCompleteClip, completionVolume);
                stagePortal?.UnlockPortal();
                ladder?.SetUnlocked(true);
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
