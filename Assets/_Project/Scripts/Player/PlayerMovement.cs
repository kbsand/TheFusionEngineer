using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TheFusionEngineer.Core;

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
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
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
        private float verticalVelocity;
        private float nextFootstepTime;
        private AudioSource footstepSource;
        private bool hasLoggedFootstepPlayback;
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");

        public event Action MovementInputDetected;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            moveAction = inputActions?.FindAction("Player/Move", true);
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
        }

        private void OnDisable()
        {
            moveAction?.Disable();
        }

        private void Update()
        {
            Vector2 input = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector3 moveDirection = GetCameraRelativeDirection(input);

            if (input.sqrMagnitude > 0.001f)
            {
                MovementInputDetected?.Invoke();
            }

            if (moveDirection.sqrMagnitude > 0.001f)
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

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = moveDirection * moveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);

            UpdateFootsteps(moveDirection.sqrMagnitude > 0.001f);

            if (animator != null)
            {
                Vector3 horizontalVelocity = characterController.velocity;
                horizontalVelocity.y = 0f;
                animator.SetFloat(SpeedParameter, horizontalVelocity.magnitude);
            }
        }

        private void UpdateFootsteps(bool hasMovementInput)
        {
            Vector3 horizontalVelocity = characterController.velocity;
            horizontalVelocity.y = 0f;
            if (footstepClip == null || !hasMovementInput || horizontalVelocity.sqrMagnitude < 0.04f)
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
            nextFootstepTime = Time.time + footstepInterval;
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
