using UnityEditor;
using UnityEngine;

/// <summary>
/// Project 창에서 선택한 AnimationClip을 자동으로 반복 재생하는 Editor 전용 창입니다.
/// </summary>
public sealed class AnimationAutoPreviewWindow : EditorWindow
{
    private const float MinimumModelSize = 1f;

    private GameObject previewModel;
    private AnimationClip selectedClip;
    private PreviewRenderUtility previewUtility;
    private GameObject previewInstance;

    private bool isPlaying = true;
    private float previewTime;
    private double previousEditorTime;

    [MenuItem("Tools/Animation Auto Preview")]
    // OpenWindow 관련 게임 로직을 수행합니다.
    private static void OpenWindow()
    {
        GetWindow<AnimationAutoPreviewWindow>("Animation Preview");
    }

    // Unity가 컴포넌트를 활성화할 때 입력과 이벤트 연결을 시작합니다.
    private void OnEnable()
    {
        CreatePreviewUtility();

        Selection.selectionChanged += OnProjectSelectionChanged;
        EditorApplication.update += UpdatePreview;

        previousEditorTime = EditorApplication.timeSinceStartup;
        OnProjectSelectionChanged();
    }

    // Unity가 컴포넌트를 비활성화할 때 입력과 이벤트 연결을 정리합니다.
    private void OnDisable()
    {
        Selection.selectionChanged -= OnProjectSelectionChanged;
        EditorApplication.update -= UpdatePreview;

        DestroyPreviewInstance();
        previewUtility?.Cleanup();
        previewUtility = null;
    }

    // Unity Editor 화면과 미리보기 상태를 갱신합니다.
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Animation Auto Preview", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        GameObject nextPreviewModel = (GameObject)EditorGUILayout.ObjectField(
            "Preview Character",
            previewModel,
            typeof(GameObject),
            false);

        AnimationClip nextClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip",
            selectedClip,
            typeof(AnimationClip),
            false);

        if (EditorGUI.EndChangeCheck())
        {
            bool modelChanged = nextPreviewModel != previewModel;
            previewModel = nextPreviewModel;
            selectedClip = nextClip;
            previewTime = 0f;

            if (modelChanged)
                RebuildPreviewInstance();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(isPlaying ? "Pause" : "Play"))
            {
                isPlaying = !isPlaying;
                previousEditorTime = EditorApplication.timeSinceStartup;
            }

            if (GUILayout.Button("Restart"))
            {
                previewTime = 0f;
                SampleCurrentFrame();
                Repaint();
            }
        }

        if (selectedClip != null)
        {
            EditorGUILayout.LabelField(
                $"Clip: {selectedClip.name} / {selectedClip.length:0.00}s");
        }

        Rect previewRect = GUILayoutUtility.GetRect(
            10f,
            10000f,
            200f,
            10000f,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));

        DrawPreview(previewRect);
    }

    // [런타임 자동 생성] 필요한 게임 오브젝트와 컴포넌트 계층을 구성합니다.
    private void CreatePreviewUtility()
    {
        previewUtility = new PreviewRenderUtility
        {
            cameraFieldOfView = 30f
        };

        previewUtility.camera.nearClipPlane = 0.01f;
        previewUtility.camera.farClipPlane = 1000f;
        previewUtility.camera.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

        previewUtility.lights[0].intensity = 1.2f;
        previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
        previewUtility.lights[1].intensity = 1.2f;
    }

    /// <summary>
    /// Project 창에서 AnimationClip을 선택하면 즉시 미리보기 대상으로 지정합니다.
    /// </summary>
    private void OnProjectSelectionChanged()
    {
        if (Selection.activeObject is not AnimationClip clip)
            return;

        selectedClip = clip;
        previewTime = 0f;
        isPlaying = true;
        previousEditorTime = EditorApplication.timeSinceStartup;

        // FBX 내부 클립인 경우, 아직 캐릭터를 지정하지 않았다면 같은 FBX 모델을 사용합니다.
        if (previewModel == null)
        {
            string assetPath = AssetDatabase.GetAssetPath(clip);
            previewModel = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;
        }

        RebuildPreviewInstance();
        Repaint();
    }

    // UpdatePreview 관련 게임 로직을 수행합니다.
    private void UpdatePreview()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTime - previousEditorTime);
        previousEditorTime = currentTime;

        if (!isPlaying || selectedClip == null || previewInstance == null)
            return;

        previewTime += deltaTime;

        if (selectedClip.length > 0f)
            previewTime = Mathf.Repeat(previewTime, selectedClip.length);

        SampleCurrentFrame();
        Repaint();
    }

    // SampleCurrentFrame 관련 게임 로직을 수행합니다.
    private void SampleCurrentFrame()
    {
        if (selectedClip == null || previewInstance == null)
            return;

        // HideAndDontSave 복제본만 변경하므로 실제 프리팹과 씬 오브젝트에는 영향이 없습니다.
        selectedClip.SampleAnimation(previewInstance, previewTime);
    }

    // RebuildPreviewInstance 관련 게임 로직을 수행합니다.
    private void RebuildPreviewInstance()
    {
        DestroyPreviewInstance();

        // PreviewRenderUtility는 추가된 오브젝트를 개별 제거하는 API가 없으므로
        // 캐릭터를 바꿀 때 유틸리티도 함께 재생성해 참조가 누적되지 않게 합니다.
        previewUtility?.Cleanup();
        CreatePreviewUtility();

        if (previewModel == null || previewUtility == null)
            return;

        previewInstance = Instantiate(previewModel);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;
        previewInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        SetHideFlagsRecursively(previewInstance.transform);
        previewUtility.AddSingleGO(previewInstance);

        SampleCurrentFrame();
        PositionPreviewCamera();
    }

    // PositionPreviewCamera 관련 게임 로직을 수행합니다.
    private void PositionPreviewCamera()
    {
        Bounds bounds = CalculateBounds(previewInstance);
        float modelSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        modelSize = Mathf.Max(modelSize, MinimumModelSize);

        Vector3 target = bounds.center;
        Vector3 direction = new Vector3(0.8f, 0.35f, -1f).normalized;

        previewUtility.camera.transform.position = target + direction * modelSize * 2.5f;
        previewUtility.camera.transform.LookAt(target);
    }

    // DrawPreview 관련 게임 로직을 수행합니다.
    private void DrawPreview(Rect previewRect)
    {
        if (previewUtility == null || previewInstance == null)
        {
            EditorGUI.HelpBox(
                previewRect,
                "Preview Character와 Animation Clip을 지정해주세요.",
                MessageType.Info);
            return;
        }

        previewUtility.BeginPreview(previewRect, GUIStyle.none);
        previewUtility.camera.Render();
        Texture result = previewUtility.EndPreview();

        GUI.DrawTexture(previewRect, result, ScaleMode.StretchToFill, false);
    }

    // DestroyPreviewInstance 관련 게임 로직을 수행합니다.
    private void DestroyPreviewInstance()
    {
        if (previewInstance == null)
            return;

        DestroyImmediate(previewInstance);
        previewInstance = null;
    }

    // CalculateBounds 관련 게임 로직을 수행합니다.
    private static Bounds CalculateBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            // Bounds 관련 게임 로직을 수행합니다.
            return new Bounds(target.transform.position, Vector3.one * 2f);

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    // 전달받은 값에 맞춰 내부 상태와 화면 표시를 갱신합니다.
    private static void SetHideFlagsRecursively(Transform root)
    {
        root.gameObject.hideFlags = HideFlags.HideAndDontSave;

        foreach (Transform child in root)
            SetHideFlagsRecursively(child);
    }
}
