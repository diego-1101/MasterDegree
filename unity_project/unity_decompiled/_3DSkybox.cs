using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class _3DSkybox : MonoBehaviour
{
	private class _3DSkyboxPass : ScriptableRenderPass
	{
		private class _3DSkyboxPassData
		{
			public CullingResults cullingResults;

			public RendererListHandle opaqueRendererListHandle;

			public RendererListHandle transparentRendererListHandle;

			public UniversalCameraData cameraData;
		}

		public FilteringSettings filterOpaqueSettings;

		public FilteringSettings filterTransparentSettings;

		public RenderStateBlock renderStateBlock;

		public Material depthClearMat;

		private readonly List<ShaderTagId> shaderTags = new List<ShaderTagId>
		{
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("UniversalForward"),
			new ShaderTagId("UniversalForwardOnly")
		};

		private static ShaderTagId[] s_ShaderTagValues = new ShaderTagId[1];

		private static RenderStateBlock[] s_RenderStateBlocks = new RenderStateBlock[1];

		public _3DSkyboxPass()
		{
			base.profilingSampler = new ProfilingSampler("_3DSkyboxPass");
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer cmd = CommandBufferPool.Get("3D Skybox");
			DrawSkyboxObjects(ref renderingData, context, ref cmd);
			if (depthClearMat != null)
			{
				CoreUtils.DrawFullScreen(cmd, depthClearMat);
			}
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
		}

		private void DrawSkyboxObjects(ref RenderingData renderingData, ScriptableRenderContext context, ref CommandBuffer cmd)
		{
			DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, ref renderingData, SortingCriteria.CommonOpaque);
			s_RenderStateBlocks[0] = renderStateBlock;
			s_ShaderTagValues[0] = ShaderTagId.none;
			RendererListParams param = new RendererListParams(renderingData.cullResults, drawSettings, filterOpaqueSettings);
			param.stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
			param.tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
			param.isPassTagName = false;
			RendererList rendererList = context.CreateRendererList(ref param);
			cmd.DrawRendererList(rendererList);
			param = new RendererListParams(renderingData.cullResults, drawSettings, filterTransparentSettings);
			param.stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
			param.tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
			param.isPassTagName = false;
			rendererList = context.CreateRendererList(ref param);
			cmd.DrawRendererList(rendererList);
		}

		private RendererListHandle InitRendererLists(ContextContainer frameData, RenderGraph renderGraph, FilteringSettings filterSettings, RenderStateBlock renderStateBlock)
		{
			UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
			UniversalCameraData universalCameraData = frameData.Get<UniversalCameraData>();
			UniversalLightData lightData = frameData.Get<UniversalLightData>();
			SortingCriteria defaultOpaqueSortFlags = universalCameraData.defaultOpaqueSortFlags;
			DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, universalRenderingData, universalCameraData, lightData, defaultOpaqueSortFlags);
			RendererListParams desc = new RendererListParams(universalRenderingData.cullResults, drawSettings, filterSettings);
			s_RenderStateBlocks[0] = renderStateBlock;
			s_ShaderTagValues[0] = ShaderTagId.none;
			desc.stateBlocks = new NativeArray<RenderStateBlock>(s_RenderStateBlocks, Allocator.Temp);
			desc.tagValues = new NativeArray<ShaderTagId>(s_ShaderTagValues, Allocator.Temp);
			desc.isPassTagName = false;
			return renderGraph.CreateRendererList(in desc);
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			UniversalCameraData universalCameraData = frameData.Get<UniversalCameraData>();
			_ = universalCameraData.camera;
			frameData.Get<UniversalRenderingData>();
			UniversalResourceData universalResourceData = frameData.Get<UniversalResourceData>();
			_3DSkyboxPassData passData;
			using IRasterRenderGraphBuilder rasterRenderGraphBuilder = renderGraph.AddRasterRenderPass<_3DSkyboxPassData>("3D Skybox", out passData, "C:\\Github\\UNIFESP-Project\\Assets\\Scenes\\Sample\\Cockpit\\Scripts\\Skybox3D.cs", 194);
			passData.opaqueRendererListHandle = InitRendererLists(frameData, renderGraph, filterOpaqueSettings, renderStateBlock);
			passData.transparentRendererListHandle = InitRendererLists(frameData, renderGraph, filterTransparentSettings, renderStateBlock);
			passData.cameraData = universalCameraData;
			rasterRenderGraphBuilder.UseRendererList(in passData.opaqueRendererListHandle);
			rasterRenderGraphBuilder.UseRendererList(in passData.transparentRendererListHandle);
			rasterRenderGraphBuilder.SetRenderAttachment(universalResourceData.activeColorTexture, 0);
			rasterRenderGraphBuilder.SetRenderAttachmentDepth(universalResourceData.activeDepthTexture);
			if (universalCameraData.xr.enabled)
			{
				rasterRenderGraphBuilder.EnableFoveatedRasterization(universalCameraData.xr.supportsFoveatedRendering);
			}
			rasterRenderGraphBuilder.SetRenderFunc(delegate(_3DSkyboxPassData data, RasterGraphContext context)
			{
				ExecuteRenderGraph(data, context);
			});
		}

		private void ExecuteRenderGraph(_3DSkyboxPassData passData, RasterGraphContext rgContext)
		{
			RasterCommandBuffer cmd = rgContext.cmd;
			UniversalCameraData cameraData = passData.cameraData;
			if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
			{
				Matrix4x4 projectionMatrix = cameraData.GetProjectionMatrix();
				Matrix4x4 stereoViewMatrix = cameraData.camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
				Matrix4x4 projectionMatrix2 = cameraData.GetProjectionMatrix(1);
				Matrix4x4 stereoViewMatrix2 = cameraData.camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
				Vector4 column = stereoViewMatrix.GetColumn(3);
				Vector4 column2 = stereoViewMatrix2.GetColumn(3);
				Vector4 column3 = (column + column2) * 0.5f;
				Matrix4x4 viewMatrix = stereoViewMatrix;
				viewMatrix.SetColumn(3, column3);
				XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(viewMatrix, projectionMatrix, renderIntoTexture: false, 0);
				XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(viewMatrix, projectionMatrix2, renderIntoTexture: false, 1);
				XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);
				cmd.DrawRendererList(passData.opaqueRendererListHandle);
				cmd.DrawRendererList(passData.transparentRendererListHandle);
				XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(stereoViewMatrix, projectionMatrix, renderIntoTexture: false, 0);
				XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(stereoViewMatrix2, projectionMatrix2, renderIntoTexture: false, 1);
				XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);
			}
			else
			{
				cmd.DrawRendererList(passData.opaqueRendererListHandle);
				cmd.DrawRendererList(passData.transparentRendererListHandle);
			}
			if (depthClearMat != null)
			{
				cmd.DrawProcedural(Matrix4x4.identity, depthClearMat, 0, MeshTopology.Triangles, 3, 1, null);
			}
		}
	}

	private _3DSkyboxPass pass;

	public LayerMask mask;

	private FilteringSettings filterOpaqueSettings = FilteringSettings.defaultValue;

	private FilteringSettings filterTransparentSettings = FilteringSettings.defaultValue;

	private RenderStateBlock renderStateBlock;

	public int stencilRef;

	private StencilState stencilState;

	public Material depthClearMat;

	public float scale = 64f;

	private Camera cam;

	private void OnEnable()
	{
		if (pass == null)
		{
			pass = new _3DSkyboxPass();
		}
		pass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
		filterOpaqueSettings = new FilteringSettings(RenderQueueRange.opaque, mask.value);
		filterTransparentSettings = new FilteringSettings(RenderQueueRange.transparent, mask.value);
		renderStateBlock = new RenderStateBlock(RenderStateMask.Stencil);
		stencilState = StencilState.defaultValue;
		stencilState.enabled = true;
		stencilState.SetCompareFunction(CompareFunction.Equal);
		renderStateBlock.stencilReference = stencilRef;
		renderStateBlock.stencilState = stencilState;
		RenderPipelineManager.beginCameraRendering += OnBeginCamera;
		RenderPipelineManager.endCameraRendering += EndCamera;
	}

	private void OnDisable()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
		RenderPipelineManager.endCameraRendering -= EndCamera;
	}

	private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
	{
		if ((!SceneTransitionManager.IsAvailable() || !SceneTransitionManager.IsInTerminal() || !cam.CompareTag("MainCamera")) && pass != null && (cam.cameraType == CameraType.Game || cam.cameraType == CameraType.SceneView))
		{
			pass.filterOpaqueSettings = filterOpaqueSettings;
			pass.filterTransparentSettings = filterTransparentSettings;
			pass.renderStateBlock = renderStateBlock;
			pass.depthClearMat = depthClearMat;
			TransformSkybox(cam);
			cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(pass);
		}
	}

	private void EndCamera(ScriptableRenderContext arg1, Camera arg2)
	{
		base.transform.position = Vector3.zero;
		base.transform.localScale = Vector3.one;
	}

	private void TransformSkybox(Camera cam)
	{
		if (cam.cameraType != CameraType.SceneView)
		{
			Vector3 position = cam.transform.position * (1f - 1f / scale);
			base.transform.position = position;
			base.transform.localScale = Vector3.one * (1f / scale);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (cam == null)
		{
			cam = Camera.main;
		}
		if (!(cam == null))
		{
			Gizmos.matrix = cam.transform.localToWorldMatrix;
			Color red = Color.red;
			red.a = 0.2f;
			Gizmos.color = red;
			Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane * (1f / scale), cam.nearClipPlane * (1f / scale), cam.aspect);
		}
	}
}
