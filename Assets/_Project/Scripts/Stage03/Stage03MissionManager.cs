using System.Collections;
using TheFusionEngineer.Missions;
using UnityEngine;
using UnityEngine.UI;
using TheFusionEngineer.Core;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Stage03
{
    /// <summary>
    /// Stage3의 AI 미션 순서, 현재 목표, 직무 표시와 엔딩 포탈 해금을 총괄합니다.
    /// </summary>
    public sealed class Stage03MissionManager : MonoBehaviour
    {
        [SerializeField] private Stage03Terminal missionA;
        [SerializeField] private Stage03Terminal missionB;
        [SerializeField] private AIMainDisplayController aiMainDisplay;
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

        // Unity가 첫 프레임 전에 게임 진행 상태를 초기화합니다.
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

            if (aiMainDisplay == null)
            {
                aiMainDisplay = FindAnyObjectByType<AIMainDisplayController>();
            }

            missionA?.SetAvailable(true);
            missionB?.SetAvailable(false);
            SetText(missionText, missionAText);
            SetText(roleBadgeText, missionARole);
            SetText(careerCoreText, "CAREER CORE 03: 잠김");
            objectiveBanner = MissionObjectiveBanner.AttachTo(this);
            objectiveBanner.SetAutoCompact(true);
            objectiveBanner.Show(
                "현재 미션  ·  1 / 2",
                ExtractMissionTitle(missionAText),
                missionAObjectiveHint);
            centerMessage?.gameObject.SetActive(false);
            careerCoreObject?.SetActive(false);
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
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

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigurePortal(StagePortalController portal)
        {
            stagePortal = portal;
        }

        /// <summary>
        /// 완료된 AI 단말기의 순서를 검증하고 다음 목표를 활성화합니다.
        /// 마지막 미션이면 AI 디스플레이 연출과 엔딩 포탈 해금을 이어서 실행합니다.
        /// </summary>
        public void CompleteTerminal(Stage03Terminal terminal)
        {
            // 미션 A 완료: AI 화면을 켜고 미션 B만 사용할 수 있게 전환합니다.
            if (terminal == missionA && missionA.IsCompleted)
            {
                aiMainDisplay?.TurnOn();
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

            // 미션 B가 아니거나 선행 미션이 끝나지 않았다면 최종 보상을 지급하지 않습니다.
            if (terminal != missionB || missionA == null || !missionA.IsCompleted || !missionB.IsCompleted)
            {
                return;
            }

            // 최종 미션 완료: 물류 설비, Career Core, 엔딩 포탈을 하나의 결과로 활성화합니다.
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

        // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
        private IEnumerator ShowFinalMessages()
        {
            // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
            yield return ShowMessageRoutine(missionBCompleteText, 3f);
            // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
            yield return ShowMessageRoutine(finalMessageText, 4f);
            messageRoutine = null;
        }

        // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
        private void ShowMessage(string value, float duration)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(ShowMessageRoutine(value, duration));
        }

        // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
        private IEnumerator ShowMessageRoutine(string value, float duration)
        {
            if (centerMessage == null)
            {
                yield break;
            }

            centerMessage.text = value;
            centerMessage.gameObject.SetActive(true);
            // WaitForSeconds 관련 게임 로직을 수행합니다.
            yield return new WaitForSeconds(duration);
            centerMessage.gameObject.SetActive(false);
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        // ExtractMissionTitle 관련 게임 로직을 수행합니다.
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
