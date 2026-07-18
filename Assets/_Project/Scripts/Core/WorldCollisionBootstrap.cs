using System.Collections;
using TheFusionEngineer.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheFusionEngineer.Core
{
    /// <summary>
    /// 스테이지의 고형 환경 Mesh에 누락된 Collider를 런타임에서 한 번만 보완합니다.
    /// UI, 이펙트, 웨이포인트, 플레이어와 Rigidbody 기반 동적 오브젝트는 자동으로 제외합니다.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class WorldCollisionBootstrap : MonoBehaviour
    {
        private static readonly string[] NonSolidNameTokens =
        {
            "waypoint",
            "lanemark",
            "label",
            "indicator",
            "guidance",
            "energy",
            "glow",
            "vfx",
            "particle",
            "beam",
            "line",
            "trigger",
            "spawn",
            "canvas",
            "progress",
            "text",
            "zone"
        };

        private Coroutine applyRoutine;
        private ulong processedSceneHandle = ulong.MaxValue;

        // Unity가 첫 프레임 전에 게임 진행 상태를 초기화합니다.
        private void Start()
        {
            ScheduleCollisionPass(SceneManager.GetActiveScene());
        }

        // Unity가 매 프레임 호출하며 입력과 현재 상태에 따른 동작을 갱신합니다.
        private void Update()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (applyRoutine == null &&
                activeScene.IsValid() &&
                activeScene.isLoaded &&
                activeScene.handle.GetRawData() != processedSceneHandle)
            {
                ScheduleCollisionPass(activeScene);
            }
        }

        // ScheduleCollisionPass 관련 게임 로직을 수행합니다.
        private void ScheduleCollisionPass(Scene scene)
        {
            if (applyRoutine != null)
            {
                StopCoroutine(applyRoutine);
            }

            applyRoutine = StartCoroutine(ApplyAfterSceneInitialization(scene));
        }

        // ApplyAfterSceneInitialization 관련 게임 로직을 수행합니다.
        private IEnumerator ApplyAfterSceneInitialization(Scene scene)
        {
            // 씬의 Awake/Start에서 생성하거나 재배치하는 오브젝트까지 반영합니다.
            yield return null;
            yield return null;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                applyRoutine = null;
                yield break;
            }

            int boxColliderCount = 0;
            int meshColliderCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer renderer in renderers)
                {
                    if (!ShouldAddCollision(renderer))
                    {
                        continue;
                    }

                    if (RequiresShapeAccurateCollision(renderer))
                    {
                        MeshCollider collider =
                            // [런타임 자동 생성] 메시 형태에 맞춘 고정 월드 충돌체입니다.
                            renderer.gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh =
                            renderer.GetComponents<MeshFilter>()[0].sharedMesh;
                        collider.convex = false;
                        meshColliderCount++;
                    }
                    else
                    {
                        BoxCollider collider =
                            // [런타임 자동 생성] 복잡한 메시 대신 가벼운 박스 충돌체를 붙입니다.
                            renderer.gameObject.AddComponent<BoxCollider>();
                        collider.isTrigger = false;
                        boxColliderCount++;
                    }
                }
            }

            Debug.Log(
                $"[World Collision] Added {boxColliderCount} BoxColliders and " +
                $"{meshColliderCount} shape-accurate MeshColliders in '{scene.name}'.",
                this);
            processedSceneHandle = scene.handle.GetRawData();
            applyRoutine = null;
        }

        // RequiresShapeAccurateCollision 관련 게임 로직을 수행합니다.
        private static bool RequiresShapeAccurateCollision(
            MeshRenderer renderer)
        {
            Vector3 size = renderer.bounds.size;
            int largeAxisCount = 0;
            if (size.x >= 6f) largeAxisCount++;
            if (size.y >= 6f) largeAxisCount++;
            if (size.z >= 6f) largeAxisCount++;

            // A box around a large three-dimensional building can seal its
            // walkable interior. Thin floors and walls remain inexpensive boxes.
            return largeAxisCount == 3 && Mathf.Max(size.x, size.y, size.z) >= 12f;
        }

        // ShouldAddCollision 관련 게임 로직을 수행합니다.
        private static bool ShouldAddCollision(MeshRenderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            if (renderer.GetComponent<TMPro.TMP_Text>() != null)
            {
                return false;
            }

            MeshFilter[] meshFilters = renderer.GetComponents<MeshFilter>();
            if (meshFilters.Length == 0 ||
                meshFilters[0].sharedMesh == null ||
                renderer.GetComponent<Collider>() != null ||
                renderer.GetComponentInParent<PlayerMovement>(true) != null ||
                renderer.GetComponentInParent<CharacterController>(true) != null ||
                renderer.GetComponentInParent<Rigidbody>(true) != null ||
                renderer.GetComponentInParent<Animator>(true) != null ||
                renderer.GetComponentInParent<Camera>(true) != null ||
                renderer.GetComponentInParent<NonSolidVisual>(true) != null)
            {
                return false;
            }

            if (HasNonTriggerColliderInParents(renderer.transform.parent))
            {
                return false;
            }

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            foreach (string token in NonSolidNameTokens)
            {
                if (objectName.Contains(token))
                {
                    return false;
                }
            }

            return true;
        }

        // 필요한 실행 조건을 검사하고 조건을 만족할 때만 동작을 수행합니다.
        private static bool HasNonTriggerColliderInParents(Transform parent)
        {
            while (parent != null)
            {
                foreach (Collider collider in parent.GetComponents<Collider>())
                {
                    if (collider != null && !collider.isTrigger)
                    {
                        return true;
                    }
                }

                parent = parent.parent;
            }

            return false;
        }
    }
}
