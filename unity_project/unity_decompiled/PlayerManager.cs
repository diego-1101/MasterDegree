using Benchmarking;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class PlayerManager : MonoBehaviour
{
	[SerializeField]
	private bool m_FlythroughWhenIdle;

	[SerializeField]
	private float m_IdleTransitionTime;

	[SerializeField]
	private GameObject m_CrosshairCanvas;

	[SerializeField]
	private GameObject m_TouchInputCanvas;

	[SerializeField]
	private GameObject m_EventSystem;

	public PlayableDirector FlythroughDirector;

	private bool m_InFlythrough;

	private float m_TimeIdle;

	private CinemachineCamera m_VirtualCamera;

	private bool m_HasFocus;

	private void Start()
	{
		if (EventSystem.current == null)
		{
			m_EventSystem.SetActive(value: true);
		}
		if (PerformanceTest.RunningBenchmark)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		m_InFlythrough = false;
		if (SystemInfo.deviceType == DeviceType.Handheld)
		{
			m_TouchInputCanvas.SetActive(value: true);
		}
		m_VirtualCamera = GetComponentInChildren<CinemachineCamera>();
	}

	private void Update()
	{
		if (m_FlythroughWhenIdle && m_TimeIdle > m_IdleTransitionTime && !m_InFlythrough)
		{
			m_TimeIdle = 0f;
			EnableFlythrough();
		}
		m_TimeIdle += Time.unscaledDeltaTime;
	}

	private void Awake()
	{
		if (base.transform.parent == null)
		{
			Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	public void EnableFlythrough()
	{
		if (SceneTransitionManager.IsAvailable())
		{
			SceneTransitionManager.DisableLoadedScene();
			SceneTransitionManager.StopTransition();
		}
		if (FlythroughDirector == null)
		{
			m_InFlythrough = true;
			return;
		}
		FlythroughDirector.gameObject.SetActive(value: true);
		TimelineAsset timelineAsset = FlythroughDirector.playableAsset as TimelineAsset;
		FlythroughDirector.SetGenericBinding(timelineAsset.GetOutputTrack(0), CinemachineCore.FindPotentialTargetBrain(m_VirtualCamera));
		FlythroughDirector.time = 0.0;
		FlythroughDirector.Play();
		m_InFlythrough = true;
		m_CrosshairCanvas.SetActive(value: false);
		if (SystemInfo.deviceType == DeviceType.Handheld)
		{
			m_TouchInputCanvas.SetActive(value: false);
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		m_HasFocus = hasFocus;
	}

	public void EnableFirstPersonController()
	{
		if (SceneTransitionManager.IsAvailable())
		{
			SceneTransitionManager.DisableLoadedScene();
			SceneTransitionManager.StopTransition();
		}
		m_CrosshairCanvas.SetActive(value: true);
		if (FlythroughDirector != null)
		{
			FlythroughDirector.gameObject.SetActive(value: false);
		}
		m_InFlythrough = false;
	}

	public void NotifyPlayerMoved()
	{
		m_TimeIdle = 0f;
		if (m_InFlythrough)
		{
			EnableFirstPersonController();
			if (SystemInfo.deviceType == DeviceType.Handheld)
			{
				m_TouchInputCanvas.SetActive(value: true);
			}
		}
	}
}
