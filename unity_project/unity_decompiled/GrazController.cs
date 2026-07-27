using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrazController : MonoBehaviour
{
	private static GrazController _instance;

	[SerializeField]
	private CanvasGroup _group;

	[Header("Baseline Phase")]
	public float baselineDuration = 10f;

	[Header("Fixation Phase")]
	public float fixationDuration = 3f;

	public RawImage indicationImage;

	[Header("Cue Phase")]
	public float cueDuration = 1f;

	public RawImage cueImageDirection;

	public Texture defaultCueImage;

	[Header("Feedback Phase")]
	public float feedbackDuration = 3.75f;

	[HideInInspector]
	public bool OnFeedbackPhase;

	[Header("Pause Phase")]
	public float minPauseDuration = 1.5f;

	public float maxPauseDuration = 3.5f;

	[HideInInspector]
	public float grazInterval;

	[HideInInspector]
	public bool inGrazProtocol;

	private AudioSource source;

	private Coroutine grazCoroutine;

	private RealtimeLog realtimeLog;

	private StartGame startGame;

	private float randomDuration;

	public static GrazController instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<GrazController>();
			}
			return _instance;
		}
	}

	private void Awake()
	{
		if (TryGetComponent<AudioSource>(out var component))
		{
			source = component;
		}
		else
		{
			source = base.gameObject.AddComponent<AudioSource>();
		}
		indicationImage.enabled = false;
		cueImageDirection.enabled = false;
		ToggleCanvas(on: false);
	}

	private void Start()
	{
		if (realtimeLog == null)
		{
			realtimeLog = RealtimeLog.instance;
		}
		realtimeLog._acerto = "NEUTRO";
	}

	public void ToggleCanvas(bool on)
	{
		CanvasGroup group = _group;
		float alpha;
		if (!on)
		{
			float num2 = (_group.alpha = 0f);
			alpha = num2;
		}
		else
		{
			float num2 = (_group.alpha = 1f);
			alpha = num2;
		}
		group.alpha = alpha;
		_group.interactable = on;
	}

	public void StartProtocol()
	{
		grazCoroutine = StartCoroutine(GrazProtocol());
	}

	public void SetCueImageDirection(float dir)
	{
		cueImageDirection.rectTransform.eulerAngles = new Vector3(cueImageDirection.rectTransform.rotation.x, cueImageDirection.rectTransform.rotation.y, dir);
	}

	public void EndProtocol()
	{
		if (grazCoroutine != null)
		{
			StopCoroutine(grazCoroutine);
		}
	}

	private IEnumerator GrazProtocol()
	{
		Debug.Log("Protocolo Graz iniciado");
		if (startGame == null)
		{
			startGame = StartGame.instance;
		}
		int trials = startGame.trials;
		yield return BaselinePhase();
		realtimeLog._acerto = "NEUTRO";
		inGrazProtocol = true;
		int cueCount = 0;
		List<GameObject> cueConfigs = UIConfigs.instance.cueConfigs;
		while (cueCount < UIConfigs.instance.cueCount)
		{
			realtimeLog._classe = cueCount;
			CueConfigs component = cueConfigs[cueCount].GetComponent<CueConfigs>();
			cueDuration = (string.IsNullOrEmpty(component.field.text) ? 5f : float.Parse(component.field.text));
			component.SetCueImage();
			UIConfigs.instance.SetLegType(component.dropdown);
			if (UIConfigs.instance._ui == UIModo.Dois)
			{
				if (UIConfigs.instance._legType == LegType.LEFT)
				{
					realtimeLog._target = "ESQUERDA";
					SetCueImageDirection(180f);
				}
				else
				{
					realtimeLog._target = "DIREITA";
					SetCueImageDirection(0f);
				}
			}
			while (startGame.actTrials < trials && StartGame.instance.Started)
			{
				realtimeLog._tentativa = startGame.actTrials;
				Debug.Log($"Trial {StartGame.instance.actTrials} / {trials}");
				yield return FixationPhase();
				yield return CuePhase(cueCount);
				yield return FeedbackPhase();
				realtimeLog._acerto = "NEUTRO";
				yield return PausePhase();
				startGame.actTrials++;
			}
			cueCount++;
			StartGame.instance.actTrials = 0;
		}
		inGrazProtocol = false;
		Debug.Log("Protocolo Graz concluido");
		StartGame.instance.RestartGame();
	}

	public IEnumerator BaselinePhase()
	{
		Debug.Log("Inicio da baseline");
		if (realtimeLog == null)
		{
			realtimeLog = RealtimeLog.instance;
		}
		realtimeLog._marcador = "BASE";
		realtimeLog._marcadorNum = 1;
		yield return new WaitForSeconds(baselineDuration);
		Debug.Log("Fim da baseline");
	}

	public IEnumerator FixationPhase()
	{
		Debug.Log("Inicio do fixation");
		indicationImage.enabled = true;
		realtimeLog._marcador = "FIX";
		realtimeLog._marcadorNum = 2;
		yield return new WaitForSeconds(fixationDuration);
		Debug.Log("Fim do fixation");
	}

	public IEnumerator CuePhase(int cue)
	{
		Debug.Log("Inicio do cue");
		cueImageDirection.enabled = true;
		indicationImage.enabled = false;
		if (BallLauncher.instance != null)
		{
			BallLauncher.instance.LaunchSingleBall();
		}
		realtimeLog._marcador = "CUE";
		realtimeLog._marcadorNum = 3;
		yield return new WaitForSeconds(cueDuration);
		cueImageDirection.enabled = false;
		Debug.Log("Fim do cue");
	}

	public IEnumerator FeedbackPhase()
	{
		Debug.Log("Inicio do feedback");
		OnFeedbackPhase = true;
		realtimeLog._marcador = "FEED";
		realtimeLog._marcadorNum = 4;
		yield return new WaitForSeconds(feedbackDuration);
		OnFeedbackPhase = false;
		if (StartGame.instance.name == "Iniciar Game Dois")
		{
			MedidorMovement componentInChildren = UIConfigs.instance.statusUI.medidor.GetComponentInChildren<MedidorMovement>();
			if (componentInChildren.entrouOver)
			{
				componentInChildren.ConfirmOverlapping();
				componentInChildren.entrouOver = false;
				FeedbackController.instance.PlayErrou();
			}
		}
		Debug.Log("Fim do feedback");
	}

	public IEnumerator PausePhase()
	{
		Debug.Log("Inicio do pause");
		realtimeLog._marcador = "PAUSE";
		realtimeLog._marcadorNum = 5;
		yield return new WaitForSeconds(randomDuration);
		Debug.Log("Fim do pause");
	}
}
