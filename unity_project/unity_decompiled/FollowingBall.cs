using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FollowingBall : MonoBehaviour
{
	private Coroutine FollowingCoroutine;

	public AnimationCurve positionCurve;

	public AnimationCurve noiseCurve;

	public float yOffSet = 1f;

	public bool canBeKicked = true;

	private Transform target { get; set; }

	public float moveSpeed { get; set; }

	public float xNoise { get; set; }

	public float yNoise { get; set; }

	public bool isLinear { get; set; }

	private void OnDisable()
	{
		Object.Destroy(base.gameObject);
	}

	public void Spawn(Transform _target, float _moveSpeed, float _xNoise, float _yNoise, bool _isLinear)
	{
		target = _target;
		moveSpeed = _moveSpeed;
		xNoise = _xNoise;
		yNoise = _yNoise;
		isLinear = _isLinear;
		if (FollowingCoroutine != null)
		{
			StopCoroutine(FollowingCoroutine);
		}
		FollowingCoroutine = StartCoroutine(FindTarget());
	}

	private IEnumerator FindTarget()
	{
		Vector3 startPosition = base.transform.position;
		Vector2 noise = new Vector2(xNoise, yNoise);
		Vector3 lhs = new Vector3(target.position.x, target.position.y + yOffSet, target.position.z) - startPosition;
		Vector3 horizontalNoise = Vector3.Cross(lhs, Vector3.up).normalized;
		float time = 0f;
		while (time < 1f)
		{
			float num = noiseCurve.Evaluate(time);
			base.transform.position = Vector3.Lerp(startPosition, target.position + new Vector3(0f, yOffSet, 0f), time) + new Vector3(horizontalNoise.x * num * noise.x, num * noise.y, num * horizontalNoise.z * noise.x);
			base.transform.LookAt(target.position + new Vector3(0f, yOffSet, 0f));
			time += Time.deltaTime * moveSpeed;
			yield return null;
		}
		yield return new WaitForSeconds(0.3f);
		base.gameObject.SetActive(value: false);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			canBeKicked = true;
		}
	}
}
