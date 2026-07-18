using UnityEngine;

namespace TheFusionEngineer.Core
{
    /// <summary>
    /// 게임 전역 효과음 참조를 보관하는 ScriptableObject 데이터 에셋입니다.
    /// 코드에 에셋 경로를 반복해서 적지 않고 Inspector 참조로 효과음을 공급합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "The Fusion Engineer/Game SFX Library")]
    public sealed class GameSfxLibrary : ScriptableObject
    {
        private const string ResourceName = "GameSfxLibrary";
        private static GameSfxLibrary instance;

        [SerializeField] private AudioClip footstep;
        [SerializeField] private AudioClip holdReverse;
        [SerializeField] private AudioClip firstMissionComplete;
        [SerializeField] private AudioClip stageComplete;
        [SerializeField] private AudioClip portalEnter;

        public static GameSfxLibrary Instance => instance ??= Resources.Load<GameSfxLibrary>(ResourceName);
        public AudioClip Footstep => footstep;
        public AudioClip HoldReverse => holdReverse;
        public AudioClip FirstMissionComplete => firstMissionComplete;
        public AudioClip StageComplete => stageComplete;
        public AudioClip PortalEnter => portalEnter;

        // LoadFootstep 관련 게임 로직을 수행합니다.
        public static AudioClip LoadFootstep()
        {
            return Instance != null ? Instance.footstep : null;
        }

        // LoadHoldReverse 관련 게임 로직을 수행합니다.
        public static AudioClip LoadHoldReverse()
        {
            return Instance != null ? Instance.holdReverse : null;
        }

        // LoadFirstMissionComplete 관련 게임 로직을 수행합니다.
        public static AudioClip LoadFirstMissionComplete()
        {
            return Instance != null ? Instance.firstMissionComplete : null;
        }

        // LoadStageComplete 관련 게임 로직을 수행합니다.
        public static AudioClip LoadStageComplete()
        {
            return Instance != null ? Instance.stageComplete : null;
        }

        // LoadPortalEnter 관련 게임 로직을 수행합니다.
        public static AudioClip LoadPortalEnter()
        {
            return Instance != null ? Instance.portalEnter : null;
        }
    }
}
