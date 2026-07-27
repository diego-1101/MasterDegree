using System;
using System.Collections;
using UnityEngine;

public class BallController : MonoBehaviour
{
	private Rigidbody rb;

	private Vector3 targetPos;

	public bool hasReached;

	public bool hasCollided;

	private Coroutine ballCoroutine;

	public int targetCont;

	public Transform origin;

	public Transform currentTarget;

	public float currentHeight;

	public float currentHorizontalPeak;

	public float ballSpeed;

	private Vector3 initialVelocity;

	private bool velocityApplied;

	private RealtimeLog log;

	public void SetTarget(Vector3 _target)
	{
		targetPos = _target;
	}

	public void SetCoroutine(Coroutine ball)
	{
		ballCoroutine = ball;
	}

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		log = RealtimeLog.instance;
	}

	private void Update()
	{
		log._ballPosition = base.transform.position;
		if (!hasReached && !rb.isKinematic && Vector3.Distance(base.transform.position, currentTarget.position) < 0.5f)
		{
			hasReached = true;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			StartCoroutine(DestroyDelay(2f));
		}
	}

	private void FixedUpdate()
	{
	}

	public void DisablePhysics()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		rb.isKinematic = true;
		rb.useGravity = false;
	}

	public void EnablePhysics()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		rb.isKinematic = false;
		rb.useGravity = true;
	}

	public void ApplyFinalVelocity()
	{
		if (rb == null)
		{
			rb = GetComponent<Rigidbody>();
		}
		Vector3 normalized = (currentTarget.position - base.transform.position).normalized;
		rb.linearVelocity = normalized * ballSpeed;
	}

	public Vector3 CalculateBallPosition(float t)
	{
		Vector3 vector = Vector3.Lerp(origin.position, currentTarget.position, t);
		Vector3 normalized2 = Vector3.Cross(rhs: (currentTarget.position - origin.position).normalized, lhs: Vector3.up).normalized;
		float num = 4f * currentHeight * t * (1f - t);
		float num2 = Mathf.Sin(t * MathF.PI) * currentHorizontalPeak;
		return vector + normalized2 * num2 + Vector3.up * num;
	}

	private IEnumerator DestroyDelay(float _delay)
	{
		yield return new WaitForSeconds(_delay);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if ((collision.gameObject.tag == "Ground" || collision.gameObject.tag == "Player") && !hasCollided)
		{
			hasCollided = true;
			if (ballCoroutine != null)
			{
				StopCoroutine(ballCoroutine);
			}
			StartCoroutine(DestroyDelay(5f));
			string collisionTime = DateTime.Now.ToString("HH:mm:ss");
			RealtimeLog.instance._collisionTime = collisionTime;
			if (collision.gameObject.tag == "Player")
			{
				RealtimeLog.instance._acerto = "ACERTOU";
				FeedbackController.instance.PlayParabens();
			}
			else
			{
				RealtimeLog.instance._acerto = "ERROU";
				FeedbackController.instance.PlayErrou();
			}
			EnablePhysics();
		}
	}
}
