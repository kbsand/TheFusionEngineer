using UnityEngine;

namespace TheFusionEngineer.Core
{
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

        public static AudioClip LoadFootstep()
        {
            return Instance != null && Instance.footstep != null
                ? Instance.footstep
                : Resources.Load<AudioClip>("SFX/Footstep");
        }

        public static AudioClip LoadHoldReverse()
        {
            return Instance != null && Instance.holdReverse != null
                ? Instance.holdReverse
                : Resources.Load<AudioClip>("SFX/HoldReverse");
        }

        public static AudioClip LoadFirstMissionComplete()
        {
            return Instance != null && Instance.firstMissionComplete != null
                ? Instance.firstMissionComplete
                : Resources.Load<AudioClip>("SFX/FirstMissionComplete");
        }

        public static AudioClip LoadStageComplete()
        {
            return Instance != null && Instance.stageComplete != null
                ? Instance.stageComplete
                : Resources.Load<AudioClip>("SFX/StageComplete");
        }

        public static AudioClip LoadPortalEnter()
        {
            return Instance != null && Instance.portalEnter != null
                ? Instance.portalEnter
                : Resources.Load<AudioClip>("SFX/PortalEnter");
        }
    }
}
