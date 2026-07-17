using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TheFusionEngineer.Core;
using TheFusionEngineer.Missions;

namespace TheFusionEngineer.Player
{
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
        private bool hasLoggedFootstepPlayback;
        private bool isDrilling;
        private float actionLockUntil;
        private int previousDanceIndex = -1;
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int IsSprintingParameter = Animator.StringToHash("IsSprinting");
        private static readonly int JumpParameter = Animator.StringToHash("Jump");
        private static readonly int GroundedParameter = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedParameter = Animator.StringToHash("VerticalSpeed");
        private static readonly int IsDrillingParameter = Animator.StringToHash("IsDrilling");
        private static readonly int ActionTag = Animator.StringToHash("Action");

        private static readonly string[] DanceStateNames =
        {
            "Dance Chicken Wing",
            "Dance Sprinkler",
            "Dance Legs Kick Arms Pump",
            "Dance Free Style",
            "Dance Legs Kick"
        };

        public event Action MovementInputDetected;

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
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 0f;
            footstepSource.priority = 0;
            footstepSource.ignoreListenerPause = true;

            if (footstepClip == null)
            {
                Debug.LogError("[Player Audio] Footstep clip could not be loaded.", this);
            }
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            sprintAction?.Enable();
            jumpAction?.Enable();
            danceAction?.Enable();
            HoldInteractionController.HoldStarted += HandleInteractionHoldStarted;
            HoldInteractionController.HoldStopped += HandleInteractionHoldStopped;
        }

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
        }

        private void Update()
        {
            if (danceAction?.WasPressedThisFrame() == true)
            {
                TryPlayRandomDance();
            }

            Vector2 input = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            bool actionAnimationLocked = IsActionAnimationLocked();
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
        }

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
            actionLockUntil = Time.time + 0.25f;
            animator.SetBool(IsDrillingParameter, true);
            animator.CrossFadeInFixedTime(drillEnterHash, 0.12f, 0, 0f);
        }

        private void HandleInteractionHoldStopped(Transform interactionPlayer)
        {
            if (interactionPlayer != transform)
            {
                return;
            }

            isDrilling = false;
            animator?.SetBool(IsDrillingParameter, false);
        }

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

        public void Configure(InputActionAsset actions, Transform cameraTransform)
        {
            inputActions = actions;
            movementCamera = cameraTransform;
        }

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
