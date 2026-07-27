using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DynamicLightsManager
{
	private static DynamicLightsManager m_instance;

	private static List<DynamicLightController> m_dynamicLightsControllers = new List<DynamicLightController>();

	public static DynamicLightsManager instance
	{
		get
		{
			if (m_instance == null)
			{
				m_instance = new DynamicLightsManager();
			}
			return m_instance;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		Application.quitting += delegate
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		};
	}

	private static void OnBeginCameraRendering(ScriptableRenderContext _ctx, Camera _camera)
	{
		UpdateLightsForCamera(_camera);
	}

	private static void UpdateLightsForCamera(Camera _camera)
	{
		foreach (DynamicLightController dynamicLightsController in m_dynamicLightsControllers)
		{
			dynamicLightsController.UpdateForCamera(_camera);
		}
	}

	private static void ResetAllLights()
	{
		foreach (DynamicLightController dynamicLightsController in m_dynamicLightsControllers)
		{
			dynamicLightsController.Reset();
		}
	}

	public static void AddLight(DynamicLightController dynamicLightController)
	{
		if (!m_dynamicLightsControllers.Contains(dynamicLightController))
		{
			m_dynamicLightsControllers.Add(dynamicLightController);
		}
	}

	public static void RemoveLight(DynamicLightController dynamicLightController)
	{
		if (m_dynamicLightsControllers.Contains(dynamicLightController))
		{
			m_dynamicLightsControllers.Remove(dynamicLightController);
		}
	}

	public static void Clear()
	{
		m_dynamicLightsControllers.Clear();
	}
}
