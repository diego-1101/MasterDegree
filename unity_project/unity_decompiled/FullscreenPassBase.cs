using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class FullscreenPassBase<T> : ScriptableRenderPass where T : FullscreenPassDataBase, new()
{
	public Material material;

	public bool hasYFlipKeyword;

	public LocalKeyword yFlipKeyword;

	public new string passName = "Fullscreen Pass";

	public ScriptableRenderPassInput inputRequirements;

	public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
	{
		if (hasYFlipKeyword)
		{
			material.SetKeyword(in yFlipKeyword, renderingData.cameraData.IsRenderTargetProjectionMatrixFlipped(renderingData.cameraData.renderer.cameraColorTargetHandle));
		}
		CommandBuffer commandBuffer = CommandBufferPool.Get(passName);
		CoreUtils.DrawFullScreen(commandBuffer, material);
		context.ExecuteCommandBuffer(commandBuffer);
		commandBuffer.Clear();
	}

	public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
	{
		UniversalResourceData universalResourceData = frameData.Get<UniversalResourceData>();
		UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
		T passData;
		using IRasterRenderGraphBuilder rasterRenderGraphBuilder = renderGraph.AddRasterRenderPass<T>(passName, out passData, "C:\\Github\\UNIFESP-Project\\Assets\\SharedAssets\\Scripts\\Runtime\\FullscreenEffect.cs", 121);
		passData.cameraData = cameraData;
		passData.hasYFlipKeyword = hasYFlipKeyword;
		if (inputRequirements.HasFlag(ScriptableRenderPassInput.Color))
		{
			passData.textureHandle = universalResourceData.activeColorTexture;
			rasterRenderGraphBuilder.SetRenderAttachment(universalResourceData.activeColorTexture, 0);
		}
		if (inputRequirements.HasFlag(ScriptableRenderPassInput.Depth))
		{
			rasterRenderGraphBuilder.SetRenderAttachmentDepth(universalResourceData.activeDepthTexture, AccessFlags.Read);
		}
		if (inputRequirements.HasFlag(ScriptableRenderPassInput.Normal))
		{
			TextureHandle cameraNormalsTexture = universalResourceData.cameraNormalsTexture;
			rasterRenderGraphBuilder.UseTexture(in cameraNormalsTexture);
		}
		if (inputRequirements.HasFlag(ScriptableRenderPassInput.Motion))
		{
			TextureHandle cameraNormalsTexture = universalResourceData.motionVectorColor;
			rasterRenderGraphBuilder.UseTexture(in cameraNormalsTexture);
		}
		rasterRenderGraphBuilder.SetRenderFunc(delegate(T data, RasterGraphContext context)
		{
			ExecuteRenderGraph(data, context);
		});
	}

	public virtual void ExecuteRenderGraph(T passData, RasterGraphContext rgContext)
	{
		RasterCommandBuffer cmd = rgContext.cmd;
		if (hasYFlipKeyword && passData.textureHandle.IsValid())
		{
			material.SetKeyword(in yFlipKeyword, passData.cameraData.IsRenderTargetProjectionMatrixFlipped(passData.textureHandle));
		}
		cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, null);
	}
}
