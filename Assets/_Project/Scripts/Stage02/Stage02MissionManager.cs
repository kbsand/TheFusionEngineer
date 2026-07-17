using System.Collections;
using TheFusionEngineer.Missions;
using UnityEngine;
using UnityEngine.UI;
using TheFusionEngineer.Core;
using TheFusionEngineer.UI;

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
        [SerializeField] private string missionAText = "미션 A\nSSM MONITORING 동기화";
        [SerializeField] private string missionBText = "미션 B\nLINUX ANALYSIS SERVER 복구";
        [SerializeField] private string missionACompleteText = "SSM MONITORING 동기화 완료\n연간 비용 절감: 1억 7천만 원";
        [SerializeField] private string missionBCompleteText = "메모리 풀 복구 완료\n연간 비용 절감: 1억 5,500만 원";
        [SerializeField] private string acquiredText = "CAREER CORE 02: 획득";
        [SerializeField] private string finalMessageText = "FULL-STACK + BACKEND\n아키텍처 구축 완료";

        [Header("Objective Banner")]
        [SerializeField] private string missionAObjectiveHint =
            "SSM MONITORING으로 이동해 E 키를 길게 눌러 동기화하세요.";
        [SerializeField] private string missionBObjectiveHint =
            "LINUX ANALYSIS SERVER로 이동해 E 키를 길게 눌러 복구하세요.";

        private Coroutine messageRoutine;
        private bool isStageComplete;
        private MissionObjectiveBanner objectiveBanner;

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
            objectiveBanner = MissionObjectiveBanner.AttachTo(this);
            objectiveBanner.SetAutoCompact(true);
            objectiveBanner.Show(
                "현재 미션  ·  1 / 2",
                ExtractMissionTitle(missionAText),
                missionAObjectiveHint);

            if (careerCoreText != null)
            {
                careerCoreText.text = "CAREER CORE 02: 잠김";
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
                objectiveBanner?.Show(
                    "현재 미션  ·  2 / 2",
                    ExtractMissionTitle(missionBText),
                    missionBObjectiveHint);
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
                objectiveBanner?.Hide();

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

        private static string ExtractMissionTitle(string mission)
        {
            if (string.IsNullOrWhiteSpace(mission))
            {
                return "현재 미션을 완료하세요";
            }

            int firstLineEnd = mission.IndexOf('\n');
            string title = firstLineEnd >= 0 ? mission[(firstLineEnd + 1)..] : mission;
            return title.Replace("\r", string.Empty).Replace("\n", " ").Trim();
        }
    }
}
