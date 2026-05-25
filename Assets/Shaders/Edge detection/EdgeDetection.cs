using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class EdgeDetection : ScriptableRendererFeature
{
    private class EdgeDetectionPass : ScriptableRenderPass
    {
        private Material material;

        private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");

        private static readonly int NoiseTexProperty = Shader.PropertyToID("_NoiseTex");
        private static readonly int NoiseScaleProperty = Shader.PropertyToID("_NoiseScale");
        private static readonly int NoiseStrengthProperty = Shader.PropertyToID("_NoiseStrength");

        private static readonly int DepthThresholdProperty = Shader.PropertyToID("_DepthThreshold");
        private static readonly int NormalThresholdProperty = Shader.PropertyToID("_NormalThreshold");
        private static readonly int LuminanceThresholdProperty = Shader.PropertyToID("_LuminanceThreshold");

        private static readonly int OutlineFadeStartProperty = Shader.PropertyToID("_OutlineFadeStart");
        private static readonly int OutlineFadeEndProperty = Shader.PropertyToID("_OutlineFadeEnd");

        public EdgeDetectionPass()
        {
            profilingSampler = new ProfilingSampler(nameof(EdgeDetectionPass));
        }

        public void Setup(ref EdgeDetectionSettings settings, ref Material edgeDetectionMaterial)
        {
            material = edgeDetectionMaterial;
            renderPassEvent = settings.renderPassEvent;

            material.SetFloat(OutlineThicknessProperty, settings.outlineThickness);
            material.SetColor(OutlineColorProperty, settings.outlineColor);

            material.SetTexture(NoiseTexProperty, settings.noiseTexture);
            material.SetFloat(NoiseScaleProperty, settings.noiseScale);
            material.SetFloat(NoiseStrengthProperty, settings.noiseStrength);

            material.SetFloat(DepthThresholdProperty, settings.depthThreshold);
            material.SetFloat(NormalThresholdProperty, settings.normalThreshold);
            material.SetFloat(LuminanceThresholdProperty, settings.luminanceThreshold);

            material.SetFloat(OutlineFadeStartProperty, settings.outlineFadeStart);
            material.SetFloat(OutlineFadeEndProperty, settings.outlineFadeEnd);
        }

        private class PassData
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData>("Edge Detection", out _);

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData _, RasterGraphContext context) => { Blitter.BlitTexture(context.cmd, Vector2.one, material, 0); });
        }
    }

    [Serializable]
    public class EdgeDetectionSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Header("Outline")]
        [Range(0, 15)] public float outlineThickness = 3;
        public Color outlineColor = Color.black;

        public float depthThreshold = 200f;
        public float normalThreshold = 4f;
        public float luminanceThreshold = 1f;

        [Header("Fadeout")]
        public float outlineFadeStart = 15f;
        public float outlineFadeEnd = 25f;

        [Header("Noise")]
        public Texture2D noiseTexture;
        public float noiseScale = 50f;
        [Range(0, 1)] public float noiseStrength = 1f;
    }

    [SerializeField] private EdgeDetectionSettings settings;
    private Material edgeDetectionMaterial;
    private EdgeDetectionPass edgeDetectionPass;

    /// Called
    /// - When the Scriptable Renderer Feature loads the first time.
    /// - When you enable or disable the Scriptable Renderer Feature.
    /// - When you change a property in the Inspector window of the Renderer Feature.

    public override void Create()
    {
        edgeDetectionPass ??= new EdgeDetectionPass();
    }

    /// Called
    /// - Every frame, once for each camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Don't render for some views.
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || renderingData.cameraData.cameraType == CameraType.Reflection
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;

        if (edgeDetectionMaterial == null)
        {
            edgeDetectionMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Edge Detection"));
            if (edgeDetectionMaterial == null)
            {
                Debug.LogWarning("Not all required materials could be created. Edge Detection will not render.");
                return;
            }
        }

        edgeDetectionPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color);
        edgeDetectionPass.requiresIntermediateTexture = true;
        edgeDetectionPass.Setup(ref settings, ref edgeDetectionMaterial);

        renderer.EnqueuePass(edgeDetectionPass);
    }

    /// Clean up resources allocated to the Scriptable Renderer Feature such as materials.
    override protected void Dispose(bool disposing)
    {
        edgeDetectionPass = null;
        CoreUtils.Destroy(edgeDetectionMaterial);
    }
}