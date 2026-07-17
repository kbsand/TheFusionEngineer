using System.Collections;
using TheFusionEngineer.Missions;
using UnityEngine;
using UnityEngine.UI;
using TheFusionEngineer.Core;
using TheFusionEngineer.UI;

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
        [SerializeField] private StagePortalController stagePortal;
        [SerializeField] private AudioClip firstMissionCompleteClip;
        [SerializeField] private AudioClip stageCompleteClip;
        [SerializeField, Range(0f, 1f)] private float completionVolume = 0.85f;

        [Header("Localized Text")]
        [SerializeField] private string missionAText = "미션 A\nG-BRAIN RAG SYSTEM 활성화";
        [SerializeField] private string missionBText = "미션 B\nSOLAR LOGISTICS + SCS 동기화";
        [SerializeField] private string missionARole = "직무: AI SYSTEM ARCHITECT";
        [SerializeField] private string missionBRole = "직무: SMART FACTORY CONTROL LEAD";
        [SerializeField] private string missionACompleteText =
            "G-BRAIN RAG SYSTEM 활성화 완료\n환각 방지 검색 기능이 활성화되었습니다";
        [SerializeField] private string missionBCompleteText =
            "SOLAR LOGISTICS 동기화 완료\nSCS 장애 전환 기능이 활성화되었습니다";
        [SerializeField] private string acquiredText = "CAREER CORE 03: 획득";
        [SerializeField] private string finalMessageText = "OT + IT + AI\nFUSION ENGINEER 역량 완성";

        [Header("Objective Banner")]
        [SerializeField] private string missionAObjectiveHint =
            "G-BRAIN RAG 터미널로 이동해 E 키를 길게 눌러 시스템을 활성화하세요.";
        [SerializeField] private string missionBObjectiveHint =
            "SOLAR LOGISTICS + SCS로 이동해 E 키를 길게 눌러 시스템을 동기화하세요.";

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
            SetText(missionText, missionAText);
            SetText(roleBadgeText, missionARole);
            SetText(careerCoreText, "CAREER CORE 03: 잠김");
            objectiveBanner = MissionObjectiveBanner.AttachTo(this);
            objectiveBanner.Show(
                "현재 미션  ·  1 / 2",
                ExtractMissionTitle(missionAText),
                missionAObjectiveHint);
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

        public void ConfigurePortal(StagePortalController portal)
        {
            stagePortal = portal;
        }

        public void CompleteTerminal(Stage03Terminal terminal)
        {
            if (terminal == missionA && missionA.IsCompleted)
            {
                ShowMessage(missionACompleteText, 3f);
                PersistentSfxPlayer.Play(firstMissionCompleteClip, completionVolume);
                missionB?.SetAvailable(true);
                SetText(missionText, missionBText);
                SetText(roleBadgeText, missionBRole);
                objectiveBanner?.Show(
                    "현재 미션  ·  2 / 2",
                    ExtractMissionTitle(missionBText),
                    missionBObjectiveHint);
                return;
            }

            if (terminal != missionB || missionA == null || !missionA.IsCompleted || !missionB.IsCompleted)
            {
                return;
            }

            solarLogistics?.StartLogistics();
            isStageComplete = true;
            PersistentSfxPlayer.Play(stageCompleteClip, completionVolume);
            stagePortal?.UnlockPortal();
            SetText(missionText, finalMessageText);
            SetText(careerCoreText, acquiredText);
            objectiveBanner?.Hide();
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
