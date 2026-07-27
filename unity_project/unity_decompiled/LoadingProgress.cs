using System;
using UnityEngine;

public class LoadingProgress : IProgress<float>
{
	public event Action<float> progressed;

	public void Report(float value)
	{
		float obj = Mathf.Clamp01(value);
		this.progressed?.Invoke(obj);
	}
}
