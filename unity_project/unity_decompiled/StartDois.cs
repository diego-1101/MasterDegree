using UnityEngine;

public class StartDois : StartGame
{
	public override void Start()
	{
		base.Start();
		_log.experienciaChoice = RealtimeLog.ExperienciaChoice.Dois;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space) && !isInitialize && (bool)UIConfigs.instance && Started)
		{
			UIConfigs.instance.StartLogRealtime();
			Medidor medidor = statusUI.medidor;
			medidor.gameObject.SetActive(value: true);
			medidor.medidor.UpdatePosition(medidor.medidor._markerAngle, medidor.medidor.positionMinYMM, medidor.medidor.positionMaxYMM, GameUIDois.Instance._anguloPerna[actTrials] / 180f);
			statusUI.configs.SetActive(value: false);
			actTrials = 0;
			isInitialize = true;
			UIConfigs.instance.spaceBar.SetActive(value: false);
			GrazController.instance.StartProtocol();
		}
	}

	public override void RestartGame()
	{
		base.RestartGame();
		statusUI.medidor.gameObject.SetActive(value: false);
	}

	public void HeightMargem(float _height, float _tempoPrecisao)
	{
		statusUI.medidor.SetParameters(this, _height, _tempoPrecisao);
	}
}
