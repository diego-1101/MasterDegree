using System.Collections.Generic;
using UnityEngine;

namespace Benchmarking;

public struct FrameData
{
	public float frameTime;

	private float _fpsOverride;

	public bool advancedFrameTiming;

	public double cpuTime;

	public double cpuRenderTime;

	public double gpuTime;

	public double timeLineTime;

	private const int k_timingsCaptureCount = 3;

	public float fps
	{
		get
		{
			if (!(_fpsOverride > 0f))
			{
				return 1000f / frameTime;
			}
			return _fpsOverride;
		}
	}

	public FrameData(float timeMS, float timelineTime = 0f)
	{
		frameTime = timeMS;
		timeLineTime = timelineTime;
		_fpsOverride = -1f;
		advancedFrameTiming = false;
		cpuTime = (cpuRenderTime = (gpuTime = timeMS));
	}

	public void CaptureFrameTimings()
	{
		advancedFrameTiming = FrameTimingManager.IsFeatureEnabled();
		if (!advancedFrameTiming)
		{
			return;
		}
		FrameTimingManager.CaptureFrameTimings();
		FrameTiming[] array = new FrameTiming[3];
		uint latestTimings = FrameTimingManager.GetLatestTimings(3u, array);
		if (latestTimings == 0)
		{
			return;
		}
		cpuTime = array[0].cpuFrameTime;
		cpuRenderTime = array[0].cpuRenderThreadFrameTime;
		gpuTime = array[0].gpuFrameTime;
		if (latestTimings > 1)
		{
			for (int i = 1; i < latestTimings; i++)
			{
				gpuTime += array[i].gpuFrameTime;
			}
			gpuTime /= latestTimings;
		}
	}

	public static FrameData GetCurrentFrameData(float timelineTime = 0f)
	{
		return new FrameData(Time.deltaTime * 1000f, timelineTime);
	}

	public void SetFPSOverride(float fpsOverride)
	{
		_fpsOverride = fpsOverride;
	}

	public void ResetFPSOverride()
	{
		_fpsOverride = -1f;
	}

	public static FrameData Min(FrameData a, FrameData b, ref bool bSmaller, bool overrideFPS = false)
	{
		FrameData result = default(FrameData);
		bSmaller = b.frameTime < a.frameTime;
		result.frameTime = (bSmaller ? b.frameTime : a.frameTime);
		if (overrideFPS)
		{
			result._fpsOverride = Mathf.Min(a.fps, b.fps);
		}
		else
		{
			result._fpsOverride = -1f;
		}
		result.advancedFrameTiming = a.advancedFrameTiming && b.advancedFrameTiming;
		if (result.advancedFrameTiming)
		{
			result.cpuTime = DoubleMin(a.cpuTime, b.cpuTime);
			result.cpuRenderTime = DoubleMin(a.cpuRenderTime, b.cpuRenderTime);
			result.gpuTime = DoubleMin(a.gpuTime, b.gpuTime);
		}
		return result;
	}

	public bool MinWith(FrameData other, bool overrideFPS = false)
	{
		bool bSmaller = false;
		this = Min(this, other, ref bSmaller, overrideFPS);
		return bSmaller;
	}

	public static FrameData Max(FrameData a, FrameData b, ref bool bGreater, bool overrideFPS = false)
	{
		FrameData result = default(FrameData);
		bGreater = b.frameTime > a.frameTime;
		result.frameTime = (bGreater ? b.frameTime : a.frameTime);
		if (overrideFPS)
		{
			result._fpsOverride = Mathf.Max(a.fps, b.fps);
		}
		else
		{
			result._fpsOverride = -1f;
		}
		result.advancedFrameTiming = a.advancedFrameTiming && b.advancedFrameTiming;
		if (result.advancedFrameTiming)
		{
			result.cpuTime = DoubleMax(a.cpuTime, b.cpuTime);
			result.cpuRenderTime = DoubleMax(a.cpuRenderTime, b.cpuRenderTime);
			result.gpuTime = DoubleMax(a.gpuTime, b.gpuTime);
		}
		return result;
	}

	public bool MaxWith(FrameData other, bool overrideFPS = false)
	{
		bool bGreater = false;
		this = Max(this, other, ref bGreater, overrideFPS);
		return bGreater;
	}

	public static FrameData Average(FrameData a, int countA, FrameData b, int countB, bool overrideFPS = false)
	{
		FrameData result = default(FrameData);
		float num = 1f / (float)(countA + countB);
		result.frameTime = (a.frameTime * (float)countA + b.frameTime * (float)countB) * num;
		if (overrideFPS)
		{
			result._fpsOverride = (a.fps * (float)countA + b.fps * (float)countB) * num;
		}
		else
		{
			result._fpsOverride = -1f;
		}
		result.advancedFrameTiming = a.advancedFrameTiming && b.advancedFrameTiming;
		if (result.advancedFrameTiming)
		{
			result.cpuTime = (a.cpuTime * (double)countA + b.cpuTime * (double)countB) * (double)num;
			result.cpuRenderTime = (a.cpuRenderTime * (double)countA + b.cpuRenderTime * (double)countB) * (double)num;
			result.gpuTime = (a.gpuTime * (double)countA + b.gpuTime * (double)countB) * (double)num;
		}
		return result;
	}

	public void AverageWith(FrameData other, int count, bool overrideFPS = false)
	{
		this = Average(this, count - 1, other, 1, overrideFPS);
	}

	public static FrameData Lerp(FrameData a, FrameData b, float t)
	{
		FrameData result = default(FrameData);
		result.frameTime = Mathf.Lerp(a.frameTime, b.frameTime, t);
		result._fpsOverride = -1f;
		result.advancedFrameTiming = a.advancedFrameTiming && b.advancedFrameTiming;
		if (result.advancedFrameTiming)
		{
			result.cpuTime = DoubleLerp(a.cpuTime, b.cpuTime, t);
			result.cpuRenderTime = DoubleLerp(a.cpuRenderTime, b.cpuRenderTime, t);
			result.gpuTime = DoubleLerp(a.gpuTime, b.gpuTime, t);
		}
		return result;
	}

	public static FrameData MinMultiple(List<FrameData> frameTimes)
	{
		if (frameTimes == null || frameTimes.Count < 1)
		{
			return default(FrameData);
		}
		FrameData result = frameTimes[0];
		for (int i = 1; i < frameTimes.Count; i++)
		{
			result.MinWith(frameTimes[i], overrideFPS: true);
		}
		return result;
	}

	public static FrameData MaxMultiple(List<FrameData> frameTimes)
	{
		if (frameTimes == null || frameTimes.Count < 1)
		{
			return default(FrameData);
		}
		FrameData result = frameTimes[0];
		for (int i = 1; i < frameTimes.Count; i++)
		{
			result.MaxWith(frameTimes[i], overrideFPS: true);
		}
		return result;
	}

	private static double DoubleMin(double a, double b)
	{
		if (!(a < b))
		{
			return b;
		}
		return a;
	}

	private static double DoubleMax(double a, double b)
	{
		if (!(a > b))
		{
			return b;
		}
		return a;
	}

	private static double DoubleLerp(double a, double b, double t)
	{
		return a * (1.0 - t) + b * t;
	}

	public override string ToString()
	{
		return $"FrameData{{frameTime: {frameTime} ms, fps: {fps}, advancedFrameTiming: {advancedFrameTiming}, cpuTime: {cpuTime} ms, cpuRenderTime: {cpuRenderTime} ms, gpuTime: {gpuTime} ms}}";
	}

	public string GetValueString(DataType dataType)
	{
		string text = "F2";
		return dataType switch
		{
			DataType.FPS => fps.ToString(text), 
			DataType.CPUTime => cpuTime.ToString(text), 
			DataType.CPURenderTime => cpuRenderTime.ToString(text), 
			DataType.GPUTime => gpuTime.ToString(text), 
			_ => frameTime.ToString(text), 
		};
	}

	public float GetValue(DataType dataType)
	{
		return dataType switch
		{
			DataType.FPS => fps, 
			DataType.CPUTime => (float)cpuTime, 
			DataType.CPURenderTime => (float)cpuRenderTime, 
			DataType.GPUTime => (float)gpuTime, 
			_ => frameTime, 
		};
	}

	public void WriteCSV(string before = null, bool writeTimeLineTime = false)
	{
		string[] array = new string[5 + ((before != null) ? 1 : 0) + (writeTimeLineTime ? 1 : 0)];
		int num = 0;
		if (before != null)
		{
			array[num] = before;
			num++;
		}
		if (writeTimeLineTime)
		{
			array[num] = timeLineTime.ToString();
			num++;
		}
		array[num] = frameTime.ToString();
		num++;
		array[num] = fps.ToString();
		num++;
		array[num] = cpuTime.ToString();
		num++;
		array[num] = cpuRenderTime.ToString();
		num++;
		array[num] = gpuTime.ToString();
		PerformanceTest.CSVWrinteLine(array);
	}
}
