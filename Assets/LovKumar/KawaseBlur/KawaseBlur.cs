//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Rendering;
//using UnityEngine.Rendering.Universal;

//public class KawaseBlur : ScriptableRendererFeature
//{
//    [System.Serializable]
//    public class KawaseBlurSettings
//    {
//        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
//        public Material blurMaterial = null;

//        [Range(2,15)]
//        public int blurPasses = 1;

//        [Range(1,4)]
//        public int downsample = 1;
//        public bool copyToFramebuffer;
//        public string targetName = "_blurTexture";
//    }

//    public KawaseBlurSettings settings = new KawaseBlurSettings();

//    class CustomRenderPass : ScriptableRenderPass
//    {
//        public Material blurMaterial;
//        public int passes;
//        public int downsample;
//        public bool copyToFramebuffer;
//        public string targetName;        
//        string profilerTag;

//        int tmpId1;
//        int tmpId2;

//        RenderTargetIdentifier tmpRT1;
//        RenderTargetIdentifier tmpRT2;

//        private RenderTargetIdentifier source { get; set; }

//        public void Setup(RenderTargetIdentifier source) {
//            this.source = source;
//        }

//        public CustomRenderPass(string profilerTag)
//        {
//            this.profilerTag = profilerTag;
//        }

//        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
//        {
//            var width = cameraTextureDescriptor.width / downsample;
//            var height = cameraTextureDescriptor.height / downsample;

//            tmpId1 = Shader.PropertyToID("tmpBlurRT1");
//            tmpId2 = Shader.PropertyToID("tmpBlurRT2");
//            cmd.GetTemporaryRT(tmpId1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
//            cmd.GetTemporaryRT(tmpId2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

//            tmpRT1 = new RenderTargetIdentifier(tmpId1);
//            tmpRT2 = new RenderTargetIdentifier(tmpId2);

//            ConfigureTarget(tmpRT1);
//            ConfigureTarget(tmpRT2);
//        }

//        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//        {
//            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

//            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
//            opaqueDesc.depthBufferBits = 0;

//            // first pass
//            // cmd.GetTemporaryRT(tmpId1, opaqueDesc, FilterMode.Bilinear);
//            cmd.SetGlobalFloat("_offset", 1.5f);
//            cmd.Blit(source, tmpRT1, blurMaterial);

//            for (var i=1; i<passes-1; i++) {
//                cmd.SetGlobalFloat("_offset", 0.5f + i);
//                cmd.Blit(tmpRT1, tmpRT2, blurMaterial);

//                // pingpong
//                var rttmp = tmpRT1;
//                tmpRT1 = tmpRT2;
//                tmpRT2 = rttmp;
//            }

//            // final pass
//            cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);
//            if (copyToFramebuffer) {
//                cmd.Blit(tmpRT1, source, blurMaterial);
//            } else {
//                cmd.Blit(tmpRT1, tmpRT2, blurMaterial);
//                cmd.SetGlobalTexture(targetName, tmpRT2);
//            }

//            context.ExecuteCommandBuffer(cmd);
//            cmd.Clear();

//            CommandBufferPool.Release(cmd);
//        }

//        public override void FrameCleanup(CommandBuffer cmd)
//        {
//        }
//    }

//    CustomRenderPass scriptablePass;

//    public override void Create()
//    {
//        scriptablePass = new CustomRenderPass("KawaseBlur");
//        scriptablePass.blurMaterial = settings.blurMaterial;
//        scriptablePass.passes = settings.blurPasses;
//        scriptablePass.downsample = settings.downsample;
//        scriptablePass.copyToFramebuffer = settings.copyToFramebuffer;
//        scriptablePass.targetName = settings.targetName;

//        scriptablePass.renderPassEvent = settings.renderPassEvent;
//    }

//    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//    {
//        var src = renderer.cameraColorTarget;
//        scriptablePass.Setup(src);
//        renderer.EnqueuePass(scriptablePass);
//    }
//}


using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KawaseBlur : ScriptableRendererFeature
{
    [System.Serializable]
    public class KawaseBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial = null;

        [Range(2, 15)] public int blurPasses = 1;
        [Range(1, 4)] public int downsample = 1;

        public bool copyToFramebuffer = false;
        public string targetName = "_blurTexture";
    }

    public KawaseBlurSettings settings = new KawaseBlurSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material blurMaterial;
        public int passes;
        public int downsample;
        public bool copyToFramebuffer;
        public string targetName;
        readonly string profilerTag;

        // RTHandles (URP 12+)
        RTHandle _source;
        RTHandle _tmpA;
        RTHandle _tmpB;

        static readonly int _Offset = Shader.PropertyToID("_offset");

        public CustomRenderPass(string profilerTag) => this.profilerTag = profilerTag;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Get camera color target handle INSIDE the pass
            _source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Build a descriptor for our temporary RTs
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            desc.bindMS = false;

            // Downsample the temp targets
            desc.width = Mathf.Max(1, desc.width / Mathf.Max(1, downsample));
            desc.height = Mathf.Max(1, desc.height / Mathf.Max(1, downsample));

            // Allocate (or re-allocate) RTHandles as needed
            RenderingUtils.ReAllocateIfNeeded(ref _tmpA, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KawaseTmpA");
            RenderingUtils.ReAllocateIfNeeded(ref _tmpB, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_KawaseTmpB");

            // Declare we read camera color
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blurMaterial == null) return;

            var cmd = CommandBufferPool.Get(profilerTag);

            // First pass (source -> tmpA)
            cmd.SetGlobalFloat(_Offset, 1.5f);
            Blitter.BlitCameraTexture(cmd, _source, _tmpA, blurMaterial, 0);

            // Middle passes (ping-pong between tmpA and tmpB)
            for (int i = 1; i < passes - 1; i++)
            {
                cmd.SetGlobalFloat(_Offset, 0.5f + i);
                Blitter.BlitCameraTexture(cmd, _tmpA, _tmpB, blurMaterial, 0);
                var t = _tmpA; _tmpA = _tmpB; _tmpB = t; // swap
            }

            // Final pass
            cmd.SetGlobalFloat(_Offset, 0.5f + passes - 1f);
            if (copyToFramebuffer)
            {
                // Write result back to camera
                Blitter.BlitCameraTexture(cmd, _tmpA, _source, blurMaterial, 0);
            }
            else
            {
                // Keep result for later passes/shaders
                Blitter.BlitCameraTexture(cmd, _tmpA, _tmpB, blurMaterial, 0);
                cmd.SetGlobalTexture(targetName, _tmpB); // RTHandle is accepted
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Release our temps
            _tmpA?.Release();
            _tmpB?.Release();
        }
    }

    CustomRenderPass _pass;

    public override void Create()
    {
        _pass = new CustomRenderPass("KawaseBlur")
        {
            blurMaterial = settings.blurMaterial,
            passes = settings.blurPasses,
            downsample = settings.downsample,
            copyToFramebuffer = settings.copyToFramebuffer,
            targetName = settings.targetName,
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Do NOT touch camera targets here
        renderer.EnqueuePass(_pass);
    }
}
