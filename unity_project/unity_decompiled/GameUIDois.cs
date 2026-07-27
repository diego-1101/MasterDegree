using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIDois : UIConfigs
{
	private static GameUIDois _instance;

	[HideInInspector]
	public StartDois startDois;

	[SerializeField]
	private float defaultGravidade = 9.71f;

	[SerializeField]
	private float defaultMargem = 1f;

	[SerializeField]
	private float defaultEspera = 3f;

	[SerializeField]
	private int defaultMaxAngle = 180;

	[SerializeField]
	private int defaultMinAngle = 90;

	[Space]
	[Header("Perna")]
	public float _rotacaoPerna;

	[SerializeField]
	private Toggle isRandomicAngle;

	[SerializeField]
	private RectTransform _inputsTransformAngle;

	[SerializeField]
	private GameObject _objInputRandomAngle;

	[SerializeField]
	private GameObject _objInputTrialAngle;

	[SerializeField]
	private MenuBtnOperation coordsButtonAngle;

	[Space]
	[Header("Gravidade")]
	[SerializeField]
	private float _gravidade;

	[SerializeField]
	private TMP_InputField gravidadeField;

	[Space]
	[Header("Medidor")]
	[SerializeField]
	private GameObject medidorUI;

	[Space]
	[SerializeField]
	private TMP_InputField margemField;

	[SerializeField]
	private TMP_InputField tempoPrecisaoField;

	[SerializeField]
	private Toggle isSplitTasks;

	[SerializeField]
	private RectTransform _inputsTransformTask;

	[SerializeField]
	private GameObject _objInputSliceTask;

	[SerializeField]
	private GameObject _objInputTask;

	[SerializeField]
	private MenuBtnOperation coordsButtonTask;

	[HideInInspector]
	public float[] _anguloPerna;

	public List<float> _temposPrecisao;

	private int trialsTaskCont;

	public static GameUIDois Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<GameUIDois>();
			}
			return _instance;
		}
	}

	public override void Awake()
	{
		base.Awake();
		startDois = Object.FindAnyObjectByType<StartDois>();
	}

	public override void Start()
	{
		base.Start();
		SetTrials("1");
		isDefault.isOn = true;
		SetTarefas(on: false);
		medidorUI.SetActive(value: false);
	}

	public override void Update()
	{
		base.Update();
		AdjustContainerHeight(_inputsTransformAngle);
		AdjustContainerHeight(_inputsTransformAngle.parent.GetComponent<RectTransform>());
		AdjustContainerHeight(_inputsTransformTask);
		AdjustContainerHeight(_inputsTransformTask.parent.GetComponent<RectTransform>());
	}

	public override void SetDefault(bool isDefault)
	{
		base.SetDefault(isDefault);
		if (isDefault)
		{
			gravidadeField.text = defaultGravidade.ToString();
			margemField.text = defaultMargem.ToString();
			tempoPrecisaoField.text = defaultEspera.ToString();
			isRandomicAngle.isOn = true;
		}
		else
		{
			gravidadeField.text = "9,81";
			margemField.text = "7,5";
			tempoPrecisaoField.text = "3";
			isRandomicAngle.isOn = false;
		}
	}

	public override void SetTrials(string trials)
	{
		base.SetTrials(trials);
		if (!isRandomicAngle.isOn)
		{
			SetRandomAngle(isRandom: false);
		}
	}

	public void SetGravidade(string gravidade)
	{
		if (!string.IsNullOrEmpty(gravidade))
		{
			_gravidade = float.Parse(gravidade);
		}
	}

	public bool SetRotacaoPerna()
	{
		if (string.IsNullOrEmpty(gravidadeField.text) || gravidadeField.text == "0" || string.IsNullOrEmpty(margemField.text) || margemField.text == "0")
		{
			return false;
		}
		if (string.IsNullOrEmpty(_pathLog))
		{
			return false;
		}
		int num = int.Parse(trialsField.text);
		float height = float.Parse(margemField.text) * 10f;
		_anguloPerna = new float[num];
		_temposPrecisao = new List<float>();
		if (isRandomicAngle.isOn)
		{
			InputRandomCS componentInChildren = _inputsTransformAngle.GetComponentInChildren<InputRandomCS>();
			if (string.IsNullOrEmpty(componentInChildren._maxX.text) || string.IsNullOrEmpty(componentInChildren._maxY.text))
			{
				return false;
			}
			float minInclusive = float.Parse(componentInChildren._maxY.text);
			float maxInclusive = float.Parse(componentInChildren._maxX.text);
			for (int i = 0; i < _anguloPerna.Length; i++)
			{
				_anguloPerna[i] = Random.Range(minInclusive, maxInclusive) - 90f;
			}
		}
		else
		{
			InputRandomCS[] componentsInChildren = _inputsTransformAngle.GetComponentsInChildren<InputRandomCS>();
			for (int j = 0; j < _anguloPerna.Length; j++)
			{
				if (string.IsNullOrEmpty(componentsInChildren[j]._maxX.text))
				{
					return false;
				}
				_anguloPerna[j] = float.Parse(componentsInChildren[j]._maxX.text) - 90f;
			}
		}
		if (isSplitTasks.isOn)
		{
			InputRandomCS[] componentsInChildren2 = _inputsTransformTask.GetComponentsInChildren<InputRandomCS>();
			for (int k = 0; k < _inputsTransformTask.childCount; k++)
			{
				if (string.IsNullOrEmpty(componentsInChildren2[k]._maxX.text) || string.IsNullOrEmpty(componentsInChildren2[k]._maxY.text))
				{
					return false;
				}
				for (int l = 0; l < int.Parse(componentsInChildren2[k]._maxY.text); l++)
				{
					_temposPrecisao.Add(float.Parse(componentsInChildren2[k]._maxX.text));
				}
			}
		}
		else
		{
			InputRandomCS componentInChildren2 = _inputsTransformTask.GetComponentInChildren<InputRandomCS>();
			_temposPrecisao.Add(float.Parse(componentInChildren2._maxX.text));
		}
		SetGraz();
		startDois.trials = num;
		startDois.HeightMargem(height, _temposPrecisao[0]);
		_rotacaoPerna = _anguloPerna[0];
		Physics.gravity = new Vector3(Physics.gravity.x, 0f - _gravidade, Physics.gravity.z);
		CheckFeedbackFrases();
		return true;
	}

	public void SetRandomAngle(bool isRandom)
	{
		GameObject gameObject = (isRandom ? _objInputRandomAngle : _objInputTrialAngle);
		if (isRandom)
		{
			inputRandom = gameObject.GetComponent<InputRandomCS>();
		}
		if ((bool)inputRandom && isDefault.isOn)
		{
			inputRandom._maxX.text = defaultMaxAngle.ToString();
			inputRandom._maxY.text = defaultMinAngle.ToString();
		}
		else if ((bool)inputRandom && !isDefault.isOn)
		{
			inputRandom._maxX.text = "";
			inputRandom._maxY.text = "";
		}
		while (_inputsTransformAngle.childCount > 0)
		{
			Transform child = _inputsTransformAngle.GetChild(0);
			child.SetParent(null);
			Object.Destroy(child.gameObject);
		}
		trialsAngleCont = 1;
		if (gameObject == _objInputTrialAngle)
		{
			while (trialsAngleCont <= startGame.trials)
			{
				AddObjAngle(gameObject);
			}
		}
		else
		{
			AddObjAngle(gameObject);
		}
		inputRandom = null;
		coordsButtonAngle.gameObject.SetActive(!isRandom);
	}

	public void AddObjAngle(GameObject objInput)
	{
		GameObject obj = Object.Instantiate(objInput);
		obj.transform.parent = _inputsTransformAngle;
		if (obj.transform.GetChild(0).TryGetComponent<TextMeshProUGUI>(out var component))
		{
			component.text = $"Trial: {trialsAngleCont}";
		}
		if (!isRandomicAngle.isOn && _inputsTransformAngle.childCount > startGame.trials)
		{
			trialsField.text = _inputsTransformAngle.childCount.ToString();
		}
		trialsAngleCont++;
	}

	public void SetTarefas(bool on)
	{
		GameObject gameObject = (on ? _objInputSliceTask : _objInputTask);
		while (_inputsTransformTask.childCount > 0)
		{
			Transform child = _inputsTransformTask.GetChild(0);
			child.SetParent(null);
			Object.Destroy(child.gameObject);
		}
		trialsTaskCont = 1;
		if (gameObject == _objInputSliceTask)
		{
			while (trialsTaskCont < 3)
			{
				AddObjTask(gameObject);
			}
		}
		else
		{
			AddObjTask(gameObject);
		}
		coordsButtonTask.gameObject.SetActive(on);
	}

	public void AddObjTask(GameObject objInput)
	{
		GameObject obj = Object.Instantiate(objInput);
		obj.transform.parent = _inputsTransformTask;
		if (obj.transform.GetChild(0).TryGetComponent<TextMeshProUGUI>(out var component))
		{
			if (objInput == _objInputSliceTask)
			{
				component.text = $"Tarefa: {trialsTaskCont}";
			}
			else
			{
				component.text = "Descanso";
			}
		}
		_ = trialsTaskCont;
		trialsTaskCont++;
	}

	public override void SetarConfigs()
	{
		base.SetarConfigs();
		spaceBar.SetActive(value: true);
		startGame.Started = true;
		configsUI.SetActive(value: true);
	}

	public override void StartGame()
	{
		if (SetRotacaoPerna())
		{
			base.StartGame();
		}
	}

	public override void EndCoroutine()
	{
		base.EndCoroutine();
		isDefault.isOn = loadedData.isDefault;
		trialsField.text = loadedData.trials.ToString() ?? "";
		AdjustContainerHeight(_inputsTransformAngle);
		AdjustContainerHeight(_inputsTransformAngle.parent.GetComponent<RectTransform>());
		isRandomicAngle.isOn = loadedData.isRandomicAngle;
		if (isRandomicAngle.isOn)
		{
			InputRandomCS componentInChildren = _inputsTransformAngle.GetComponentInChildren<InputRandomCS>();
			componentInChildren._maxX.text = loadedData.variations[0].ToString() ?? "";
			componentInChildren._maxY.text = loadedData.variations[1].ToString() ?? "";
		}
		else
		{
			InputRandomCS[] componentsInChildren = _inputsTransformAngle.GetComponentsInChildren<InputRandomCS>();
			int num = ((componentsInChildren.Length <= loadedData.variations.Length) ? componentsInChildren.Length : loadedData.variations.Length);
			for (int i = 0; i < num; i++)
			{
				componentsInChildren[i]._maxX.text = loadedData.variations[i].ToString() ?? "";
			}
		}
		gravidadeField.text = loadedData.gravidade.ToString() ?? "";
		margemField.text = loadedData.margem.ToString() ?? "";
		isSplitTasks.isOn = loadedData.isSplitTasks;
		if (isSplitTasks.isOn)
		{
			InputRandomCS[] componentsInChildren2 = _inputsTransformTask.GetComponentsInChildren<InputRandomCS>();
			if (loadedData.totalDiv > componentsInChildren2.Length)
			{
				for (int j = 2; j < loadedData.totalDiv; j++)
				{
					AddObjTask(_objInputSliceTask);
				}
				AdjustContainerHeight(_inputsTransformTask);
				AdjustContainerHeight(_inputsTransformTask.parent.GetComponent<RectTransform>());
				componentsInChildren2 = _inputsTransformTask.GetComponentsInChildren<InputRandomCS>();
			}
			int num2 = ((componentsInChildren2.Length <= loadedData.tempoEspera.Length) ? componentsInChildren2.Length : loadedData.tempoEspera.Length);
			for (int k = 0; k < num2; k++)
			{
				componentsInChildren2[k]._maxX.text = loadedData.tempoEspera[k].ToString() ?? "";
			}
		}
		else
		{
			tempoPrecisaoField.text = loadedData.tempoEspera[0].ToString() ?? "";
		}
	}

	public override void BackToMenuConfigs()
	{
		base.BackToMenuConfigs();
		startGame.RestartGame();
		configsUI.SetActive(value: false);
	}
}
