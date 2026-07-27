using System.Collections;
using UnityEngine;

public class ShootBall : MonoBehaviour
{
	public LayerMask layerMask;

	public FollowingBall ballPrefab;

	public Vector3 ballSpawnOffset = new Vector3(0f, 1f, 0f);

	public float shootDelay;

	private Transform[] targets;

	private float moveSpeed = 2f;

	private Vector2[] noise;

	private int maxTrials = 25;

	private int actTrials;

	private bool isLinear = true;

	public void StartShoot()
	{
		StartCoroutine(Shoot());
	}

	public void SetParameters(Transform[] _targets, int _trials, float _moveSpeed, Vector2[] _noise)
	{
		targets = _targets;
		moveSpeed = _moveSpeed / 10f;
		maxTrials = _trials;
		noise = _noise;
	}

	private IEnumerator Shoot()
	{
		actTrials = 0;
		WaitForSeconds wait = new WaitForSeconds(shootDelay);
		while (actTrials < maxTrials)
		{
			FollowingBall followingBall = Object.Instantiate(ballPrefab);
			followingBall.transform.position = base.transform.position + ballSpawnOffset;
			followingBall.transform.rotation = base.transform.parent.rotation;
			followingBall.Spawn(targets[Random.Range(0, targets.Length)], moveSpeed, noise[actTrials].x, noise[actTrials].y, isLinear);
			actTrials++;
			yield return wait;
		}
		yield return new WaitForSeconds(0.5f);
		Object.FindAnyObjectByType<StartGame>().RestartGame();
	}

	public int GetTrial()
	{
		return actTrials;
	}
}
