using UnityEngine;

public class Medidor : MonoBehaviour
{
	[HideInInspector]
	public PlayerController _player;

	[HideInInspector]
	public float inputValue;

	[HideInInspector]
	public float[] inputAngles;

	public MedidorMovement medidor;

	[HideInInspector]
	public StartGame startGame;

	private void Update()
	{
		if (!_player)
		{
			_player = Object.FindAnyObjectByType<PlayerController>();
		}
		if ((bool)_player)
		{
			float[] inputValues = _player.GetInputValues();
			LegType legType = UIConfigs.instance._legType;
			inputValue = ((legType == LegType.LEFT) ? inputValues[0] : inputValues[1]);
			if (inputAngles.Length < 1)
			{
				inputAngles = _player.GetInputAngles();
			}
		}
	}

	public void SetParameters(StartDois start, float height, float tempoPrecisao)
	{
		startGame = start;
		medidor.SetMargemAcerto(height);
		medidor._tempoPrecisao = tempoPrecisao;
	}
}
