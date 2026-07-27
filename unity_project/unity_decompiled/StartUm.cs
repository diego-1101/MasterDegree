using UnityEngine;

public class StartUm : StartGame
{
	[Space]
	[Header("Components")]
	public BallLauncher shootBall;

	public override void Start()
	{
		base.Start();
		_log.experienciaChoice = RealtimeLog.ExperienciaChoice.Um;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space) && !isInitialize && (bool)UIConfigs.instance && Started)
		{
			UIConfigs.instance.StartLogRealtime();
			statusUI.configs.SetActive(value: false);
			actTrials = 0;
			isInitialize = true;
			UIConfigs.instance.spaceBar.SetActive(value: false);
			GrazController.instance.StartProtocol();
		}
	}

	public override void StopTasks()
	{
		base.StopTasks();
		shootBall.StopSequence();
	}
}
