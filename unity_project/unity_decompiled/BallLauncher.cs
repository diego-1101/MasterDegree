using System;
using System.Collections;
using UnityEngine;

public class BallLauncher : MonoBehaviour
{
	public enum LaunchMode
	{
		Manual,
		Destroyed,
		Interval
	}

	private static BallLauncher _instance;

	private Transform origin;

	[SerializeField]
	private Transform[] targets;

	private Transform currentTarget;

	private Vector2[] noises;

	private int noiseIndex;

	[Header("Configurações")]
	public GameObject ballPrefab;

	public int resolution = 30;

	public bool isRendEnable = true;

	[Space]
	[Header("Modo")]
	public LaunchMode launchMode;

	private float interval = 0.5f;

	private float ballSpeed = 1f;

	private int trials;

	private LineRenderer trajectoryLine;

	private float currentHorizontalPeak;

	private float currentHeight;

	private Coroutine launchCoroutine;

	private Coroutine ballCoroutine;

	[HideInInspector]
	public bool isLaunching;

	public static BallLauncher instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindAnyObjectByType<BallLauncher>();
			}
			return _instance;
		}
	}

	private void Start()
	{
		origin = base.transform;
		trajectoryLine = GetComponent<LineRenderer>();
	}

	private void Update()
	{
		if (StartGame.instance.GetStats())
		{
			UpdateTrajectoryVisualization();
			if (isRendEnable != trajectoryLine.enabled)
			{
				trajectoryLine.enabled = isRendEnable;
			}
		}
	}

	public void SetParameters(int _trials, float _moveSpeed, Vector2[] _noise, float _interval = 0f)
	{
		trials = _trials;
		ballSpeed = _moveSpeed;
		noises = _noise;
		interval = _interval;
	}

	public GameObject LaunchSingleBall()
	{
		int num = UnityEngine.Random.Range(0, targets.Length - 1);
		GrazController grazController = GrazController.instance;
		switch (num)
		{
		case 0:
			grazController.SetCueImageDirection(180f);
			RealtimeLog.instance._target = "ESQUERDA";
			break;
		case 1:
			grazController.SetCueImageDirection(-90f);
			RealtimeLog.instance._target = "CENTRO";
			break;
		case 2:
			grazController.SetCueImageDirection(0f);
			RealtimeLog.instance._target = "DIREITA";
			break;
		case 3:
			grazController.SetCueImageDirection(90f);
			RealtimeLog.instance._target = "FORA";
			break;
		}
		currentTarget = targets[num];
		GeneratePeak();
		GameObject gameObject = UnityEngine.Object.Instantiate(ballPrefab, origin.position, Quaternion.identity);
		ballCoroutine = StartCoroutine(MoveBallCoroutine(gameObject));
		return gameObject;
	}

	public IEnumerator LaunchSequenceBall()
	{
		while (StartGame.instance.actTrials < trials && isLaunching && StartGame.instance.Started)
		{
			GameObject ball = LaunchSingleBall();
			switch (launchMode)
			{
			case LaunchMode.Destroyed:
				yield return new WaitUntil(() => ball == null);
				break;
			case LaunchMode.Interval:
				yield return new WaitForSeconds(interval);
				break;
			}
			StartGame.instance.actTrials++;
		}
		isLaunching = false;
		StartGame.instance.RestartGame();
	}

	public void StartSequence()
	{
		if (!isLaunching)
		{
			isLaunching = true;
			StartGame.instance.actTrials = 0;
			if (launchMode != 0)
			{
				launchCoroutine = StartCoroutine(LaunchSequenceBall());
			}
		}
	}

	public void StopSequence()
	{
		if (isLaunching)
		{
			isLaunching = false;
			if (launchCoroutine != null)
			{
				StopCoroutine(launchCoroutine);
				StopCoroutine(ballCoroutine);
			}
		}
	}

	private void GeneratePeak()
	{
		if (noises != null && noises.Length != 0)
		{
			currentHorizontalPeak = noises[noiseIndex].x;
			currentHeight = noises[noiseIndex].y;
			noiseIndex = (noiseIndex + 1) % noises.Length;
		}
	}

	private void UpdateTrajectoryVisualization()
	{
		if (!(origin == null) && !(currentTarget == null))
		{
			Vector3[] array = new Vector3[resolution];
			for (int i = 0; i < resolution; i++)
			{
				float t = (float)i / (float)(resolution - 1);
				array[i] = CalculateBallPosition(t);
			}
			trajectoryLine.positionCount = resolution;
			trajectoryLine.SetPositions(array);
		}
	}

	public Vector3 CalculateBallPosition(float t)
	{
		Vector3 vector = Vector3.Lerp(origin.position, currentTarget.position, t);
		Vector3 normalized2 = Vector3.Cross(rhs: (currentTarget.position - origin.position).normalized, lhs: Vector3.up).normalized;
		float num = 4f * currentHeight * t * (1f - t);
		float num2 = Mathf.Sin(t * MathF.PI) * currentHorizontalPeak;
		return vector + normalized2 * num2 + Vector3.up * num;
	}

	private IEnumerator MoveBallCoroutine(GameObject ball)
	{
		float num = Vector3.Distance(origin.position, currentTarget.position);
		if (GrazController.instance.inGrazProtocol)
		{
			ballSpeed = num / (GrazController.instance.cueDuration + 1f);
		}
		float duration = num / ballSpeed;
		float elapsed = 0f;
		BallController controller = ball.AddComponent<BallController>();
		controller.SetCoroutine(ballCoroutine);
		controller.SetTarget(currentTarget.position);
		controller.origin = origin;
		controller.currentTarget = currentTarget;
		controller.currentHeight = currentHeight;
		controller.currentHorizontalPeak = currentHorizontalPeak;
		controller.ballSpeed = ballSpeed;
		controller.DisablePhysics();
		while (elapsed < duration && ball != null && !controller.hasCollided)
		{
			float num2 = elapsed / duration;
			ball.transform.position = controller.CalculateBallPosition(num2);
			if (num2 > 0.1f && num2 < 0.9f)
			{
				Vector3 forward = controller.CalculateBallPosition(num2 + 0.05f) - ball.transform.position;
				ball.transform.rotation = Quaternion.LookRotation(forward);
			}
			elapsed += Time.deltaTime;
			yield return null;
		}
		controller.EnablePhysics();
		controller.ApplyFinalVelocity();
		if (!isLaunching)
		{
			trajectoryLine.enabled = false;
		}
	}
}
