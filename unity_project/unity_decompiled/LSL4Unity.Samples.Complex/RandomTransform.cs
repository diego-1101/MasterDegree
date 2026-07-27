using UnityEngine;

namespace LSL4Unity.Samples.Complex;

public class RandomTransform : MonoBehaviour
{
	public float ResetInterval = 2f;

	private float elapsed_time;

	public Rigidbody rb;

	private Vector3 start_position;

	private void Start()
	{
		rb = base.gameObject.AddComponent<Rigidbody>();
		rb.useGravity = false;
		Vector3 position = base.gameObject.transform.position;
		start_position = new Vector3(position.x, position.y, position.z);
	}

	private void Update()
	{
		elapsed_time += Time.deltaTime;
		if (elapsed_time >= ResetInterval)
		{
			base.gameObject.transform.position = start_position;
			rb.linearVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), Random.Range(-2f, 2f));
			rb.angularVelocity = new Vector3(Random.Range(-6f, 6f), Random.Range(-6f, 6f), Random.Range(-6f, 6f));
			elapsed_time = 0f;
		}
	}
}
