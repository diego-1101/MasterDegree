using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

[ExecuteAlways]
public class WormSplineFollower : MonoBehaviour
{
	public SplineContainer Spline;

	public bool flipDirection;

	public float Distance;

	public float PositionOffset;

	public float ScaleOffset;

	private float m_SplineLength;

	private void OnEnable()
	{
		m_SplineLength = Spline.CalculateLength();
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
		if (Spline == null)
		{
			return;
		}
		foreach (Transform item in base.transform)
		{
			float num = 1f - ScaleOffset * (float)item.GetSiblingIndex();
			item.localScale = Vector3.one * num;
			float value = (flipDirection ? (Distance + PositionOffset * (float)item.GetSiblingIndex()) : (Distance - PositionOffset * (float)item.GetSiblingIndex()));
			value = Mathf.Clamp(value, 0f, m_SplineLength - float.MinValue);
			var (position, rotation) = GotPositionAndRotation(value);
			item.SetPositionAndRotation(position, rotation);
		}
	}

	private void EndFrame(ScriptableRenderContext arg1, List<Camera> arg2)
	{
		if (Application.isPlaying)
		{
			return;
		}
		foreach (Transform item in base.transform)
		{
			item.SetPositionAndRotation(Vector3.zero, quaternion.identity);
			item.localScale = Vector3.one;
		}
	}

	private (Vector3, Quaternion) GotPositionAndRotation(float distance)
	{
		float splineTime = GetSplineTime(distance);
		Spline.Evaluate(splineTime, out var position, out var tangent, out var upVector);
		Quaternion item = Quaternion.LookRotation(flipDirection ? (-tangent) : tangent, upVector);
		return (position, item);
	}

	private float GetSplineTime(float distance)
	{
		return SplineUtility.GetNormalizedInterpolation(Spline.Spline, distance, PathIndexUnit.Distance);
	}
}
