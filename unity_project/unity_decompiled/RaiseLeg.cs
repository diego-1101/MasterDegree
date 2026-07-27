using UnityEngine;

public class RaiseLeg : MonoBehaviour
{
	[Header("Environment Variables")]
	[Range(0f, 1f)]
	public float inputValue;

	public float minAngle = 65f;

	public float maxAngle;

	public float legForce = 300f;

	public float smoothVelocity;

	public float maxTorque = 50f;

	public float smoothTime = 0.1f;

	[Space]
	[Header("Física da Perna")]
	public Transform _bone;

	public Rigidbody hipRb;

	public float currentAngle;

	private void Update()
	{
		float num = 0f;
		if (!(UIConfigs.instance == null))
		{
			num = ((!GrazController.instance.OnFeedbackPhase && !UIConfigs.instance.GetPhotoActive()) ? 0f : inputValue);
			currentAngle = _bone.localEulerAngles.x;
			if (currentAngle > 180f)
			{
				currentAngle -= 360f;
			}
			if (num > 0.1f)
			{
				float num2 = (Mathf.Lerp(minAngle, maxAngle, num) - currentAngle) * legForce * Time.fixedDeltaTime;
				hipRb.AddTorque(_bone.right * num2, ForceMode.VelocityChange);
			}
		}
	}
}
