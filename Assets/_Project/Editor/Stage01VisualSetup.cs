#if UNITY_EDITOR
using TheFusionEngineer.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public static class Stage01VisualSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Stage01_Origin.unity";
    private const string MaterialFolder = "Assets/_Project/Materials/Stage01";

    [MenuItem("The Fusion Engineer/Apply Stage 01 Corning FPS Look")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureFolder("Assets/_Project/Materials", "Stage01");

        SetupCorningLogo();
        SetupExterior();
        SetupThirdPersonCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Stage01 Corning branding, exterior, and FPS camera setup complete.");
    }

    private static void SetupCorningLogo()
    {
        var existing = GameObject.Find("Corning_CI_WallPrint");
        if (existing != null) Object.DestroyImmediate(existing);

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Branding/Corning_CI.png");
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var material = LoadOrCreateMaterial("Corning_CI_WallPrint", shader);
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);

        var logo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        logo.name = "Corning_CI_WallPrint";
        Object.DestroyImmediate(logo.GetComponent<Collider>());
        logo.transform.SetPositionAndRotation(new Vector3(-9.84f, 3.15f, 0f), Quaternion.Euler(0f, -90f, 0f));
        logo.transform.localScale = new Vector3(6.4f, 2.72f, 1f);
        logo.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void SetupExterior()
    {
        var previous = GameObject.Find("Corning_Exterior_Campus");
        if (previous != null) Object.DestroyImmediate(previous);
        var root = new GameObject("Corning_Exterior_Campus");

        var facade = LoadOrCreateMaterial("Exterior_Facade", Shader.Find("Universal Render Pipeline/Lit"));
        facade.color = new Color(0.07f, 0.14f, 0.2f);
        facade.SetFloat("_Metallic", 0.35f);
        facade.SetFloat("_Smoothness", 0.6f);
        var glass = LoadOrCreateMaterial("Exterior_Glass", Shader.Find("Universal Render Pipeline/Lit"));
        glass.color = new Color(0.03f, 0.28f, 0.46f);
        glass.SetFloat("_Metallic", 0.55f);
        glass.SetFloat("_Smoothness", 0.92f);
        var glow = LoadOrCreateMaterial("Exterior_Light", Shader.Find("Universal Render Pipeline/Unlit"));
        glow.color = new Color(0.16f, 0.65f, 1f);

        CreateBox(root.transform, "Research_Center", new Vector3(0f, 5f, 28f), new Vector3(34f, 10f, 4f), facade);
        CreateBox(root.transform, "Glass_Band", new Vector3(0f, 5.2f, 25.92f), new Vector3(30f, 3.2f, 0.12f), glass);
        for (int x = -14; x <= 14; x += 4)
            CreateBox(root.transform, "WindowLight", new Vector3(x, 5.2f, 25.84f), new Vector3(0.08f, 2.7f, 0.08f), glow);
        CreateBox(root.transform, "Utility_Wing_Left", new Vector3(-25f, 3f, 34f), new Vector3(15f, 6f, 12f), facade);
        CreateBox(root.transform, "Utility_Wing_Right", new Vector3(25f, 4f, 36f), new Vector3(16f, 8f, 14f), facade);
        CreateBox(root.transform, "Horizon_Light", new Vector3(0f, 0.05f, 24f), new Vector3(80f, 0.08f, 2f), glow);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.055f, 0.11f, 0.17f);
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.12f, 0.2f, 0.3f);
        RenderSettings.ambientEquatorColor = new Color(0.055f, 0.11f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.04f, 0.055f);
    }

    private static void SetupThirdPersonCamera()
    {
        var player = GameObject.Find("Player");
        var camera = GameObject.Find("Main Camera");
        if (player == null || camera == null) return;

        camera.transform.SetParent(null, true);
        var follow = camera.GetComponent<CameraFollow>() ?? camera.AddComponent<CameraFollow>();
        follow.Configure(player.transform, new Vector3(8.5f, 9.5f, -8.5f));
        camera.transform.position = player.transform.position + new Vector3(8.5f, 9.5f, -8.5f);
        camera.transform.LookAt(player.transform.position + Vector3.up);
        camera.GetComponent<Camera>().fieldOfView = 50f;
    }

    private static Material LoadOrCreateMaterial(string name, Shader shader)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.SetPositionAndRotation(position, Quaternion.identity);
        box.transform.localScale = scale;
        box.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(box.GetComponent<Collider>());
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
