using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TheFusionEngineer.Core;

namespace TheFusionEngineer.Missions
{
    public sealed class HoldInteractionController : MonoBehaviour
    {
        private static readonly List<HoldInteractionController> Instances = new();
        private static HoldInteractionController activeInteraction;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] interactionPoints = Array.Empty<Transform>();
        [SerializeField] private CanvasGroup progressRoot;
        [SerializeField] private Image progressFill;
        [SerializeField] private Text promptLabel;
        [SerializeField] private Text percentLabel;
        [SerializeField] private string prompt = "Hold E";
        [SerializeField] private string lockedPrompt = "Interaction Locked";
        [SerializeField, Min(0.1f)] private float holdDuration = 2f;
        [SerializeField, Min(0.1f)] private float interactionDistance = 2.7f;
        [SerializeField] private bool startsAvailable = true;
        [SerializeField] private bool repeatable;
        [SerializeField] private bool useTriggerRange = true;

        [Header("Audio")]
        [SerializeField] private AudioClip holdClip;
        [SerializeField, Range(0f, 1f)] private float holdVolume = 0.85f;

        private InputAction interactAction;
        private float heldTime;
        private float currentSqrDistance = float.PositiveInfinity;
        private bool isAvailable;
        private bool isInsideTrigger;
        private bool isInRange;
        private bool isCompleted;
        private bool requiresRelease;
        private bool isHolding;
        private AudioSource holdSource;
        private AudioClip reversedHoldClip;
        private bool hasLoggedHoldPlayback;

        public event Action Completed;

        public bool IsCompleted => isCompleted;
        public bool IsAvailable => isAvailable;

        private void Awake()
        {
            interactAction = inputActions?.FindAction("Player/Interact", true);
            isAvailable = startsAvailable;
            if (holdClip == null)
            {
                holdClip = GameSfxLibrary.LoadHoldReverse();
            }
            ResetProgress();
            HideAllUI();
            holdSource = gameObject.AddComponent<AudioSource>();
            holdSource.playOnAwake = false;
            holdSource.loop = true;
            holdSource.spatialBlend = 0f;
            holdSource.priority = 0;
            holdSource.ignoreListenerPause = true;
            reversedHoldClip = CreateReversedClip(holdClip);

            if (holdClip == null)
            {
                Debug.LogError("[Hold Interaction] Hold clip could not be loaded.", this);
            }
        }

        private void OnEnable()
        {
            if (!Instances.Contains(this))
            {
                Instances.Add(this);
            }

            interactAction?.Enable();
        }

        private void OnDisable()
        {
            Instances.Remove(this);
            if (activeInteraction == this)
            {
                activeInteraction = null;
            }

            interactAction?.Disable();
            isInsideTrigger = false;
            isInRange = false;
            ResetProgress();
            HideAllUI();
            StopHoldSound();
        }

        private void OnDestroy()
        {
            if (reversedHoldClip != null)
            {
                Destroy(reversedHoldClip);
            }
        }

        private void Update()
        {
            EvaluateRange();
        }

        private void LateUpdate()
        {
            HoldInteractionController closest = FindClosestFor(player);
            if (activeInteraction != closest)
            {
                activeInteraction?.CancelAndHide();
                activeInteraction = closest;
            }

            if (activeInteraction == this)
            {
                ProcessActiveInteraction();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                isInsideTrigger = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            isInsideTrigger = false;
            isInRange = false;
            CancelAndHide();
        }

        public void Configure(
            InputActionAsset actions,
            Transform playerTransform,
            CanvasGroup sharedProgressRoot,
            Image sharedProgressFill,
            Text sharedPromptLabel,
            string promptText,
            float duration,
            bool availableAtStart,
            string lockedPromptText = "Interaction Locked",
            bool canRepeat = false,
            float distance = 2.7f,
            bool requireTrigger = true,
            Transform[] points = null)
        {
            inputActions = actions;
            player = playerTransform;
            progressRoot = sharedProgressRoot;
            progressFill = sharedProgressFill;
            promptLabel = sharedPromptLabel;
            prompt = promptText;
            lockedPrompt = lockedPromptText;
            holdDuration = Mathf.Max(0.1f, duration);
            startsAvailable = availableAtStart;
            repeatable = canRepeat;
            interactionDistance = Mathf.Max(0.1f, distance);
            useTriggerRange = requireTrigger;
            interactionPoints = points ?? Array.Empty<Transform>();
        }

        public void SetAvailable(bool available)
        {
            SetAvailable(available, lockedPrompt);
        }

        public void ConfigurePercentLabel(Text label)
        {
            percentLabel = label;
            SetProgress(0f);
            percentLabel?.gameObject.SetActive(false);
        }

        public void SetAvailable(bool available, string unavailablePrompt)
        {
            lockedPrompt = unavailablePrompt;
            isAvailable = available && !isCompleted;
            if (!isAvailable)
            {
                ResetProgress();
                HideProgress();
            }

            if (activeInteraction == this)
            {
                ShowPrompt();
            }
        }

        public void ResetForReuse()
        {
            if (!repeatable)
            {
                return;
            }

            isCompleted = false;
            isAvailable = true;
            requiresRelease = true;
            ResetProgress();
            HideProgress();
        }

        private void EvaluateRange()
        {
            currentSqrDistance = GetClosestSqrDistance();
            bool distanceInRange = currentSqrDistance <= interactionDistance * interactionDistance;
            isInRange = useTriggerRange ? isInsideTrigger : distanceInRange;

            if (!isInRange && activeInteraction == this)
            {
                activeInteraction = null;
                CancelAndHide();
            }
        }

        private void ProcessActiveInteraction()
        {
            if (!isInRange || isCompleted)
            {
                CancelAndHide();
                return;
            }

            ShowPrompt();
            if (!isAvailable || interactAction == null)
            {
                ResetProgress();
                HideProgress();
                return;
            }

            if (requiresRelease)
            {
                ResetProgress();
                HideProgress();
                if (!interactAction.IsPressed())
                {
                    requiresRelease = false;
                }

                return;
            }

            if (!interactAction.IsPressed())
            {
                ResetProgress();
                HideProgress();
                return;
            }

            if (!isHolding)
            {
                isHolding = true;
                heldTime = 0f;
                StartHoldSound();
                SetProgress(0f);
                ShowProgress();
                return;
            }

            ShowProgress();
            heldTime += Time.unscaledDeltaTime;
            SetProgress(heldTime / holdDuration);
            if (heldTime < holdDuration)
            {
                return;
            }

            SetProgress(1f);
            isCompleted = true;
            isAvailable = false;
            requiresRelease = true;
            HidePrompt();
            Completed?.Invoke();
            StartCoroutine(HideCompletedProgress());
        }

        private static HoldInteractionController FindClosestFor(Transform targetPlayer)
        {
            HoldInteractionController closest = null;
            float closestDistance = float.PositiveInfinity;
            foreach (HoldInteractionController candidate in Instances)
            {
                if (candidate == null ||
                    !candidate.isActiveAndEnabled ||
                    candidate.player != targetPlayer ||
                    !candidate.isInRange ||
                    candidate.isCompleted)
                {
                    continue;
                }

                if (candidate.currentSqrDistance < closestDistance)
                {
                    closest = candidate;
                    closestDistance = candidate.currentSqrDistance;
                }
            }

            return closest;
        }

        private float GetClosestSqrDistance()
        {
            if (player == null)
            {
                return float.PositiveInfinity;
            }

            float closest = (transform.position - player.position).sqrMagnitude;
            foreach (Transform point in interactionPoints)
            {
                if (point != null)
                {
                    closest = Mathf.Min(closest, (point.position - player.position).sqrMagnitude);
                }
            }

            return closest;
        }

        private bool IsPlayer(Collider other)
        {
            if (player == null || other == null)
            {
                return false;
            }

            CharacterController controller = other.GetComponentInParent<CharacterController>();
            return controller != null && controller.transform == player;
        }

        private void CancelAndHide()
        {
            ResetProgress();
            HideAllUI();
        }

        private void ResetProgress()
        {
            heldTime = 0f;
            isHolding = false;
            StopHoldSound();
            SetProgress(0f);
        }

        private void StartHoldSound()
        {
            if (holdSource == null || reversedHoldClip == null)
            {
                return;
            }

            holdSource.Stop();
            holdSource.clip = reversedHoldClip;
            holdSource.volume = holdVolume;
            holdSource.pitch = 1f;
            holdSource.time = 0f;
            holdSource.Play();
            if (!hasLoggedHoldPlayback)
            {
                hasLoggedHoldPlayback = true;
                Debug.Log($"[Hold Interaction] Playing reversed hold sound '{holdClip.name}' at volume {holdVolume:0.00}.", this);
            }
        }

        private static AudioClip CreateReversedClip(AudioClip source)
        {
            if (source == null)
            {
                return null;
            }

            int sampleCount = source.samples * source.channels;
            float[] samples = new float[sampleCount];
            if (!source.GetData(samples, 0))
            {
                Debug.LogWarning($"[Hold Interaction] Could not read '{source.name}' for reverse playback.");
                return null;
            }

            int channels = source.channels;
            int frameCount = source.samples;
            for (int leftFrame = 0, rightFrame = frameCount - 1; leftFrame < rightFrame; leftFrame++, rightFrame--)
            {
                int leftOffset = leftFrame * channels;
                int rightOffset = rightFrame * channels;
                for (int channel = 0; channel < channels; channel++)
                {
                    (samples[leftOffset + channel], samples[rightOffset + channel]) =
                        (samples[rightOffset + channel], samples[leftOffset + channel]);
                }
            }

            AudioClip reversed = AudioClip.Create(
                $"{source.name} (Reversed)",
                frameCount,
                channels,
                source.frequency,
                false);
            reversed.SetData(samples, 0);
            return reversed;
        }

        private void StopHoldSound()
        {
            if (holdSource != null && holdSource.isPlaying)
            {
                holdSource.Stop();
            }
        }

        private void SetProgress(float normalized)
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(normalized);
            }

            if (percentLabel != null)
            {
                percentLabel.text = $"{Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f)}%";
            }
        }

        private void ShowPrompt()
        {
            if (promptLabel == null)
            {
                return;
            }

            promptLabel.text = isAvailable ? prompt : lockedPrompt;
            promptLabel.gameObject.SetActive(true);
        }

        private void HidePrompt()
        {
            if (promptLabel != null)
            {
                promptLabel.gameObject.SetActive(false);
            }
        }

        private void ShowProgress()
        {
            if (progressRoot != null)
            {
                progressRoot.alpha = 1f;
            }

            if (percentLabel != null)
            {
                percentLabel.gameObject.SetActive(true);
            }
        }

        private void HideProgress()
        {
            heldTime = 0f;
            isHolding = false;
            SetProgress(0f);
            if (progressRoot != null)
            {
                progressRoot.alpha = 0f;
                progressRoot.blocksRaycasts = false;
                progressRoot.interactable = false;
            }

            if (percentLabel != null)
            {
                percentLabel.gameObject.SetActive(false);
            }
        }

        private void HideAllUI()
        {
            HidePrompt();
            HideProgress();
        }

        private IEnumerator HideCompletedProgress()
        {
            yield return null;
            HideProgress();
        }
    }
}
