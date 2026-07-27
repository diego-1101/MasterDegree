using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class NpcSplineFollower : MonoBehaviour
{
	public Transform HandAnimatedTransform;

	[Range(0f, 1f)]
	public float HandAnimationWeight;

	public SplineContainer Spline;

	public float Distance;

	public float m_SplineLength;

	private Quaternion RotVel;

	private void Start()
	{
		m_SplineLength = Spline.CalculateLength();
	}

	private void Update()
	{
		float splineTime = GetSplineTime(Distance);
		Vector3 vector = Spline.EvaluatePosition(splineTime);
		Vector3 forward = Vector3.Normalize(Spline.EvaluateTangent(splineTime));
		Vector3 vector2 = Spline.EvaluateUpVector(GetSplineTime(Distance));
		Debug.DrawRay(base.transform.position, vector2 * 5f, Color.magenta);
		Quaternion quaternion = SplineFollower.SmoothDamp(base.transform.rotation, Quaternion.LookRotation(forward, vector2), ref RotVel, 0.3f);
		if (HandAnimatedTransform != null)
		{
			base.transform.position = Vector3.Lerp(vector, HandAnimatedTransform.position, HandAnimationWeight);
			base.transform.rotation = Quaternion.Lerp(quaternion, HandAnimatedTransform.rotation, HandAnimationWeight);
		}
		else
		{
			base.transform.position = vector;
			base.transform.rotation = quaternion;
		}
	}

	public float GetSplineTime(float distance)
	{
		return Mathf.Repeat(distance / m_SplineLength, 1f);
	}
}
