using UnityEngine;

public class CapitalShipCannon : MonoBehaviour
{
	[Header("Transform Refs")]
	public Transform CannonYaw;

	public Transform CannonPitch;

	public Transform[] Barrels;

	[Header("Shooting")]
	public int teamIndex;

	public Transform ManualAim;

	public bool autoFire = true;

	public float fireRate = 100f;

	public float range = 350f;

	private ParticleSpawnSystem partSpawner;

	private bool firing;

	private int barrelIndex;

	public Vector3 targetPosition = Vector3.zero;

	public Vector3 aimingPosition;

	private Boids.Boid targetBoid;

	private bool newBoid = true;

	private float cooldown;

	private Vector3 yawValue;

	private Vector3 manualYawValue;

	private Quaternion pitchValue;

	private Quaternion manualPitchValue;

	private float pitchSmooth;

	private float pitchVel;

	[Range(0f, 1f)]
	public float manualBlend = 1f;

	private void Start()
	{
		if (partSpawner == null)
		{
			ParticleSpawnSystem[] array = Object.FindObjectsByType<ParticleSpawnSystem>(FindObjectsSortMode.None);
			foreach (ParticleSpawnSystem particleSpawnSystem in array)
			{
				if (particleSpawnSystem.CompareTag(base.gameObject.tag))
				{
					partSpawner = particleSpawnSystem;
				}
			}
		}
		manualYawValue = CannonYaw.forward;
		manualPitchValue = CannonPitch.localRotation;
	}

	private void Update()
	{
		if (!CannonYaw || !CannonPitch)
		{
			return;
		}
		if ((bool)Boids.Instance && manualBlend < 1f)
		{
			if (newBoid || targetBoid.id < 0 || targetBoid.health <= 0f || Vector3.Distance(targetBoid.position, CannonYaw.position) > 550f)
			{
				targetBoid = Boids.Instance.GetClosestBoid(ManualAim.position, teamIndex);
				newBoid = false;
			}
			else
			{
				targetBoid = Boids.Instance.GetBoid(targetBoid.id);
			}
		}
		if (targetBoid.id == -1)
		{
			return;
		}
		targetPosition = targetBoid.position + targetBoid.velocity * targetBoid.speed;
		aimingPosition = Vector3.Lerp(targetPosition, ManualAim.position, manualBlend);
		Vector3 vector = base.transform.InverseTransformPoint(aimingPosition);
		vector.y = 0f;
		Vector3 vector2 = base.transform.TransformDirection(vector.normalized);
		Vector3 vector3 = vector2 * 10f;
		Debug.DrawLine(CannonYaw.position, CannonYaw.position + vector3, Color.blue);
		manualYawValue = Vector3.RotateTowards(manualYawValue, vector2, 1.2217305f * Time.deltaTime, Time.deltaTime * 0.1f);
		Vector3 vector4 = CannonYaw.InverseTransformPoint(aimingPosition);
		vector4.x = 0f;
		vector4.y -= CannonPitch.localPosition.y;
		float num = Vector3.Angle(Vector3.forward, vector4.normalized);
		num = ((vector4.y > 0f) ? (0f - num) : num);
		num = Mathf.Clamp(num, -60f, 5f);
		pitchSmooth = Mathf.SmoothDamp(pitchSmooth, num, ref pitchVel, 0.2f, 25f);
		manualPitchValue = Quaternion.AngleAxis(pitchSmooth, Vector3.right);
		CannonYaw.rotation = Quaternion.LookRotation(manualYawValue, base.transform.up);
		CannonPitch.localRotation = manualPitchValue;
		if (!autoFire)
		{
			return;
		}
		if (Vector3.Distance(CannonPitch.position, aimingPosition) < range)
		{
			firing = Vector3.Dot(CannonPitch.forward, (aimingPosition - CannonPitch.position).normalized) > 0.95f;
		}
		else
		{
			firing = false;
		}
		if (!firing)
		{
			return;
		}
		if (cooldown < 0f)
		{
			Transform transform = Barrels[barrelIndex];
			partSpawner.Spawn(transform.position, transform.forward, aimingPosition);
			if ((bool)Boids.Instance && manualBlend <= float.Epsilon)
			{
				Boids.Instance.DamageBoid(targetBoid.id, 10f);
			}
			barrelIndex = (int)Mathf.Repeat(barrelIndex + 1, Barrels.Length);
			cooldown += Random.Range(0.9f, 1.1f) / fireRate * 60f;
		}
		else
		{
			cooldown -= Time.deltaTime;
		}
	}

	private void OnDrawGizmos()
	{
		Color blue = Color.blue;
		blue.a = 0.1f;
		Gizmos.color = blue;
		Vector3 position = ManualAim.position;
		Gizmos.DrawSphere(position, 2f);
		Gizmos.DrawLine(position, CannonPitch.position);
		blue = Color.green;
		blue.a = 0.1f;
		Gizmos.color = blue;
		Gizmos.DrawSphere(targetPosition, 2f);
		Gizmos.DrawLine(targetPosition, CannonPitch.position);
		blue = Color.red;
		blue.a = 0.25f;
		Gizmos.color = blue;
		Gizmos.DrawRay(CannonPitch.position, CannonPitch.forward * 250f);
	}
}
