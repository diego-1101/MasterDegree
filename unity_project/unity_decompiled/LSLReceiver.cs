using System.Collections;
using LSL;
using TMPro;
using UnityEngine;

public class LSLReceiver : MonoBehaviour
{
	private static LSLReceiver _instance;

	[Header("Configurações LSL")]
	public string streamName = "Python_EEG";

	public string streamType = "EEG";

	private StreamInfo[] streamInfos;

	private StreamInlet inlet;

	private float[] sample;

	private int channelCount;

	private float sampleRate;

	[Space]
	[Header("Painel")]
	public TMP_Text debugConection;

	private Animator debugAnimator;

	public PlayerController _player;

	public static LSLReceiver instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<LSLReceiver>();
			}
			return _instance;
		}
	}

	private void Start()
	{
		debugAnimator = debugConection.GetComponentInParent<Animator>();
	}

	public void CallInitialize(UIConfigs configs)
	{
		StartCoroutine(Initialize(configs));
	}

	private IEnumerator Initialize(UIConfigs configs)
	{
		debugAnimator.Play("Show_debug");
		yield return StartCoroutine(ResolveStream());
		if (streamInfos != null)
		{
			inlet = new StreamInlet(streamInfos[0]);
			channelCount = inlet.info().channel_count();
			sampleRate = (float)inlet.info().nominal_srate();
			sample = new float[channelCount];
			debugConection.text = $"Conectado a {streamName} - {channelCount} canais a {sampleRate}Hz";
			debugConection.color = Color.green;
			configs.SetarConfigs();
		}
		yield return new WaitForSeconds(1f);
		debugAnimator.Play("Hide_debug");
	}

	private IEnumerator ResolveStream()
	{
		streamInfos = null;
		float timeout = 5f;
		float startTime = Time.time;
		debugConection.text = "Procurando stream " + streamName + "...";
		debugConection.color = new Color32(207, 125, 0, byte.MaxValue);
		while (streamInfos == null || streamInfos.Length == 0)
		{
			streamInfos = global::LSL.LSL.resolve_stream("name", streamName, 1, 1.0);
			if (Time.time - startTime > timeout)
			{
				streamInfos = null;
				debugConection.text = "Stream não encontrado dentro do tempo limite!";
				debugConection.color = Color.red;
				yield return new WaitForSeconds(1f);
				debugAnimator.Play("Hide_debug");
				break;
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void Update()
	{
		if (inlet != null && inlet.pull_sample(sample, 0.0) != 0.0)
		{
			ProcessData(sample);
		}
	}

	private void ProcessData(float[] data)
	{
		if (StartGame.instance.GetStats())
		{
			_player.UpdateLegValues(data[0], data[1], data[2]);
		}
	}

	public void StopInlet()
	{
		if (inlet != null)
		{
			inlet.close_stream();
		}
	}
}
