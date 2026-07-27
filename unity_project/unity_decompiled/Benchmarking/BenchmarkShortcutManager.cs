using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.SceneManagement;

namespace Benchmarking;

public class BenchmarkShortcutManager : MonoBehaviour
{
	private static BenchmarkShortcutManager s_instance;

	private bool availableToStart = true;

	public static BenchmarkShortcutManager instance
	{
		get
		{
			if (s_instance == null)
			{
				GetOrCreate();
			}
			return s_instance;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void GetOrCreate()
	{
		s_instance = Object.FindFirstObjectByType<BenchmarkShortcutManager>();
		if (s_instance == null)
		{
			s_instance = new GameObject("Benchmark Shortcut Manager").AddComponent<BenchmarkShortcutManager>();
		}
		Object.DontDestroyOnLoad(s_instance.gameObject);
	}

	private void Awake()
	{
		if (s_instance != null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		s_instance = this;
		Object.DontDestroyOnLoad(s_instance.gameObject);
		InputAction inputAction = new InputAction();
		inputAction.AddCompositeBinding("ButtonWithOneModifier").With("Button", "<Keyboard>/B").With("Modifier", "<Keyboard>/leftShift")
			.With("Modifier", "<Keyboard>/rightShift");
		inputAction.performed += delegate
		{
			StartBenchmark();
		};
		inputAction.Enable();
		EnhancedTouchSupport.Enable();
	}

	public void Update()
	{
		if (SystemInfo.deviceType == DeviceType.Handheld && UnityEngine.InputSystem.EnhancedTouch.Touch.activeFingers.Count > 3)
		{
			StartBenchmark();
		}
	}

	private void StartBenchmark()
	{
		if (!PerformanceTest.RunningBenchmark && availableToStart)
		{
			StartCoroutine(WaitingTime());
			SceneManager.LoadScene("BenchmarkScene");
		}
	}

	private IEnumerator WaitingTime()
	{
		availableToStart = false;
		yield return new WaitForSeconds(5f);
		availableToStart = true;
	}
}
