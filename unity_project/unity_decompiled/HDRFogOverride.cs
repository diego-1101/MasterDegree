using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class HDRFogOverride : MonoBehaviour
{
	private Color _previousFogColor;

	[ColorUsage(false, true)]
	public Color FogColor = Color.white;

	private void OnEnable()
	{
		RenderPipelineManager.beginContextRendering += BeginFrame;
		RenderPipelineManager.endContextRendering += EndFrame;
	}

	private void OnDisable()
	{
		RenderPipelineManager.beginContextRendering -= BeginFrame;
		RenderPipelineManager.endContextRendering -= EndFrame;
	}

	private void BeginFrame(ScriptableRenderContext arg1, List<Camera> arg2)
	{
		_previousFogColor = RenderSettings.fogColor;
		RenderSettings.fogColor = FogColor;
	}

	private void EndFrame(ScriptableRenderContext arg1, List<Camera> arg2)
	{
		RenderSettings.fogColor = _previousFogColor;
	}
}
