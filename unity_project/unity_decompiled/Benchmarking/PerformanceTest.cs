using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Benchmarking;

public class PerformanceTest : MonoBehaviour
{
	private static PerformanceTest _instance;

	public bool _autoStart = true;

	[FormerlySerializedAs("m_Stages")]
	public List<PerformanceTestStage> _stages;

	[SerializeField]
	public float _waitTime = 5f;

	[SerializeField]
	public int _framesToCapture = 500;

	[SerializeField]
	[FormerlySerializedAs("m_TestDataVisualTreeReference")]
	private VisualTreeAsset _testDataVisualTreeReference;

	[SerializeField]
	private int _currentTimingRefreshCount = 10;

	private int _currentTimingRefreshCounter = 11;

	private FrameData _maxCurrentTiming = new FrameData(0f);

	[SerializeField]
	private bool _liveRefreshGraph = true;

	[SerializeField]
	private GameObject _cameraPrefab;

	[SerializeField]
	private EventSystem _eventSystem;

	private int _currentStageIndex;

	private Camera _testCamera;

	private UIDocument _UIDocument;

	private TextElement _currentDataTypeLabel;

	private TextElement _currentTimingLabel;

	private TextElement _currentTimingUnitLabel;

	private Button _changeDataButtonNext;

	private Button _changeDataButtonPrev;

	private Button _closeButton;

	private static DataType _displayedDataType = DataType.FrameTime;

	private int _previousSleepTimeout;

	private int _previousTargetFrameRate = -1;

	private int _previousVSyncCount;

	private const float k_frameLineMul = 1000f;

	private const float k_cpuLineMul = 400f;

	private const float k_cpuRenderLineMul = 200f;

	private const float k_gpuLineMul = 400f;

	private static Dictionary<int, Color> k_fpsThresholds = new Dictionary<int, Color>
	{
		{
			30,
			Color.red
		},
		{
			45,
			Color.yellow
		},
		{
			60,
			Color.green
		},
		{
			90,
			Color.cyan
		},
		{
			120,
			Color.blue
		}
	};

	private static Color s_worstColor = Color.red;

	private static Dictionary<FrameData, Color> s_timingThresholds;

	private StringBuilder _sb = new StringBuilder("URP Template Performance Test");

	public static PerformanceTest instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindAnyObjectByType<PerformanceTest>();
			}
			return _instance;
		}
	}

	public bool liveRefreshGraph => _liveRefreshGraph;

	private PerformanceTestStage _currentStage => _stages[_currentStageIndex];

	public Camera testCamera
	{
		get
		{
			if (_testCamera == null)
			{
				CreateCamera();
			}
			return _testCamera;
		}
	}

	public static DataType displayedDataType => _displayedDataType;

	public static bool RunningBenchmark => _instance != null;

	public static Dictionary<FrameData, Color> timingThresholds
	{
		get
		{
			if (s_timingThresholds == null)
			{
				s_timingThresholds = new Dictionary<FrameData, Color>();
				foreach (KeyValuePair<int, Color> k_fpsThreshold in k_fpsThresholds)
				{
					s_timingThresholds.Add(new FrameData
					{
						frameTime = 1000f / (float)k_fpsThreshold.Key,
						cpuTime = 400f / (float)k_fpsThreshold.Key,
						cpuRenderTime = 200f / (float)k_fpsThreshold.Key,
						gpuTime = 400f / (float)k_fpsThreshold.Key
					}, k_fpsThreshold.Value);
				}
				s_worstColor = s_timingThresholds.First().Value;
			}
			return s_timingThresholds;
		}
	}

	public void RefreshEventSystem()
	{
		EventSystem.SetUITookitEventSystemOverride(_eventSystem);
	}

	public void SetCurrentTiming(FrameData currentFrameData)
	{
		_maxCurrentTiming.MaxWith(currentFrameData);
		if (_currentTimingRefreshCounter > _currentTimingRefreshCount)
		{
			_currentTimingLabel.text = _maxCurrentTiming.GetValueString(_displayedDataType);
			_currentTimingLabel.style.color = GetColorForFrameData(_maxCurrentTiming);
			_currentTimingRefreshCounter = 0;
			_maxCurrentTiming = new FrameData(0f);
		}
		_currentTimingRefreshCounter++;
	}

	public static Color GetColorForFrameData(FrameData frameData)
	{
		Color value = s_worstColor;
		if (_displayedDataType == DataType.FPS)
		{
			foreach (KeyValuePair<FrameData, Color> s_timingThreshold in s_timingThresholds)
			{
				if (frameData.fps < s_timingThreshold.Key.fps)
				{
					return value;
				}
				value = s_timingThreshold.Value;
			}
		}
		else
		{
			foreach (KeyValuePair<FrameData, Color> s_timingThreshold2 in s_timingThresholds)
			{
				if (frameData.frameTime < s_timingThreshold2.Key.frameTime)
				{
					value = s_timingThreshold2.Value;
				}
			}
		}
		return value;
	}

	private void Awake()
	{
		if (_instance != null)
		{
			Object.Destroy(this);
			return;
		}
		_instance = this;
		Object.DontDestroyOnLoad(this);
		PlayerManager playerManager = Object.FindFirstObjectByType<PlayerManager>();
		if (playerManager != null)
		{
			playerManager.gameObject.SetActive(value: false);
		}
		UnityEngine.Cursor.lockState = CursorLockMode.None;
	}

	private void Start()
	{
		Time.maximumDeltaTime = 120f;
		Application.runInBackground = true;
		QualitySettings.vSyncCount = 0;
		_UIDocument = GetComponent<UIDocument>();
		VisualElement rootVisualElement = _UIDocument.rootVisualElement;
		_currentDataTypeLabel = rootVisualElement.Q<TextElement>("DataTypeLabel");
		_currentTimingLabel = rootVisualElement.Q<TextElement>("CurrentTiming");
		_currentTimingUnitLabel = rootVisualElement.Q<TextElement>("CurrentTimingUnit");
		_changeDataButtonNext = rootVisualElement.Q<Button>("ChangeDataButtonNext");
		_changeDataButtonNext.clicked += LoopDisplayedDataNext;
		_changeDataButtonPrev = rootVisualElement.Q<Button>("ChangeDataButtonPrev");
		_changeDataButtonPrev.clicked += LoopDisplayedDataPrevious;
		SetDisplayedData(_displayedDataType);
		_closeButton = rootVisualElement.Q<Button>("CloseButton");
		_closeButton.style.opacity = 0f;
		VisualElement visualElement = rootVisualElement.Q<VisualElement>("TestsList");
		List<PerformanceTestStage> list = _stages.Where((PerformanceTestStage s) => SceneExists(s.sceneName) && s.enabled).ToList();
		for (int i = 0; i < list.Count; i++)
		{
			PerformanceTestStage performanceTestStage = list[i];
			performanceTestStage.InstantiateVisualElement(_testDataVisualTreeReference, visualElement);
			if (i < list.Count - 1)
			{
				performanceTestStage.SetFinishedAction(list[i + 1].Start);
			}
			else
			{
				performanceTestStage.SetFinishedAction(FinalizeTests);
			}
		}
		visualElement.MarkDirtyRepaint();
		_previousSleepTimeout = Screen.sleepTimeout;
		Screen.sleepTimeout = -1;
		_previousVSyncCount = QualitySettings.vSyncCount;
		QualitySettings.vSyncCount = 0;
		_previousTargetFrameRate = Application.targetFrameRate;
		Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value * 8;
		list[0].Start();
	}

	private static bool SceneExists(string sceneName)
	{
		for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
		{
			if (SceneUtility.GetScenePathByBuildIndex(i).Contains(sceneName))
			{
				return true;
			}
		}
		return false;
	}

	private void CreateCamera()
	{
		GameObject gameObject = Object.Instantiate(_cameraPrefab);
		_testCamera = gameObject.GetComponent<Camera>();
		Object.DontDestroyOnLoad(gameObject);
	}

	private void FinalizeTests()
	{
		Debug.Log(_sb.ToString());
		GUIUtility.systemCopyBuffer = _sb.ToString();
		_closeButton.style.opacity = 1f;
		_closeButton.clicked += CloseBenchmark;
	}

	private void SetDisplayedData(DataType dataType)
	{
		_displayedDataType = dataType;
		_currentDataTypeLabel.text = dataType.ToString();
		_currentTimingUnitLabel.text = ((dataType == DataType.FPS) ? "" : "ms");
		foreach (PerformanceTestStage stage in _stages)
		{
			stage.RefreshDisplayedData();
		}
	}

	private void LoopDisplayedDataNext()
	{
		LoopDisplayedData();
	}

	private void LoopDisplayedDataPrevious()
	{
		LoopDisplayedData(-1);
	}

	private void LoopDisplayedData(int offset = 1)
	{
		int num = ((!FrameTimingManager.IsFeatureEnabled()) ? 1 : 4);
		int num2 = (int)(_displayedDataType + offset);
		if (num2 < 0)
		{
			num2 = num;
		}
		else if (num2 > num)
		{
			num2 = 0;
		}
		SetDisplayedData((DataType)num2);
	}

	private void CloseBenchmark()
	{
		Object.Destroy(base.gameObject);
		PlayerManager playerManager = Object.FindFirstObjectByType<PlayerManager>();
		if (playerManager != null)
		{
			playerManager.gameObject.SetActive(value: true);
		}
		UnityEngine.Cursor.lockState = CursorLockMode.Locked;
		Screen.sleepTimeout = _previousSleepTimeout;
		QualitySettings.vSyncCount = _previousVSyncCount;
		Application.targetFrameRate = _previousTargetFrameRate;
		SceneManager.LoadScene(0);
	}

	public static void CSVWrinteLine(params string[] values)
	{
		instance._sb.AppendLine();
		for (int i = 0; i < values.Length; i++)
		{
			instance._sb.Append((values[i] == null) ? "" : values[i]);
			if (i < values.Length - 1)
			{
				instance._sb.Append(",");
			}
		}
	}
}
