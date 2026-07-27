using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class SplineFollower : MonoBehaviour
{
	public SplineContainer splineContainer;

	public Transform handAnimatedTransform;

	[Range(0f, 1f)]
	public float animationLerp;

	[Range(0f, 1f)]
	public float BankContribution;

	public float Distance;

	public float Prediction;

	public float SmoothTime;

	public float BankSampleDistance;

	public float splineLength;

	private Quaternion rotVel;

	private float rollVel;

	private float currentRotationSpeed;

	private void Start()
	{
		splineLength = splineContainer.CalculateLength();
	}

	private void Update()
	{
		Vector3 vector = splineContainer.EvaluatePosition(GetSplineTime(Distance + Prediction));
		Vector3 vector2 = splineContainer.EvaluatePosition(GetSplineTime(Distance + Prediction - BankSampleDistance));
		Vector3 vector3 = splineContainer.EvaluatePosition(GetSplineTime(Distance + Prediction + BankSampleDistance));
		Vector3 normalized = (vector2 - vector).normalized;
		Vector3 normalized2 = (vector3 - vector).normalized;
		Debug.DrawRay(vector, normalized * BankSampleDistance, Color.cyan);
		Debug.DrawRay(vector, normalized2 * BankSampleDistance, Color.magenta);
		Mathf.Clamp01((Vector3.Dot(normalized, normalized2) + 1f) * 4f);
		float splineTime = GetSplineTime(Distance);
		Vector3 vector4 = splineContainer.EvaluatePosition(splineTime);
		Vector3 normalized3 = (vector - vector4).normalized;
		Vector3 normalized4 = Vector3.Cross(normalized, normalized2).normalized;
		normalized4 = Vector3.Cross(normalized3, normalized4);
		Debug.DrawRay(vector, normalized4 * 5f, Color.blue);
		Quaternion target = Quaternion.Lerp(Quaternion.LookRotation(normalized3, Vector3.up), Quaternion.LookRotation(normalized3, normalized4), BankContribution);
		target = SmoothDamp(base.transform.rotation, target, ref rotVel, SmoothTime);
		Vector3 a = vector4;
		base.transform.rotation = Quaternion.Lerp(target, handAnimatedTransform.rotation, animationLerp);
		base.transform.position = Vector3.Lerp(a, handAnimatedTransform.position, animationLerp);
	}

	public static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
	{
		if (Time.deltaTime < Mathf.Epsilon)
		{
			return rot;
		}
		float num = ((Quaternion.Dot(rot, target) > 0f) ? 1f : (-1f));
		target.x *= num;
		target.y *= num;
		target.z *= num;
		target.w *= num;
		Vector4 normalized = new Vector4(Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time), Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time), Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time), Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)).normalized;
		Vector4 vector = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), normalized);
		deriv.x -= vector.x;
		deriv.y -= vector.y;
		deriv.z -= vector.z;
		deriv.w -= vector.w;
		return new Quaternion(normalized.x, normalized.y, normalized.z, normalized.w);
	}

	private float GetSplineTime(float distance)
	{
		return Mathf.Repeat(distance / splineLength, 1f);
	}
}
