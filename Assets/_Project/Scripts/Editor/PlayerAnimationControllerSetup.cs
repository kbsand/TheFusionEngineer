using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TheFusionEngineer.Editor
{
    /// <summary>
    /// 플레이어 Animator Controller를 현재 이동 코드와 Kevin Iglesias 모션에 맞게 구성합니다.
    /// 메뉴를 다시 실행해도 같은 구성이 유지되도록 전체 상태 머신을 재구성합니다.
    /// </summary>
    public static class PlayerAnimationControllerSetup
    {
        private const string ControllerPath =
            "Assets/_Project/Art/Characters/Stage01/Stage01_PlayerAnimator.controller";

        private const string IdlePath =
            "Assets/_Project/Art/Characters/Stage01/Stage01_Idle.fbx";

        private const string WalkPath =
            "Assets/_Project/Art/Characters/Stage01/Stage01_Walk.fbx";

        private const string RunPath =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx";

        private const string JumpBeginPath =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01 - Begin.fbx";

        private const string FallPath =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Fall01.fbx";

        private const string LandPath =
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01 - Land.fbx";

        private static readonly string[] DancePaths =
        {
            "Assets/MoCapCentral/MC_Sample/Animations/Dance/MCU_af_Stand_DanceChickenWing.FBX",
            "Assets/MoCapCentral/MC_Sample/Animations/Dance/MCU_af_Stand_DanceSprinkler.FBX",
            "Assets/MoCapCentral/MC_Sample/Animations/Dance/MCU_am_Dance_LegsKickArmsPump.FBX",
            "Assets/MoCapCentral/MC_Sample/Animations/Dance/MCU_am_HandsHips_Dance_FreeStyle_Loop.FBX",
            "Assets/MoCapCentral/MC_Sample/Animations/Dance/MCU_am_HandsHips_Dance_LegsKick_01_Loop.FBX"
        };

        private static readonly string[] DanceStateNames =
        {
            "Dance Chicken Wing",
            "Dance Sprinkler",
            "Dance Legs Kick Arms Pump",
            "Dance Free Style",
            "Dance Legs Kick"
        };

        private const string DrillEnterPath =
            "Assets/MoCapCentral/MC_Sample/Animations/Drill/MCU_am_Stand_Trans_StandDrillLow.FBX";

        private const string DrillLoopPath =
            "Assets/MoCapCentral/MC_Sample/Animations/Drill/MCU_am_StandDrillLow_01_Drill.FBX";

        private const string DrillExitPath =
            "Assets/MoCapCentral/MC_Sample/Animations/Drill/MCU_am_StandDrillLow_Trans_Stand.FBX";

        [InitializeOnLoadMethod]
        private static void ScheduleSetupAfterScriptsReload()
        {
            EditorApplication.delayCall += SetupIfRequired;
        }

        private static void SetupIfRequired()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null || controller.layers.Length == 0)
            {
                return;
            }

            AnimatorState[] states = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .ToArray();
            bool hasDanceStates = DanceStateNames.All(
                stateName => states.Any(state => state.name == stateName));
            bool hasDrillStates = states.Any(state => state.name == "Drill Enter") &&
                                  states.Any(state => state.name == "Drill Loop") &&
                                  states.Any(state => state.name == "Drill Exit");
            bool hasDrillParameter = controller.parameters.Any(
                parameter => parameter.name == "IsDrilling" &&
                             parameter.type == AnimatorControllerParameterType.Bool);

            if (!hasDanceStates || !hasDrillStates || !hasDrillParameter)
            {
                Setup();
            }
        }

        [MenuItem("Tools/The Fusion Engineer/Setup Player Animations")]
        public static void Setup()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimationClip idleClip = LoadAnimationClip(IdlePath);
            AnimationClip walkClip = LoadAnimationClip(WalkPath);
            AnimationClip runClip = LoadAnimationClip(RunPath);
            AnimationClip jumpBeginClip = LoadAnimationClip(JumpBeginPath);
            AnimationClip fallClip = LoadAnimationClip(FallPath);
            AnimationClip landClip = LoadAnimationClip(LandPath);
            AnimationClip[] danceClips = DancePaths.Select(LoadAnimationClip).ToArray();
            AnimationClip drillEnterClip = LoadAnimationClip(DrillEnterPath);
            AnimationClip drillLoopClip = LoadAnimationClip(DrillLoopPath);
            AnimationClip drillExitClip = LoadAnimationClip(DrillExitPath);

            if (controller == null || idleClip == null || walkClip == null || runClip == null ||
                jumpBeginClip == null || fallClip == null || landClip == null ||
                danceClips.Any(clip => clip == null) || drillEnterClip == null ||
                drillLoopClip == null || drillExitClip == null)
            {
                Debug.LogError("[Player Animation] Controller or one or more animation clips are missing.");
                return;
            }

            controller.parameters = new[]
            {
                FloatParameter("Speed"),
                BoolParameter("IsSprinting"),
                TriggerParameter("Jump"),
                BoolParameter("Grounded", true),
                FloatParameter("VerticalSpeed"),
                BoolParameter("IsDrilling")
            };

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ClearStateMachine(stateMachine);

            AnimatorState idle = AddState(stateMachine, "Idle", idleClip, new Vector3(180f, 50f));
            AnimatorState walk = AddState(stateMachine, "Walk", walkClip, new Vector3(430f, 0f), 1.35f);
            AnimatorState run = AddState(stateMachine, "Run", runClip, new Vector3(680f, 50f));
            AnimatorState jumpBegin = AddState(stateMachine, "Jump Begin", jumpBeginClip,
                new Vector3(260f, 250f));
            AnimatorState fall = AddState(stateMachine, "Fall", fallClip, new Vector3(500f, 250f));
            AnimatorState land = AddState(stateMachine, "Land", landClip, new Vector3(740f, 250f));

            for (int i = 0; i < danceClips.Length; i++)
            {
                AnimatorState dance = AddState(
                    stateMachine,
                    DanceStateNames[i],
                    danceClips[i],
                    new Vector3(180f + i * 190f, 450f));
                dance.tag = "Action";
                AddTransition(dance, idle, true, 0.95f);
            }

            AnimatorState drillEnter = AddState(
                stateMachine, "Drill Enter", drillEnterClip, new Vector3(260f, 650f));
            AnimatorState drillLoop = AddState(
                stateMachine, "Drill Loop", drillLoopClip, new Vector3(500f, 650f));
            AnimatorState drillExit = AddState(
                stateMachine, "Drill Exit", drillExitClip, new Vector3(740f, 650f));
            drillEnter.tag = "Action";
            drillLoop.tag = "Action";
            drillExit.tag = "Action";

            stateMachine.defaultState = idle;

            AddTransition(idle, walk, false, 0f, (AnimatorConditionMode.Greater, 0.1f, "Speed"));
            AddTransition(walk, idle, false, 0f, (AnimatorConditionMode.Less, 0.1f, "Speed"));

            AddTransition(idle, run, false, 0f,
                (AnimatorConditionMode.If, 0f, "IsSprinting"),
                (AnimatorConditionMode.Greater, 0.1f, "Speed"));
            AddTransition(walk, run, false, 0f, (AnimatorConditionMode.If, 0f, "IsSprinting"));
            AddTransition(run, idle, false, 0f, (AnimatorConditionMode.Less, 0.1f, "Speed"));
            AddTransition(run, walk, false, 0f,
                (AnimatorConditionMode.IfNot, 0f, "IsSprinting"),
                (AnimatorConditionMode.Greater, 0.1f, "Speed"));

            AnimatorStateTransition jumpTransition = stateMachine.AddAnyStateTransition(jumpBegin);
            ConfigureTransition(jumpTransition, false, 0f);
            jumpTransition.canTransitionToSelf = false;
            jumpTransition.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            AddTransition(jumpBegin, fall, false, 0f,
                (AnimatorConditionMode.Less, 0f, "VerticalSpeed"));
            AddTransition(fall, land, false, 0f, (AnimatorConditionMode.If, 0f, "Grounded"));

            // 점프 버튼을 누르지 않고 발판에서 떨어졌을 때도 낙하 모션으로 전환합니다.
            AddFallTransition(idle, fall);
            AddFallTransition(walk, fall);
            AddFallTransition(run, fall);

            AddTransition(land, idle, true, 0.72f, (AnimatorConditionMode.Less, 0.1f, "Speed"));
            AddTransition(land, walk, true, 0.72f, (AnimatorConditionMode.Greater, 0.1f, "Speed"));

            AddTransition(drillEnter, drillLoop, true, 0.9f);
            AddTransition(drillLoop, drillExit, false, 0f,
                (AnimatorConditionMode.IfNot, 0f, "IsDrilling"));
            AddTransition(drillExit, idle, true, 0.9f);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Player Animation] Locomotion, random dances and drill interaction configured.", controller);
        }

        [MenuItem("Tools/The Fusion Engineer/Setup Player Animations", true)]
        private static bool ValidateSetup()
        {
            return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null;
        }

        private static AnimationClip LoadAnimationClip(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }

        private static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(childState.state);
            }

            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(childStateMachine.stateMachine);
            }

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }
        }

        private static AnimatorState AddState(AnimatorStateMachine stateMachine, string name, Motion motion,
            Vector3 position, float speed = 1f)
        {
            AnimatorState state = stateMachine.AddState(name, position);
            state.motion = motion;
            state.speed = speed;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddFallTransition(AnimatorState source, AnimatorState fall)
        {
            AddTransition(source, fall, false, 0f,
                (AnimatorConditionMode.IfNot, 0f, "Grounded"),
                (AnimatorConditionMode.Less, -0.1f, "VerticalSpeed"));
        }

        private static AnimatorStateTransition AddTransition(AnimatorState source, AnimatorState destination,
            bool hasExitTime, float exitTime,
            params (AnimatorConditionMode mode, float threshold, string parameter)[] conditions)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            ConfigureTransition(transition, hasExitTime, exitTime);
            foreach ((AnimatorConditionMode mode, float threshold, string parameter) in conditions)
            {
                transition.AddCondition(mode, threshold, parameter);
            }

            return transition;
        }

        private static void ConfigureTransition(AnimatorStateTransition transition, bool hasExitTime, float exitTime)
        {
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
        }

        private static AnimatorControllerParameter FloatParameter(string name)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Float
            };
        }

        private static AnimatorControllerParameter BoolParameter(string name, bool defaultValue = false)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = defaultValue
            };
        }

        private static AnimatorControllerParameter TriggerParameter(string name)
        {
            return new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Trigger
            };
        }
    }
}
