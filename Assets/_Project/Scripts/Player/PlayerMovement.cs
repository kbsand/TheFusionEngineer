using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TheFusionEngineer.Core;
using TheFusionEngineer.Missions;

namespace TheFusionEngineer.Player
{
    /// <summary>
    /// 플레이어의 이동·달리기·점프 입력과 Animator, 발소리, 댄스 음악을 함께 제어합니다.
    /// Inspector에 연결된 InputActionAsset과 Animator를 사용하며 런타임에는 재생용 AudioSource만 자동 생성합니다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform movementCamera;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 8f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -20f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Dance Music")]
        [Tooltip("DanceStateNames와 같은 순서로 배치된 댄스별 전용 음악입니다.")]
        [SerializeField] private AudioClip[] danceMusicClips = new AudioClip[5];
        [SerializeField, Range(0f, 1f)] private float danceMusicVolume = 0.8f;
        [SerializeField] private AudioSource stageMusicSource;
        [SerializeField, Min(0.05f)] private float musicFadeDuration = 0.6f;

        [Header("Footsteps")]
        [SerializeField] private AudioClip footstepClip;
        [SerializeField, Min(0.05f)] private float footstepInterval = 0.42f;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.85f;
        [SerializeField] private Vector2 footstepPitchRange = new(0.95f, 1.05f);

        private CharacterController characterController;
        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction danceAction;
        private float verticalVelocity;
        private float nextFootstepTime;
        private AudioSource footstepSource;
        private AudioSource danceMusicSource;
        private Coroutine musicFadeRoutine;
        private float stageMusicVolume;
        private bool resumeStageMusicAfterDance;
        private bool hasLoggedFootstepPlayback;
        private bool isDrilling;
        private float actionLockUntil;
        private int previousDanceIndex = -1;
        private int activeDanceIndex = -1;
        private float dancePlaybackStartTime;
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int IsSprintingParameter = Animator.StringToHash("IsSprinting");
        private static readonly int JumpParameter = Animator.StringToHash("Jump");
        private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedParameter = Animator.StringToHash("VerticalSpeed");
        private static readonly int IsDrillingParameter = Animator.StringToHash("IsDrilling");
        private static readonly int ActionTag = Animator.StringToHash("Action");
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");

        private static readonly string[] DanceStateNames =
        {
            "Dance Chicken Wing",
            "Dance Sprinkler",
            "Dance Legs Kick Arms Pump",
            "Dance Free Style",
            "Dance Legs Kick"
        };
        private static readonly int[] DanceStateHashes = CreateDanceStateHashes();

        public event Action MovementInputDetected;

        // Unity가 오브젝트를 초기화할 때 필요한 참조와 초기 상태를 준비합니다.
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            moveAction = inputActions?.FindAction("Player/Move", true);
            sprintAction = inputActions?.FindAction("Player/Sprint", true);
            jumpAction = inputActions?.FindAction("Player/Jump", true);
            danceAction = inputActions?.FindAction("Player/Dance", false);
            animator ??= GetComponentInChildren<Animator>(true);
            if (footstepClip == null)
            {
                footstepClip = GameSfxLibrary.LoadFootstep();
            }
            // [런타임 자동 생성] 씬에 별도 컴포넌트를 두지 않고 재생 전용 AudioSource를 붙입니다.
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 0f;
            footstepSource.priority = 0;
            footstepSource.ignoreListenerPause = true;

            // [런타임 자동 생성] 댄스곡 페이드와 BGM 전환에만 사용하는 AudioSource입니다.
            danceMusicSource = gameObject.AddComponent<AudioSource>();
            danceMusicSource.playOnAwake = false;
            danceMusicSource.loop = false;
            danceMusicSource.spatialBlend = 0f;
            danceMusicSource.priority = 0;
            danceMusicSource.ignoreListenerPause = true;
            stageMusicVolume = stageMusicSource != null ? stageMusicSource.volume : 0f;

            if (footstepClip == null)
            {
                Debug.LogError("[Player Audio] Footstep clip could not be loaded.", this);
            }
        }

        // Unity가 컴포넌트를 활성화할 때 입력과 이벤트 연결을 시작합니다.
        private void OnEnable()
        {
            moveAction?.Enable();
            sprintAction?.Enable();
            jumpAction?.Enable();
            danceAction?.Enable();
            HoldInteractionController.HoldStarted += HandleInteractionHoldStarted;
            HoldInteractionController.HoldStopped += HandleInteractionHoldStopped;
        }

        // Unity가 컴포넌트를 비활성화할 때 입력과 이벤트 연결을 정리합니다.
        private void OnDisable()
        {
            moveAction?.Disable();
            sprintAction?.Disable();
            jumpAction?.Disable();
            danceAction?.Disable();
            HoldInteractionController.HoldStarted -= HandleInteractionHoldStarted;
            HoldInteractionController.HoldStopped -= HandleInteractionHoldStopped;

            isDrilling = false;
            animator?.SetBool(IsDrillingParameter, false);
            RestoreStageMusicImmediately();
        }

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
        private void Update()
        {
            if (danceAction?.WasPressedThisFrame() == true)
            {
                TryPlayRandomDance();
            }

            Vector2 input = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            bool danceInterrupted = TryInterruptDanceForMovement(input);
            UpdateDanceMusic(danceInterrupted);
            bool actionAnimationLocked =
                !danceInterrupted && IsActionAnimationLocked();
            if (actionAnimationLocked)
            {
                input = Vector2.zero;
            }

            Vector3 moveDirection = GetCameraRelativeDirection(input);
            bool hasMovementInput = moveDirection.sqrMagnitude > 0.001f;
            bool isSprinting = hasMovementInput && sprintAction?.IsPressed() == true;

            if (input.sqrMagnitude > 0.001f)
            {
                MovementInputDetected?.Invoke();
            }

            if (hasMovementInput)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (!actionAnimationLocked && characterController.isGrounded &&
                jumpAction?.WasPressedThisFrame() == true)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator?.SetTrigger(JumpParameter);
            }

            verticalVelocity += gravity * Time.deltaTime;
            float horizontalSpeed = isSprinting ? sprintSpeed : moveSpeed;
            Vector3 velocity = moveDirection * horizontalSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            bool isGrounded = characterController.isGrounded;
            UpdateFootsteps(hasMovementInput && isGrounded, isSprinting);

            if (animator != null)
            {
                Vector3 horizontalVelocity = characterController.velocity;
                horizontalVelocity.y = 0f;
                animator.SetFloat(SpeedParameter, horizontalVelocity.magnitude);
                animator.SetBool(IsSprintingParameter, isSprinting);
                animator.SetBool(GroundedParameter, isGrounded);
                animator.SetFloat(VerticalSpeedParameter, verticalVelocity);
            }
        }

        // 필요한 실행 조건을 검사하고 조건을 만족할 때만 동작을 수행합니다.
        private void TryPlayRandomDance()
        {
            if (animator == null || !characterController.isGrounded || IsActionAnimationLocked())
            {
                return;
            }

            int danceIndex = UnityEngine.Random.Range(0, DanceStateNames.Length);
            if (DanceStateNames.Length > 1 && danceIndex == previousDanceIndex)
            {
                danceIndex = (danceIndex + UnityEngine.Random.Range(1, DanceStateNames.Length)) %
                    DanceStateNames.Length;
            }

            string stateName = DanceStateNames[danceIndex];
            int stateHash = Animator.StringToHash($"Base Layer.{stateName}");
            if (!animator.HasState(0, stateHash))
            {
                Debug.LogWarning($"[Player Animation] Dance state '{stateName}' is missing.", animator);
                return;
            }

            previousDanceIndex = danceIndex;
            actionLockUntil = Time.time + 0.25f;
            animator.CrossFadeInFixedTime(stateHash, 0.12f, 0, 0f);
            PlayDanceMusic(danceIndex);
        }

        // PlayDanceMusic 관련 게임 로직을 수행합니다.
        private void PlayDanceMusic(int danceIndex)
        {
            if (danceMusicSource == null ||
                danceMusicClips == null ||
                danceIndex < 0 ||
                danceIndex >= danceMusicClips.Length ||
                danceMusicClips[danceIndex] == null)
            {
                Debug.LogWarning(
                    $"[Player Audio] No music is assigned for dance '{DanceStateNames[danceIndex]}'.",
                    this);
                return;
            }

            StopMusicFadeRoutine();
            danceMusicSource.Stop();
            activeDanceIndex = danceIndex;
            dancePlaybackStartTime = Time.time;
            danceMusicSource.clip = danceMusicClips[danceIndex];
            danceMusicSource.volume = 0f;
            danceMusicSource.Play();
            resumeStageMusicAfterDance = stageMusicSource != null && stageMusicSource.isPlaying;
            musicFadeRoutine = StartCoroutine(FadeToDanceMusic());
        }

        // UpdateDanceMusic 관련 게임 로직을 수행합니다.
        private void UpdateDanceMusic(bool danceInterrupted)
        {
            if (activeDanceIndex < 0)
            {
                return;
            }

            if (danceInterrupted)
            {
                StopDanceMusic();
                return;
            }

            // CrossFade 직후에는 아직 현재 Animator 상태가 Idle일 수 있으므로 잠깐 기다립니다.
            if (Time.time - dancePlaybackStartTime < 0.2f || animator == null)
            {
                return;
            }

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            bool isPlayingDance = IsDanceState(currentState.fullPathHash);
            if (animator.IsInTransition(0))
            {
                isPlayingDance |= IsDanceState(
                    animator.GetNextAnimatorStateInfo(0).fullPathHash);
            }

            if (!isPlayingDance)
            {
                StopDanceMusic();
            }
        }

        // 더 이상 필요하지 않은 화면 요소와 진행 중 작업을 정리합니다.
        private void StopDanceMusic()
        {
            if (activeDanceIndex < 0 &&
                (danceMusicSource == null || !danceMusicSource.isPlaying))
            {
                return;
            }

            activeDanceIndex = -1;
            StopMusicFadeRoutine();
            musicFadeRoutine = StartCoroutine(FadeBackToStageMusic());
        }

        // FadeToDanceMusic 관련 게임 로직을 수행합니다.
        private IEnumerator FadeToDanceMusic()
        {
            float elapsed = 0f;
            float danceStartVolume = danceMusicSource.volume;
            float stageStartVolume = stageMusicSource != null ? stageMusicSource.volume : 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / musicFadeDuration);
                danceMusicSource.volume = Mathf.Lerp(danceStartVolume, danceMusicVolume, progress);
                if (stageMusicSource != null && resumeStageMusicAfterDance)
                {
                    stageMusicSource.volume = Mathf.Lerp(stageStartVolume, 0f, progress);
                }

                yield return null;
            }

            danceMusicSource.volume = danceMusicVolume;
            if (stageMusicSource != null && resumeStageMusicAfterDance)
            {
                stageMusicSource.volume = 0f;
                stageMusicSource.Pause();
            }

            musicFadeRoutine = null;
        }

        // FadeBackToStageMusic 관련 게임 로직을 수행합니다.
        private IEnumerator FadeBackToStageMusic()
        {
            float elapsed = 0f;
            float danceStartVolume = danceMusicSource != null ? danceMusicSource.volume : 0f;
            float stageStartVolume = stageMusicSource != null ? stageMusicSource.volume : 0f;

            if (stageMusicSource != null && resumeStageMusicAfterDance)
            {
                if (!stageMusicSource.isPlaying)
                {
                    stageMusicSource.UnPause();
                }
            }

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / musicFadeDuration);
                if (danceMusicSource != null)
                {
                    danceMusicSource.volume = Mathf.Lerp(danceStartVolume, 0f, progress);
                }

                if (stageMusicSource != null && resumeStageMusicAfterDance)
                {
                    stageMusicSource.volume =
                        Mathf.Lerp(stageStartVolume, stageMusicVolume, progress);
                }

                yield return null;
            }

            if (danceMusicSource != null)
            {
                danceMusicSource.Stop();
                danceMusicSource.clip = null;
                danceMusicSource.volume = 0f;
            }

            if (stageMusicSource != null && resumeStageMusicAfterDance)
            {
                stageMusicSource.volume = stageMusicVolume;
            }

            resumeStageMusicAfterDance = false;
            musicFadeRoutine = null;
        }

        // 더 이상 필요하지 않은 화면 요소와 진행 중 작업을 정리합니다.
        private void StopMusicFadeRoutine()
        {
            if (musicFadeRoutine == null)
            {
                return;
            }

            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        // RestoreStageMusicImmediately 관련 게임 로직을 수행합니다.
        private void RestoreStageMusicImmediately()
        {
            StopMusicFadeRoutine();
            activeDanceIndex = -1;

            if (danceMusicSource != null)
            {
                danceMusicSource.Stop();
                danceMusicSource.clip = null;
                danceMusicSource.volume = 0f;
            }

            if (stageMusicSource != null && resumeStageMusicAfterDance)
            {
                stageMusicSource.volume = stageMusicVolume;
                stageMusicSource.UnPause();
            }

            resumeStageMusicAfterDance = false;
        }

        // 필요한 실행 조건을 검사하고 조건을 만족할 때만 동작을 수행합니다.
        private bool TryInterruptDanceForMovement(Vector2 input)
        {
            if (animator == null ||
                isDrilling ||
                input.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            bool currentStateIsDance = IsDanceState(currentState.fullPathHash);
            bool nextStateIsDance = false;
            bool nextStateIsIdle = false;

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
                nextStateIsDance = IsDanceState(nextState.fullPathHash);
                nextStateIsIdle = nextState.fullPathHash == IdleState;
            }

            if (!currentStateIsDance && !nextStateIsDance)
            {
                return false;
            }

            actionLockUntil = Time.time;
            if (!nextStateIsIdle)
            {
                animator.CrossFadeInFixedTime(IdleState, 0.08f, 0, 0f);
            }

            return true;
        }

        // 필요한 실행 조건을 검사하고 조건을 만족할 때만 동작을 수행합니다.
        private static bool IsDanceState(int stateHash)
        {
            foreach (int danceStateHash in DanceStateHashes)
            {
                if (stateHash == danceStateHash)
                {
                    return true;
                }
            }

            return false;
        }

        // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
        private static int[] CreateDanceStateHashes()
        {
            int[] hashes = new int[DanceStateNames.Length];
            for (int index = 0; index < DanceStateNames.Length; index++)
            {
                hashes[index] = Animator.StringToHash(
                    $"Base Layer.{DanceStateNames[index]}");
            }

            return hashes;
        }

        // 입력 또는 게임 이벤트가 발생했을 때 후속 동작을 처리합니다.
        private void HandleInteractionHoldStarted(Transform interactionPlayer)
        {
            if (interactionPlayer != transform || animator == null)
            {
                return;
            }

            int drillEnterHash = Animator.StringToHash("Base Layer.Drill Enter");
            if (!animator.HasState(0, drillEnterHash))
            {
                Debug.LogWarning("[Player Animation] Drill Enter state is missing.", animator);
                return;
            }

            isDrilling = true;
            StopDanceMusic();
            actionLockUntil = Time.time + 0.25f;
            animator.SetBool(IsDrillingParameter, true);
            animator.CrossFadeInFixedTime(drillEnterHash, 0.12f, 0, 0f);
        }

        // 입력 또는 게임 이벤트가 발생했을 때 후속 동작을 처리합니다.
        private void HandleInteractionHoldStopped(Transform interactionPlayer)
        {
            if (interactionPlayer != transform)
            {
                return;
            }

            isDrilling = false;
            animator?.SetBool(IsDrillingParameter, false);
        }

        // 필요한 실행 조건을 검사하고 조건을 만족할 때만 동작을 수행합니다.
        private bool IsActionAnimationLocked()
        {
            if (isDrilling || Time.time < actionLockUntil || animator == null)
            {
                return isDrilling || Time.time < actionLockUntil;
            }

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
            if (currentState.tagHash == ActionTag)
            {
                return true;
            }

            return animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).tagHash == ActionTag;
        }

        // UpdateFootsteps 관련 게임 로직을 수행합니다.
        private void UpdateFootsteps(bool canPlayFootsteps, bool isSprinting)
        {
            Vector3 horizontalVelocity = characterController.velocity;
            horizontalVelocity.y = 0f;
            if (footstepClip == null || !canPlayFootsteps || horizontalVelocity.sqrMagnitude < 0.04f)
            {
                nextFootstepTime = Time.time;
                return;
            }

            if (Time.time < nextFootstepTime)
            {
                return;
            }

            float minimumPitch = Mathf.Min(footstepPitchRange.x, footstepPitchRange.y);
            float maximumPitch = Mathf.Max(footstepPitchRange.x, footstepPitchRange.y);
            footstepSource.pitch = UnityEngine.Random.Range(minimumPitch, maximumPitch);
            footstepSource.PlayOneShot(footstepClip, footstepVolume);
            if (!hasLoggedFootstepPlayback)
            {
                hasLoggedFootstepPlayback = true;
                Debug.Log($"[Player Audio] Playing footstep '{footstepClip.name}' at volume {footstepVolume:0.00}.", this);
            }

            float interval = isSprinting ? footstepInterval * 0.7f : footstepInterval;
            nextFootstepTime = Time.time + interval;
        }

        // 다른 컴포넌트가 전달한 참조와 설정값을 저장합니다.
        public void Configure(InputActionAsset actions, Transform cameraTransform)
        {
            inputActions = actions;
            movementCamera = cameraTransform;
        }

        // GetCameraRelativeDirection 관련 게임 로직을 수행합니다.
        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Vector3 forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            Vector3 right = movementCamera != null ? movementCamera.right : Vector3.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 direction = forward * input.y + right * input.x;
            return Vector3.ClampMagnitude(direction, 1f);
        }
    }
}
