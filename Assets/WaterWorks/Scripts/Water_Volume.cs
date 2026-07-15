using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class Water_Volume : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material _material;

        public CustomRenderPass(Material material)
        {
            _material = material;
            requiresIntermediateTexture = true;
        }

        public void Setup(Material material)
        {
            _material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType == CameraType.Reflection || _material == null)
            {
                return;
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc descriptor = source.GetDescriptor(renderGraph);
            descriptor.name = "_TemporaryColourTexture";
            descriptor.clearBuffer = false;
            descriptor.msaaSamples = MSAASamples.None;

            TextureHandle temporary = renderGraph.CreateTexture(descriptor);
            var effectParameters = new RenderGraphUtils.BlitMaterialParameters(
                source,
                temporary,
                _material,
                0);

            renderGraph.AddBlitPass(effectParameters, "Water Volume Effect");
            renderGraph.AddBlitPass(
                temporary,
                source,
                Vector2.one,
                Vector2.zero,
                passName: "Water Volume Copy Back");
        }
    }

    [System.Serializable]
    public class _Settings
    {
        public Material material = null;
        public RenderPassEvent renderPass = RenderPassEvent.AfterRenderingSkybox;
    }

    public _Settings settings = new _Settings();

    CustomRenderPass m_ScriptablePass;

    public override void Create()
    {
        if (settings.material == null)
        {
            settings.material = Resources.Load<Material>("Water_Volume");
        }

        m_ScriptablePass = new CustomRenderPass(settings.material)
        {
            renderPassEvent = settings.renderPass
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_ScriptablePass == null ||
            settings.material == null ||
            renderingData.cameraData.cameraType == CameraType.Reflection)
        {
            return;
        }

        m_ScriptablePass.renderPassEvent = settings.renderPass;
        m_ScriptablePass.Setup(settings.material);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass = null;
    }
}
