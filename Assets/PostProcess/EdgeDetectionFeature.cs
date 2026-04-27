using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class EdgeDetectionFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        // 显式提示用户把参数放这里调
        [Header("Material Reference")]
        public Material material = null;

        [Header("Edge Settings (Adjust Here)")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        [Tooltip("基础描边宽度，会自动根据分辨率缩放")]
        [Range(0, 5)] public float edgeWidth = 1.0f;
        public Color edgeColor = Color.black;

        [Header("Sensitivity")]
        [Range(0.0f, 10.0f)] public float depthSensitivity = 1.5f;
        [Range(0.0f, 5.0f)] public float normalsSensitivity = 1.0f;
    }

    public Settings settings = new Settings();
    EdgeDetectionPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new EdgeDetectionPass(settings);
        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null) return;
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();
    }

    class EdgeDetectionPass : ScriptableRenderPass
    {
        Settings settings;
        Material m_MaterialInstance; // 运行时实例，避免修改资源文件
        RTHandle m_TempRT;

        private class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle destination;
        }

        public EdgeDetectionPass(Settings settings)
        {
            this.settings = settings;
        }

        public void Dispose()
        {
            m_TempRT?.Release();
            if (m_MaterialInstance != null)
            {
                CoreUtils.Destroy(m_MaterialInstance);
                m_MaterialInstance = null;
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // RTHandle 在 URP 内部管理，这里不需要显式 release 除非是我们自己分配的
        }

        // 获取或创建材质实例
        private Material GetMaterialInstance()
        {
            if (settings.material == null) return null;

            // 如果实例不存在，或者源材质变了（比如用户拖了新材质进去），则重新创建
            if (m_MaterialInstance == null || m_MaterialInstance.shader != settings.material.shader)
            {
                if (m_MaterialInstance != null) CoreUtils.Destroy(m_MaterialInstance);
                m_MaterialInstance = new Material(settings.material);
            }
            return m_MaterialInstance;
        }

        private void UpdateMaterialParameters(Material mat, UniversalCameraData cameraData)
        {
            if (mat == null) return;

            // 分辨率缩放逻辑
            float resolutionScale = cameraData.cameraTargetDescriptor.height / 1080.0f;
            resolutionScale = Mathf.Max(resolutionScale, 0.5f);

            // 设置参数到临时实例上
            mat.SetFloat("_EdgeWidth", settings.edgeWidth * resolutionScale);
            mat.SetColor("_EdgeColor", settings.edgeColor);
            mat.SetFloat("_SensitivityDepth", settings.depthSensitivity);
            mat.SetFloat("_SensitivityNormals", settings.normalsSensitivity);
        }

        // ---------------------------------------------------------------------
        // Render Graph 实现
        // ---------------------------------------------------------------------
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            Material mat = GetMaterialInstance();

            if (mat == null || cameraData.cameraType == CameraType.Preview) return;

            UpdateMaterialParameters(mat, cameraData);

            TextureHandle source = resourceData.activeColorTexture;
            RenderTextureDescriptor rtDesc = cameraData.cameraTargetDescriptor;
            
            TextureDesc desc = new TextureDesc(rtDesc.width, rtDesc.height);
            desc.colorFormat = rtDesc.graphicsFormat;
            desc.depthBufferBits = DepthBits.None;
            desc.msaaSamples = MSAASamples.None;
            desc.name = "EdgeDetectionTarget";
            
            TextureHandle destination = renderGraph.CreateTexture(desc);

            // Pass 1: Apply
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Edge Detection Apply", out var passData))
            {
                passData.material = mat;
                passData.source = source;
                passData.destination = destination;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Pass 2: Copy Back
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Edge Detection CopyBack", out var passData))
            {
                passData.material = null;
                passData.source = destination;
                passData.destination = source;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }
        }

        // // ---------------------------------------------------------------------
        // // 兼容模式实现
        // // ---------------------------------------------------------------------
        // public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        // {
        //     Material mat = GetMaterialInstance();
        //     if (mat == null) return;

        //     var cameraData = renderingData.cameraData;
        //     UpdateMaterialParameters(mat, cameraData);

        //     CommandBuffer cmd = CommandBufferPool.Get("Edge Detection");
        //     var source = cameraData.renderer.cameraColorTargetHandle;

        //     RenderingUtils.ReAllocateIfNeeded(ref m_TempRT, cameraData.cameraTargetDescriptor, name: "_TempEdgeTexture");

        //     Blitter.BlitCameraTexture(cmd, source, m_TempRT, mat, 0);
        //     Blitter.BlitCameraTexture(cmd, m_TempRT, source);
            
        //     context.ExecuteCommandBuffer(cmd);
        //     CommandBufferPool.Release(cmd);
        // }
    }
}