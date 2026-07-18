using System.Collections;
using TheFusionEngineer.Missions;
using UnityEngine;
using UnityEngine.UI;
using TheFusionEngineer.Core;
using TheFusionEngineer.UI;

namespace TheFusionEngineer.Stage02
{
    /// <summary>
    /// Stage2의 미션 순서, 현재 목표, 직무 배지 및 포탈 해금을 총괄합니다.
    /// </summary>
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

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigurePortal(StagePortalController portal)
        {
            stagePortal = portal;
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void ConfigureLadder(LadderClimbController climbLadder)
        {
            ladder = climbLadder;
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
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

        /// <summary>
        /// 완료된 단말기가 현재 목표와 일치하는지 검증하고 다음 미션 또는 포탈 해금으로 진행합니다.
        /// 잘못된 순서의 완료 요청과 중복 완료 요청은 여기서 차단합니다.
        /// </summary>
        public void CompleteTerminal(Stage02Terminal terminal)
        {
            // 첫 번째 단말기 완료: 두 번째 단말기와 사다리 이전 단계까지 진행시킵니다.
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

            // 두 미션이 순서대로 완료된 경우에만 Career Core와 다음 스테이지 경로를 엽니다.
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
        private void ShowMessage(string message, float duration)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }

            messageRoutine = StartCoroutine(ShowMessageRoutine(message, duration));
        }

        // 현재 진행 상황을 플레이어가 확인할 수 있도록 화면에 표시합니다.
        private IEnumerator ShowMessageRoutine(string message, float duration)
        {
            if (centerMessage == null)
            {
                yield break;
            }

            centerMessage.text = message;
            centerMessage.gameObject.SetActive(true);
            // WaitForSeconds 관련 게임 로직을 수행합니다.
            yield return new WaitForSeconds(duration);
            centerMessage.gameObject.SetActive(false);
        }

        // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
        private void SetMissionText(string value)
        {
            if (missionText != null)
            {
                missionText.text = value;
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
