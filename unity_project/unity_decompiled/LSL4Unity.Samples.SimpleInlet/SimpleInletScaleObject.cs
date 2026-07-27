using System.Collections;
using LSL;
using UnityEngine;

namespace LSL4Unity.Samples.SimpleInlet;

public class SimpleInletScaleObject : MonoBehaviour
{
	public string StreamName;

	private ContinuousResolver resolver;

	private double max_chunk_duration = 0.2;

	private StreamInlet inlet;

	private float[,] data_buffer;

	private double[] timestamp_buffer;

	private void Start()
	{
		if (!StreamName.Equals(""))
		{
			resolver = new ContinuousResolver("name", StreamName);
			StartCoroutine(ResolveExpectedStream());
		}
		else
		{
			Debug.LogError("Object must specify a name for resolver to lookup a stream.");
			base.enabled = false;
		}
	}

	private IEnumerator ResolveExpectedStream()
	{
		StreamInfo[] array = resolver.results();
		while (array.Length == 0)
		{
			yield return new WaitForSeconds(0.1f);
			array = resolver.results();
		}
		inlet = new StreamInlet(array[0]);
		int num = (int)Mathf.Ceil((float)(inlet.info().nominal_srate() * max_chunk_duration));
		int num2 = inlet.info().channel_count();
		data_buffer = new float[num, num2];
		timestamp_buffer = new double[num];
	}

	private void Update()
	{
		if (inlet != null)
		{
			int num = inlet.pull_chunk(data_buffer, timestamp_buffer);
			if (num > 0)
			{
				float x = data_buffer[num - 1, 0];
				float y = data_buffer[num - 1, 1];
				float z = data_buffer[num - 1, 2];
				Vector3 localScale = new Vector3(x, y, z);
				base.gameObject.transform.localScale = localScale;
			}
		}
	}
}
