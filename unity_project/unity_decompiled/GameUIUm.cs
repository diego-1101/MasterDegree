using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIUm : UIConfigs
{
	private static GameUIUm _instance;

	[HideInInspector]
	public StartUm startUm;

	[SerializeField]
	private float defaultVelocity = 3f;

	[SerializeField]
	private float defaultMaxValueX = 25f;

	[SerializeField]
	private float defaultMaxValueY = 25f;

	[SerializeField]
	private float defaultMinValueX = -25f;

	[SerializeField]
	private float defaultMinValueY = 5f;

	[Space]
	[Header("Bola configs")]
	[Space]
	[SerializeField]
	private TMP_InputField velocityField;

	[SerializeField]
	private TMP_InputField intervalField;

	[SerializeField]
	private float _moveSpeed;

	[Space]
	[Header("Posição da bola")]
	[Space]
	[SerializeField]
	private Toggle isRandomic;

	[SerializeField]
	private RectTransform inputsTransform;

	[SerializeField]
	private GameObject objInputRandom;

	[SerializeField]
	private GameObject objInputTrial;

	[SerializeField]
	private Button coordsButton;

	[HideInInspector]
	public Vector2[] _posicoesBola;

	public static GameUIUm Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<GameUIUm>();
			}
			return _instance;
		}
	}

	public override void Awake()
	{
		base.Awake();
		startUm = Object.FindAnyObjectByType<StartUm>();
	}

	public override void Start()
	{
		base.Start();
		SetTrials("1");
		isDefault.isOn = true;
		Physics.gravity = new Vector3(Physics.gravity.x, -9.81f, Physics.gravity.z);
		playerController.LegChoice("nenhuma");
	}

	public override void Update()
	{
		base.Update();
		AdjustContainerHeight(inputsTransform);
		AdjustContainerHeight(inputsTransform.parent.GetComponent<RectTransform>());
	}

	public override void SetDefault(bool isDefault)
	{
		base.SetDefault(isDefault);
		if (isDefault)
		{
			velocityField.text = defaultVelocity.ToString();
			isRandomic.isOn = true;
		}
		else
		{
			velocityField.text = "0";
			isRandomic.isOn = false;
		}
	}

	public override void SetTrials(string trials)
	{
		base.SetTrials(trials);
		if (!isRandomic.isOn)
		{
			SetRandomNoise(isRandom: false);
		}
	}

	public void SetMoveSpeed(string moveSpeed)
	{
		if (!string.IsNullOrEmpty(moveSpeed))
		{
			_moveSpeed = float.Parse(moveSpeed);
		}
	}

	public void SetRandomNoise(bool isRandom)
	{
		GameObject gameObject = (isRandom ? objInputRandom : objInputTrial);
		if (isRandom)
		{
			inputRandom = gameObject.GetComponent<InputRandomCS>();
		}
		if ((bool)inputRandom && isDefault.isOn)
		{
			inputRandom._maxX.text = defaultMaxValueX.ToString();
			inputRandom._maxY.text = defaultMaxValueY.ToString();
			inputRandom._minX.text = defaultMinValueX.ToString();
			inputRandom._minY.text = defaultMinValueY.ToString();
		}
		else if ((bool)inputRandom && !isDefault.isOn)
		{
			inputRandom._maxX.text = "";
			inputRandom._maxY.text = "";
			inputRandom._minX.text = "";
			inputRandom._minY.text = "";
		}
		while (inputsTransform.childCount > 0)
		{
			Transform child = inputsTransform.GetChild(0);
			child.SetParent(null);
			Object.Destroy(child.gameObject);
		}
		trialsCont = 1;
		if (gameObject == objInputTrial)
		{
			while (trialsCont <= startGame.trials)
			{
				AddObjInput(gameObject);
			}
		}
		else
		{
			AddObjInput(gameObject);
		}
		inputRandom = null;
		coordsButton.gameObject.SetActive(!isRandom);
	}

	public void AddObjInput(GameObject objInput)
	{
		GameObject obj = Object.Instantiate(objInput);
		obj.transform.parent = inputsTransform;
		if (obj.transform.GetChild(0).TryGetComponent<TextMeshProUGUI>(out var component))
		{
			component.text = $"Trial: {trialsCont}";
		}
		if (!isRandomic.isOn && inputsTransform.childCount > startGame.trials)
		{
			trialsField.text = inputsTransform.childCount.ToString();
		}
		trialsCont++;
	}

	public bool SetPosicaoBola()
	{
		if (string.IsNullOrEmpty(velocityField.text) || velocityField.text == "0")
		{
			return false;
		}
		if (string.IsNullOrEmpty(_pathLog))
		{
			return false;
		}
		int num = int.Parse(trialsField.text);
		_posicoesBola = new Vector2[num];
		if (isRandomic.isOn)
		{
			InputRandomCS componentInChildren = inputsTransform.GetComponentInChildren<InputRandomCS>();
			if (string.IsNullOrEmpty(componentInChildren._minX.text) || string.IsNullOrEmpty(componentInChildren._minY.text) || string.IsNullOrEmpty(componentInChildren._maxX.text) || string.IsNullOrEmpty(componentInChildren._maxY.text))
			{
				return false;
			}
			Vector2 vector = new Vector2(float.Parse(componentInChildren._minX.text), float.Parse(componentInChildren._minY.text));
			Vector2 vector2 = new Vector2(float.Parse(componentInChildren._maxX.text), float.Parse(componentInChildren._maxY.text));
			for (int i = 0; i < _posicoesBola.Length; i++)
			{
				_posicoesBola[i] = new Vector2(Random.Range(vector.x, vector2.x), Random.Range(vector.y, vector2.y));
			}
		}
		else
		{
			InputRandomCS[] componentsInChildren = inputsTransform.GetComponentsInChildren<InputRandomCS>();
			for (int j = 0; j < _posicoesBola.Length; j++)
			{
				if (string.IsNullOrEmpty(componentsInChildren[j]._maxX.text) || string.IsNullOrEmpty(componentsInChildren[j]._maxY.text))
				{
					return false;
				}
				float x = float.Parse(componentsInChildren[j]._maxX.text);
				float y = float.Parse(componentsInChildren[j]._maxY.text);
				_posicoesBola[j] = new Vector2(x, y);
			}
		}
		SetGraz();
		configsUI.SetActive(value: false);
		startUm.shootBall.SetParameters(num, _moveSpeed, _posicoesBola);
		CheckFeedbackFrases();
		return true;
	}

	public override void SetarConfigs()
	{
		base.SetarConfigs();
		spaceBar.SetActive(value: true);
		configsUI.SetActive(value: true);
		startGame.Started = true;
	}

	public override void BackToMenuConfigs()
	{
		base.BackToMenuConfigs();
		startGame.RestartGame();
		startGame.StopTasks();
		configsUI.SetActive(value: false);
	}

	public override void StartGame()
	{
		if (SetPosicaoBola())
		{
			base.StartGame();
		}
	}

	public override void EndCoroutine()
	{
		base.EndCoroutine();
		isDefault.isOn = loadedData.isDefault;
		trialsField.text = loadedData.trials.ToString() ?? "";
		AdjustContainerHeight(inputsTransform);
		AdjustContainerHeight(inputsTransform.parent.GetComponent<RectTransform>());
		velocityField.text = loadedData.ballVelocity.ToString() ?? "";
		intervalField.text = loadedData.ballInterval.ToString() ?? "";
		isRandomic.isOn = loadedData.isRandomicPos;
		if (isRandomic.isOn)
		{
			InputRandomCS componentInChildren = inputsTransform.GetComponentInChildren<InputRandomCS>();
			componentInChildren._maxX.text = loadedData.positions[0].ToString() ?? "";
			componentInChildren._maxY.text = loadedData.positions[1].ToString() ?? "";
			componentInChildren._minX.text = loadedData.positions[2].ToString() ?? "";
			componentInChildren._minY.text = loadedData.positions[3].ToString() ?? "";
			return;
		}
		InputRandomCS[] componentsInChildren = inputsTransform.GetComponentsInChildren<InputRandomCS>();
		int num = ((componentsInChildren.Length <= loadedData.positions.Length) ? componentsInChildren.Length : loadedData.positions.Length);
		int num2;
		for (num2 = 0; num2 < num; num2++)
		{
			componentsInChildren[num2]._maxX.text = loadedData.positions[num2].ToString() ?? "";
			num2++;
			componentsInChildren[num2]._maxY.text = loadedData.positions[num2].ToString() ?? "";
		}
	}
}
