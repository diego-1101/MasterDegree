using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class SimpleUpvector : MonoBehaviour
{
	private Transform parent;

	public Transform upVectorGoal;

	private Quaternion cachedRotation;

	private void OnEnable()
	{
		parent = base.transform.parent;
		if (!(parent == null) && !(upVectorGoal == null))
		{
			RenderPipelineManager.beginContextRendering += BeginFrame;
			RenderPipelineManager.endContextRendering += EndFrame;
		}
	}

	private void OnDisable()
	{
		RenderPipelineManager.beginContextRendering -= BeginFrame;
		RenderPipelineManager.endContextRendering -= EndFrame;
	}

	private void BeginFrame(ScriptableRenderContext context, List<Camera> cams)
	{
		cachedRotation = base.transform.rotation;
		base.transform.rotation = Quaternion.LookRotation(parent.forward, upVectorGoal.position - base.transform.position);
	}

	private void EndFrame(ScriptableRenderContext context, List<Camera> cams)
	{
		if (!Application.isPlaying)
		{
			base.transform.rotation = cachedRotation;
		}
	}
}
