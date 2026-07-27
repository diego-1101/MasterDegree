using UnityEngine;

public class StartGame : MonoBehaviour
{
	private static StartGame _instance;

	public bool SetupDefault;

	[HideInInspector]
	public StatusUIController statusUI;

	[HideInInspector]
	public bool isInitialize;

	[HideInInspector]
	public bool Started;

	[HideInInspector]
	public int trials = 25;

	public int actTrials;

	[HideInInspector]
	public RealtimeLog _log;

	public static StartGame instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<StartGame>();
			}
			return _instance;
		}
	}

	public virtual void Start()
	{
		_log = Object.FindAnyObjectByType<RealtimeLog>();
	}

	public virtual void RestartGame()
	{
		actTrials = 0;
		isInitialize = false;
		Started = false;
		GrazController.instance.ToggleCanvas(on: false);
		LSLReceiver.instance.StopInlet();
		Object.FindAnyObjectByType<PlayerController>().UpdateLegValues(0f, 0f, 0f);
		_log.EndLog();
	}

	public virtual void StopTasks()
	{
	}

	public bool GetStats()
	{
		return isInitialize;
	}
}
