using System.Collections.Generic;
using LSL4Unity.Utils;
using UnityEngine;

namespace LSL4Unity.Samples.Complex;

public class ChangePlaneColour : AStringOutlet
{
	public float ResetInterval = 3.4f;

	private float elapsed_time;

	private Material mat;

	public override List<string> ChannelNames => new List<string> { "Event" };

	public void Reset()
	{
		StreamName = "Unity.MatColour";
		StreamType = "Markers";
		moment = MomentForSampling.EndOfFrame;
		IrregularRate = true;
	}

	protected override void Start()
	{
		base.Start();
		Renderer component = GetComponent<Renderer>();
		mat = component.material;
	}

	protected override bool BuildSample()
	{
		elapsed_time += Time.deltaTime;
		if (elapsed_time >= ResetInterval)
		{
			mat.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
			sample[0] = mat.color.ToString();
			elapsed_time = 0f;
			return true;
		}
		return false;
	}
}
