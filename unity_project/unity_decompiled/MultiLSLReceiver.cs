using System.Collections;
using LSL;
using UnityEngine;

public class MultiLSLReceiver : MonoBehaviour
{
	[Header("Configurações LSL")]
	public string streamName = "MultiDataStream";

	public string streamType = "BioData";

	private StreamInlet inlet;

	private float[] sample;

	private int channelCount;

	private bool isConnected;

	private IEnumerator Start()
	{
		MonoBehaviour.print("Procurando stream: " + streamName + "...");
		StreamInfo[] streams = null;
		float timeout = 30f;
		float startTime = Time.time;
		while (streams == null || streams.Length == 0)
		{
			streams = global::LSL.LSL.resolve_stream("name", streamName, 1, 1.0);
			if (Time.time - startTime > timeout)
			{
				MonoBehaviour.print("Stream não encontrado!");
				yield break;
			}
			yield return new WaitForSeconds(0.5f);
		}
		inlet = new StreamInlet(streams[0]);
		channelCount = inlet.info().channel_count();
		sample = new float[channelCount];
		string[] array = new string[channelCount];
		inlet.info().desc().child("channels");
		isConnected = true;
		MonoBehaviour.print("Conectado a: " + streamName);
		Debug.Log($"Stream encontrado com {channelCount} canais:");
		string[] array2 = array;
		foreach (string text in array2)
		{
			Debug.Log("- " + text);
		}
	}

	private void Update()
	{
		if (isConnected && inlet.pull_sample(sample, 0.0) != 0.0)
		{
			UpdateDataDisplay(sample);
			ProcessValues(sample);
		}
	}

	private void UpdateDataDisplay(float[] values)
	{
		string text = "";
		for (int i = 0; i < values.Length; i++)
		{
			text += $"{GetChannelName(i)}: {values[i]:F2}\n";
		}
		MonoBehaviour.print(text);
	}

	private string GetChannelName(int index)
	{
		return index switch
		{
			0 => "Frequência Cardíaca", 
			1 => "Temperatura", 
			2 => "Resistência Galvânica", 
			3 => "Aceleração X", 
			4 => "Aceleração Y", 
			_ => $"Canal {index}", 
		};
	}

	private void ProcessValues(float[] values)
	{
		_ = values[0];
		float num = values[1];
		float value = values[2];
		float num2 = values[3];
		float num3 = values[4];
		base.transform.Translate(new Vector3(num2 * 0.1f, 0f, num3 * 0.1f));
		float t = Mathf.InverseLerp(1f, 20f, value);
		GetComponent<Renderer>().material.color = Color.Lerp(Color.blue, Color.red, t);
		if (num > 37.2f)
		{
			Debug.LogWarning($"Temperatura elevada: {num:F1}°C");
		}
	}

	private void OnDestroy()
	{
		if (inlet != null)
		{
			inlet.close_stream();
			Debug.Log("Stream LSL fechado");
		}
	}
}
