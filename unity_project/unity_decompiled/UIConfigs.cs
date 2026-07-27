using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIConfigs : MonoBehaviour
{
	private static UIConfigs _instance;

	[HideInInspector]
	public PlayerController playerController;

	[HideInInspector]
	public StartGame startGame;

	[HideInInspector]
	public Dictionary<string, List<string>> dataInfos;

	public UIModo _ui;

	[HideInInspector]
	public ModoJogo _mode;

	public LegType _legType;

	[Space]
	[Header("UI")]
	[Space]
	public GameObject spaceBar;

	public CanvasGroup painelConfigs;

	public GameObject configsUI;

	public StatusUIController statusUI;

	public MenuBtnOperation confirmButton;

	public TMP_InputField inputStreamName;

	public TMP_InputField inputStreamType;

	private string fraseAcerto;

	private string fraseErro;

	[Space]
	[Header("Graz")]
	[Space]
	public TMP_InputField _baselineField;

	public TMP_InputField _fixationField;

	public TMP_Text _refFixationText;

	public TMP_InputField _feedbackField;

	public TMP_InputField _minPauseField;

	public TMP_InputField _maxPauseField;

	[SerializeField]
	private Image colorRefAcerto;

	[SerializeField]
	private Image colorRefErro;

	[SerializeField]
	private RectTransform _inputsTransformCue;

	public List<GameObject> cueConfigs;

	[SerializeField]
	private GameObject curInputObj;

	public int cueCount = 1;

	[Space]
	[Header("Trials")]
	[Space]
	public TMP_InputField trialsField;

	[HideInInspector]
	public int trialsCont;

	[HideInInspector]
	public int trialsAngleCont;

	[HideInInspector]
	public MenuBtnOperation actSkin;

	[Space]
	[Header("Path")]
	[Space]
	[SerializeField]
	private TextMeshProUGUI _pathMesh;

	private string archiveName;

	[Header("Default")]
	[Space]
	public Toggle isDefault;

	[SerializeField]
	private int defaultTrials = 25;

	[HideInInspector]
	public InputRandomCS inputRandom;

	private PhotoMode photoMode;

	[HideInInspector]
	public string _path;

	[HideInInspector]
	public string _pathFolder;

	[HideInInspector]
	public string _pathLog;

	private RealtimeLog _realtime;

	[HideInInspector]
	public Data loadedData;

	[HideInInspector]
	public Dictionary<string, List<string>> pathDataInfos;

	public static UIConfigs instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindAnyObjectByType<UIConfigs>();
			}
			return _instance;
		}
	}

	public virtual void Awake()
	{
		startGame = UnityEngine.Object.FindAnyObjectByType<StartGame>();
		startGame.statusUI = statusUI;
		playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
		playerController.StartPlayerController(statusUI);
		playerController.CameraSetup();
		LSLReceiver lSLReceiver = UnityEngine.Object.FindAnyObjectByType<LSLReceiver>();
		inputStreamName.text = lSLReceiver.streamName;
		inputStreamType.text = lSLReceiver.streamType;
	}

	public virtual void Start()
	{
		spaceBar.SetActive(value: false);
		startGame.Started = false;
		configsUI.SetActive(value: false);
	}

	public virtual void Update()
	{
		if (startGame != null && startGame.Started && Input.GetKeyDown(KeyCode.X))
		{
			BackToMenuConfigs();
		}
		if (actSkin != null)
		{
			confirmButton.enabled = true;
			confirmButton.GetComponent<Image>().color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		}
		else
		{
			confirmButton.enabled = false;
			confirmButton.GetComponent<Image>().color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, 160);
		}
		AdjustContainerHeight(_inputsTransformCue);
		AdjustContainerHeight(_inputsTransformCue.parent.GetComponent<RectTransform>());
	}

	public void SetMode(string mode)
	{
		_mode = ((mode == "treino") ? ModoJogo.TREINO : ModoJogo.PRINCIPAL);
	}

	public void SetLegType(TMP_Dropdown pernaDropdown)
	{
		string leg = GetLeg(pernaDropdown);
		playerController.LegChoice(leg);
		if (leg.Equals("esquerda"))
		{
			_legType = LegType.LEFT;
		}
		else if (leg.Equals("direita"))
		{
			_legType = LegType.RIGHT;
		}
		else
		{
			_legType = LegType.BOTH;
		}
	}

	public string GetLeg(TMP_Dropdown pernaDropdown)
	{
		return pernaDropdown.options[pernaDropdown.value].text.ToLower();
	}

	public bool GetPhotoActive()
	{
		if (photoMode == null)
		{
			photoMode = GetComponent<PhotoMode>();
		}
		return photoMode.modeActive;
	}

	private IEnumerator FixPanel()
	{
		base.transform.GetChild(0).gameObject.SetActive(value: false);
		yield return new WaitForSeconds(10f);
		base.transform.GetChild(0).gameObject.SetActive(value: true);
	}

	public virtual void SetDefault(bool isDefault)
	{
		if (isDefault)
		{
			trialsField.text = defaultTrials.ToString();
			startGame.SetupDefault = true;
		}
		else
		{
			trialsField.text = "1";
		}
	}

	public virtual void SetTrials(string trials)
	{
		if (!string.IsNullOrEmpty(trials))
		{
			startGame.trials = int.Parse(trials);
		}
	}

	public void AddCueInput()
	{
		GameObject input = UnityEngine.Object.Instantiate(curInputObj);
		input.transform.parent = _inputsTransformCue;
		CueConfigs config = input.GetComponent<CueConfigs>();
		config.minusBtn.onClick.AddListener(delegate
		{
			RemoveCue(input);
		});
		config.imageBtn.onClick.AddListener(delegate
		{
			CarregarImagem(config);
		});
		cueConfigs.Add(input);
		cueCount++;
	}

	public void RemoveCue(GameObject obj)
	{
		UnityEngine.Object.Destroy(obj);
		AdjustContainerHeight(_inputsTransformCue);
		AdjustContainerHeight(_inputsTransformCue.parent.GetComponent<RectTransform>());
		cueConfigs.Remove(obj);
		cueCount--;
	}

	public void AdjustContainerHeight(RectTransform rect)
	{
		float num = 0f;
		int num2 = 0;
		foreach (RectTransform item in rect)
		{
			if (item.gameObject.activeSelf)
			{
				num += item.sizeDelta.y;
				num2++;
			}
		}
		if (num2 > 1)
		{
			num += (float)(num2 - 1);
		}
		rect.sizeDelta = new Vector2(rect.sizeDelta.x, num);
	}

	public virtual void SetarConfigs()
	{
		painelConfigs.alpha = 0f;
	}

	public Dictionary<string, List<string>> FormatInfos(string path)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string text = "";
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = File.ReadAllLines(path);
		foreach (string text2 in array)
		{
			if (text2.StartsWith("[") && text2.EndsWith("]"))
			{
				if (text != "")
				{
					dictionary[text] = stringBuilder.ToString();
					stringBuilder.Clear();
				}
				text = text2.Trim('[', ']');
			}
			else
			{
				stringBuilder.AppendLine(text2);
			}
		}
		if (text != "")
		{
			dictionary[text] = stringBuilder.ToString();
		}
		Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
		foreach (KeyValuePair<string, string> item2 in dictionary)
		{
			StringReader stringReader = new StringReader(item2.Value);
			List<string> list = new List<string>();
			string item;
			while (!string.IsNullOrWhiteSpace(item = stringReader.ReadLine()))
			{
				list.Add(item);
			}
			List<string> list2 = new List<string>();
			foreach (string item3 in list)
			{
				list2.Add(item3);
			}
			dictionary2[item2.Key] = list2;
		}
		return dictionary2;
	}

	public virtual void StartGame()
	{
		LSLReceiver lSLReceiver = LSLReceiver.instance;
		lSLReceiver.streamName = inputStreamName.text;
		lSLReceiver.streamType = inputStreamType.text;
		lSLReceiver.CallInitialize(this);
	}

	public void SetGraz()
	{
		GrazController grazController = GrazController.instance;
		if (_mode == ModoJogo.TREINO)
		{
			grazController.ToggleCanvas(on: true);
		}
		grazController.baselineDuration = (string.IsNullOrEmpty(_baselineField.text) ? 10f : float.Parse(_baselineField.text));
		grazController.fixationDuration = (string.IsNullOrEmpty(_fixationField.text) ? 3f : float.Parse(_fixationField.text));
		grazController.feedbackDuration = (string.IsNullOrEmpty(_feedbackField.text) ? 3.75f : float.Parse(_feedbackField.text));
		grazController.minPauseDuration = (string.IsNullOrEmpty(_minPauseField.text) ? 1.5f : float.Parse(_minPauseField.text));
		grazController.maxPauseDuration = (string.IsNullOrEmpty(_maxPauseField.text) ? 3.5f : float.Parse(_maxPauseField.text));
	}

	public virtual void BackToMenuConfigs()
	{
		GrazController.instance.EndProtocol();
		painelConfigs.alpha = 1f;
	}

	public virtual void StartLogRealtime()
	{
		if (_realtime == null)
		{
			_realtime = UnityEngine.Object.FindAnyObjectByType<RealtimeLog>();
		}
		_realtime.StartPath(_pathLog);
	}

	public void CarregarInfos()
	{
		FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON Files", ".json"));
		FileBrowser.SetDefaultFilter(".json");
		StartCoroutine(ShowLoadDialogCoroutine(EndCoroutine));
	}

	private IEnumerator ShowLoadDialogCoroutine(Action onComplete)
	{
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, allowMultiSelection: false, null, null, "Selecione uma arquivo JSON", "Selecionar");
		if (FileBrowser.Success)
		{
			for (int i = 0; i < FileBrowser.Result.Length; i++)
			{
				_path = FileBrowser.Result[i];
			}
			string json = File.ReadAllText(_path);
			loadedData = JsonUtility.FromJson<Data>(json);
			onComplete?.Invoke();
		}
	}

	public void CarregarImagem(CueConfigs cue)
	{
		FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png", ".jpeg"));
		FileBrowser.SetDefaultFilter(".jpg");
		FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
		StartCoroutine(ShowLoadImageCoroutine(cue));
	}

	private IEnumerator ShowLoadImageCoroutine(CueConfigs cue)
	{
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, allowMultiSelection: false, null, null, "Selecione uma imagem", "Carregar");
		if (FileBrowser.Success)
		{
			for (int i = 0; i < FileBrowser.Result.Length; i++)
			{
				_path = FileBrowser.Result[i];
			}
			cue.path = _path;
		}
	}

	public virtual void EndCoroutine()
	{
	}

	public void SetArchiveName(string name)
	{
		archiveName = name;
	}

	public void SelecionarPath()
	{
		FileBrowser.SetFilters(showAllFilesFilter: false);
		StartCoroutine(SelectPathCoroutine());
	}

	private IEnumerator SelectPathCoroutine()
	{
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, allowMultiSelection: false, null, null, "Selecione uma Pasta", "Selecionar");
		if (FileBrowser.Success)
		{
			for (int i = 0; i < FileBrowser.Result.Length; i++)
			{
				_pathFolder = OnFilesSelected(FileBrowser.Result);
			}
			string text = _pathFolder + "/" + archiveName + ".csv";
			_pathMesh.text = text;
			_pathLog = text;
		}
	}

	private string OnFilesSelected(string[] filePaths)
	{
		return filePaths[0];
	}

	public void UpdateFeedbackAcerto(string str)
	{
		fraseAcerto = str;
	}

	public void UpdateFeedbackErro(string str)
	{
		fraseErro = str;
	}

	public void CheckFeedbackFrases()
	{
		if (string.IsNullOrEmpty(fraseAcerto))
		{
			fraseAcerto = "Parabéns";
		}
		if (string.IsNullOrEmpty(fraseErro))
		{
			fraseErro = "Tente novamente";
		}
		FeedbackController feedbackController = FeedbackController.instance;
		feedbackController.SetParabens(fraseAcerto, colorRefAcerto.color);
		feedbackController.SetErrou(fraseErro, colorRefErro.color);
	}

	public void ChangeGen(int value)
	{
		PlayerController.instance.ChangeGen(value * 100);
	}
}
