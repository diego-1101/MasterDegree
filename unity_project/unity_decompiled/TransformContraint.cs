using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class TransformContraint : MonoBehaviour
{
	public Transform pivot;

	[Header("Constraints")]
	public Transform constraintA;

	public Transform constraintB;

	[Range(0f, 1f)]
	public float blendContraints;

	[Header("Blend Controls")]
	[Range(0f, 1f)]
	public float blendTotal;

	[Range(0f, 1f)]
	public float blendPosition;

	[Range(0f, 1f)]
	public float blendRotation;

	[Range(0f, 1f)]
	public float blendScale;

	private Vector3 positionWS;

	private Quaternion rotation;

	private Vector3 scale = Vector3.one;

	public float dampening = 0.1f;

	private Vector3 posVel;

	private Quaternion rotVel;

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

	private void Update()
	{
		if (Application.isPlaying)
		{
			UpdateConstraint();
		}
	}

	private void BeginFrame(ScriptableRenderContext context, List<Camera> cameras)
	{
		if (!Application.isPlaying)
		{
			Transform transform = base.transform;
			positionWS = transform.position;
			rotation = transform.rotation;
			scale = transform.localScale;
			UpdateConstraint();
		}
	}

	private void EndFrame(ScriptableRenderContext context, List<Camera> cameras)
	{
		if (!Application.isPlaying)
		{
			pivot.SetPositionAndRotation(positionWS, rotation);
			pivot.localScale = scale;
		}
	}

	private void UpdateConstraint()
	{
		if ((bool)constraintA && (bool)constraintB)
		{
			Vector3 b = Vector3.Lerp(constraintA.position, constraintB.position, blendContraints);
			Quaternion b2 = Quaternion.Lerp(constraintA.rotation, constraintB.rotation, blendContraints);
			Vector3 b3 = Vector3.Lerp(constraintA.lossyScale, constraintB.lossyScale, blendContraints);
			Vector3 position = Vector3.Lerp(base.transform.position, b, blendTotal * blendPosition);
			Quaternion quaternion = Quaternion.Lerp(base.transform.rotation, b2, blendTotal * blendRotation);
			pivot.SetPositionAndRotation(position, quaternion);
			pivot.localScale = Vector3.Lerp(base.transform.localScale, b3, blendTotal * blendScale);
		}
	}
}
