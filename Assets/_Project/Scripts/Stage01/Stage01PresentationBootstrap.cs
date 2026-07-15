using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TheFusionEngineer.Stage01
{
    public static class Stage01PresentationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            if (SceneManager.GetActiveScene().name != "Stage01_Origin") return;
            CreateLogo();
            CreateExterior();
        }

        private static void CreateLogo()
        {
            if (GameObject.Find("Corning_CI_WallPrint") != null) return;
            Texture2D texture = Resources.Load<Texture2D>("Branding/Corning_CI");
            if (texture == null) return;

            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;

            GameObject logo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            logo.name = "Corning_CI_WallPrint";
            Object.Destroy(logo.GetComponent<Collider>());
            logo.transform.SetPositionAndRotation(new Vector3(-9.84f, 3.15f, 0f), Quaternion.Euler(0f, -90f, 0f));
            logo.transform.localScale = new Vector3(6.4f, 2.72f, 1f);
            logo.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateExterior()
        {
            if (GameObject.Find("Corning_Exterior_Campus") != null) return;
            var root = new GameObject("Corning_Exterior_Campus");
            Material facade = MakeLit(new Color(0.07f, 0.14f, 0.2f), 0.35f, 0.6f);
            Material glass = MakeLit(new Color(0.03f, 0.28f, 0.46f), 0.55f, 0.92f);
            Material glow = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            glow.color = new Color(0.16f, 0.65f, 1f);

            Box(root.transform, "Research_Center", new Vector3(0f, 5f, 28f), new Vector3(34f, 10f, 4f), facade);
            Box(root.transform, "Glass_Band", new Vector3(0f, 5.2f, 25.92f), new Vector3(30f, 3.2f, 0.12f), glass);
            for (int x = -14; x <= 14; x += 4)
                Box(root.transform, "WindowLight", new Vector3(x, 5.2f, 25.84f), new Vector3(0.08f, 2.7f, 0.08f), glow);
            Box(root.transform, "Utility_Wing_Left", new Vector3(-25f, 3f, 34f), new Vector3(15f, 6f, 12f), facade);
            Box(root.transform, "Utility_Wing_Right", new Vector3(25f, 4f, 36f), new Vector3(16f, 8f, 14f), facade);
            Box(root.transform, "Horizon_Light", new Vector3(0f, 0.05f, 24f), new Vector3(80f, 0.08f, 2f), glow);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.055f, 0.11f, 0.17f);
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.12f, 0.2f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.055f, 0.11f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.025f, 0.04f, 0.055f);
        }

        private static Material MakeLit(Color color, float metallic, float smoothness)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.SetPositionAndRotation(position, Quaternion.identity);
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.Destroy(box.GetComponent<Collider>());
        }
    }
}
