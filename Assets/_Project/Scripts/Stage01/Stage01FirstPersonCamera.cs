using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace TheFusionEngineer.Stage01
{
    public sealed class Stage01FirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Vector3 eyeOffset = new(0f, 0.62f, 0f);
        [SerializeField] private float sensitivity = 0.09f;
        [SerializeField] private float pitchLimit = 80f;

        private readonly Dictionary<Renderer, ShadowCastingMode> originalModes = new();
        private float pitch;

        public void Configure(Transform target)
        {
            player = target;
            transform.SetParent(player, false);
            transform.localPosition = eyeOffset;
            transform.localRotation = Quaternion.identity;
            ApplyFirstPersonRendering();
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ApplyFirstPersonRendering();
        }

        private void OnDisable()
        {
            foreach (var pair in originalModes)
                if (pair.Key != null) pair.Key.shadowCastingMode = pair.Value;
            originalModes.Clear();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (player == null || Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;
            Vector2 look = Mouse.current.delta.ReadValue();
            player.Rotate(Vector3.up, look.x * sensitivity, Space.World);
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, -pitchLimit, pitchLimit);
            transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void ApplyFirstPersonRendering()
        {
            if (player == null || originalModes.Count > 0) return;
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform == player) continue;
                string lowerName = renderer.name.ToLowerInvariant();
                if (renderer is SkinnedMeshRenderer || lowerName.Contains("head") || lowerName.Contains("body"))
                {
                    originalModes[renderer] = renderer.shadowCastingMode;
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }
    }
}
