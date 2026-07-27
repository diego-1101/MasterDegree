using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Benchmarking;

[Serializable]
public class PerformanceTestStage
{
	public bool enabled = true;

	[FormerlySerializedAs("SceneName")]
	public string sceneName;

	public Vector3 cameraPosition;

	public Quaternion cameraRotation;

	public bool useFullTimeline = true;

	private List<FrameData> _frameDatas;

	private FrameData _minFrameData;

	private FrameData _maxFrameData;

	private FrameData _avgFrameData;

	private FrameData _medianFrameData;

	private FrameData _lowerQuartileFrameData;

	private FrameData _upperQuartileFrameData;

	private int _recordingIndex;

	private Action _finishedAction;

	private PlayableDirector _playableDirector;

	private float _intermediateCaptureTime;

	private bool _uiInitialized;

	private VisualElement _visualElementRoot;

	private VisualElement _timingsGraphContainerVE;

	private VisualElement _quartilesVE;

	private VisualElement _quartilesMinMaxRangeVE;

	private VisualElement _quartilesRangeVE;

	private VisualElement _progressContainerVE;

	private VisualElement _progressBarVe;

	private Label _testNameLabel;

	private Label _statusLabel;

	private Label _timingsUnitLabel;

	private Label _minLabel;

	private Label _maxLabel;

	private Label _avgLabel;

	private Label _lowerQuartileLabel;

	private Label _medianLabel;

	private Label _upperQuartileLabel;

	private Label _progressLabel;

	private StatsGraphVE _timingsGraphVE;

	private bool _timingVisible = true;

	private Button _cancelButton;

	private TestStageStatus _status;

	private Dictionary<FrameData, VisualElement> _timingLines = new Dictionary<FrameData, VisualElement>();

	private bool sceneLoaded;

	private Camera _testCamera => PerformanceTest.instance.testCamera;

	public TestStageStatus status
	{
		get
		{
			return _status;
		}
		private set
		{
			_status = value;
			if (_statusLabel != null)
			{
				_statusLabel.text = _status.ToString();
			}
		}
	}

	private DataType _displayedDataType => PerformanceTest.displayedDataType;

	public void RecordTiming(FrameData currentFrameData)
	{
		bool flag = false;
		_frameDatas.Add(currentFrameData);
		if (_recordingIndex == 0)
		{
			_minFrameData = (_maxFrameData = (_avgFrameData = currentFrameData));
			flag = true;
		}
		else
		{
			flag |= _minFrameData.MinWith(currentFrameData, overrideFPS: true);
			flag |= _maxFrameData.MaxWith(currentFrameData, overrideFPS: true);
			_avgFrameData.AverageWith(currentFrameData, _recordingIndex + 1, overrideFPS: true);
		}
		_recordingIndex++;
		_avgLabel.Set(_avgFrameData);
		if (flag)
		{
			_minLabel.Set(_minFrameData);
			_maxLabel.Set(_maxFrameData);
			UpdateRangeAndGraph();
		}
	}

	public void InstantiateVisualElement(VisualTreeAsset referenceVisuaTree, VisualElement parent = null)
	{
		if (referenceVisuaTree == null)
		{
			return;
		}
		_visualElementRoot = referenceVisuaTree.Instantiate();
		_testNameLabel = _visualElementRoot.Q<Label>("TestName");
		_statusLabel = _visualElementRoot.Q<Label>("Status");
		_timingsUnitLabel = _visualElementRoot.Q<Label>("UnitText");
		_minLabel = _visualElementRoot.Q<Label>("MinText");
		_maxLabel = _visualElementRoot.Q<Label>("MaxText");
		_avgLabel = _visualElementRoot.Q<Label>("AvgText");
		_lowerQuartileLabel = _visualElementRoot.Q<Label>("LowerQuartileText");
		_medianLabel = _visualElementRoot.Q<Label>("MedianText");
		_upperQuartileLabel = _visualElementRoot.Q<Label>("UpperQuartileText");
		_timingsGraphContainerVE = _visualElementRoot.Q("TimingsGraph");
		_timingsGraphContainerVE.style.backgroundImage = null;
		_timingsGraphVE = new StatsGraphVE();
		_timingsGraphContainerVE.Add(_timingsGraphVE);
		if (!PerformanceTest.instance.liveRefreshGraph)
		{
			_timingsGraphVE.style.display = DisplayStyle.None;
			_timingVisible = false;
		}
		_quartilesVE = _visualElementRoot.Q("Quartiles");
		_quartilesMinMaxRangeVE = _visualElementRoot.Q("MinMaxRange");
		_quartilesRangeVE = _visualElementRoot.Q("QuartilesRange");
		_progressContainerVE = _visualElementRoot.Q("ProgressContainer");
		_progressBarVe = _visualElementRoot.Q("ProgressBar");
		_progressLabel = _visualElementRoot.Q<Label>("ProgressLabel");
		_progressContainerVE.style.opacity = 0f;
		_cancelButton = _visualElementRoot.Q<Button>("CancelButton");
		_cancelButton.clicked += Cancel;
		_cancelButton.text = "Skip";
		VisualElement visualElement = _visualElementRoot.Q("StatLine");
		visualElement.parent.Remove(visualElement);
		float num = 100f / (float)(PerformanceTest.timingThresholds.Count + 2);
		Length length = new Length(num, LengthUnit.Percent);
		foreach (KeyValuePair<FrameData, Color> timingThreshold in PerformanceTest.timingThresholds)
		{
			VisualElement visualElement2 = new VisualElement();
			visualElement2.style.backgroundColor = timingThreshold.Value;
			visualElement2.style.bottom = length;
			visualElement2.AddToClassList("StatLine");
			_timingsGraphContainerVE.Add(visualElement2);
			length.value += num;
			_timingLines.Add(timingThreshold.Key, visualElement2);
		}
		_testNameLabel.text = sceneName;
		parent?.Add(_visualElementRoot);
		_uiInitialized = true;
		status = TestStageStatus.Waiting;
	}

	public void CalculateValues(bool recalculateRange = false)
	{
		if (recalculateRange)
		{
			_minFrameData = FrameData.MinMultiple(_frameDatas);
			_maxFrameData = FrameData.MaxMultiple(_frameDatas);
		}
		FrameData[] array = new List<FrameData>(_frameDatas).OrderBy((FrameData v) => v.frameTime).ToArray();
		float num = (float)_frameDatas.Count * 0.25f;
		float num2 = (float)_frameDatas.Count * 0.5f;
		float num3 = (float)_frameDatas.Count * 0.75f;
		int num4 = (int)num;
		num -= (float)num4;
		int num5 = (int)num2;
		num2 -= (float)num5;
		int num6 = (int)num3;
		num3 -= (float)num6;
		int b = array.Length - 1;
		_lowerQuartileFrameData = FrameData.Lerp(array[num4], array[Mathf.Min(num4 + 1, b)], num);
		_medianFrameData = FrameData.Lerp(array[num5], array[Mathf.Min(num5 + 1, b)], num2);
		_upperQuartileFrameData = FrameData.Lerp(array[num6], array[Mathf.Min(num6 + 1, b)], num3);
		float fps = _lowerQuartileFrameData.fps;
		_lowerQuartileFrameData.SetFPSOverride(_upperQuartileFrameData.fps);
		_upperQuartileFrameData.SetFPSOverride(fps);
	}

	public void Start()
	{
		Start(null);
	}

	public void Start(Action finishedAction)
	{
		if (finishedAction != null)
		{
			_finishedAction = finishedAction;
		}
		PerformanceTest.instance.StartCoroutine(ProcessTest());
	}

	public void SetFinishedAction(Action finishedAction)
	{
		_finishedAction = finishedAction;
	}

	private IEnumerator ProcessTest()
	{
		if (status == TestStageStatus.Waiting)
		{
			yield return LoadAndInit();
			yield return new WaitForSeconds(PerformanceTest.instance._waitTime);
			yield return RunTest();
			yield return End();
		}
		if (_finishedAction != null)
		{
			_finishedAction();
		}
	}

	private IEnumerator LoadAndInit()
	{
		status = TestStageStatus.Warming;
		_cancelButton.text = "Stop";
		_testCamera.transform.position = cameraPosition;
		_testCamera.transform.rotation = cameraRotation;
		SceneManager.sceneLoaded += SceneLoadCallback;
		SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
		yield return new WaitUntil(HasSceneLoaded);
		yield return null;
		PerformanceTest.instance.RefreshEventSystem();
		DisableCamerasInScene();
		PlayableDirector[] array = Resources.FindObjectsOfTypeAll<PlayableDirector>();
		if (array.Length > 1 && array.Any((PlayableDirector d) => d.gameObject.name == "CinematicTimeline" || d.gameObject.CompareTag("BenchmarkTimeline")))
		{
			_playableDirector = array.First((PlayableDirector d) => d.gameObject.name == "CinematicTimeline" || d.gameObject.CompareTag("BenchmarkTimeline"));
		}
		else if (array.Length != 0)
		{
			_playableDirector = array[0];
		}
		if (_playableDirector != null)
		{
			_playableDirector.gameObject.SetActive(value: true);
			PlayableAsset playableAsset = _playableDirector.playableAsset;
			if (playableAsset.outputs.Any((PlayableBinding o) => o.outputTargetType == typeof(CinemachineBrain)))
			{
				UnityEngine.Object sourceObject = playableAsset.outputs.Single((PlayableBinding o) => o.outputTargetType == typeof(CinemachineBrain)).sourceObject;
				_playableDirector.SetGenericBinding(sourceObject, _testCamera.GetComponent<CinemachineBrain>());
			}
			float num = (float)_playableDirector.duration;
			_intermediateCaptureTime = num / (float)(PerformanceTest.instance._framesToCapture + 1);
			_playableDirector.Play();
			_playableDirector.extrapolationMode = DirectorWrapMode.None;
		}
		_ = PerformanceTest.instance._framesToCapture;
		if (_playableDirector != null && useFullTimeline)
		{
			_ = _playableDirector.duration;
		}
		_recordingIndex = 0;
		_frameDatas = new List<FrameData>();
		_maxFrameData = (_avgFrameData = (_medianFrameData = (_upperQuartileFrameData = (_lowerQuartileFrameData = new FrameData(0f)))));
		_minFrameData = new FrameData(float.PositiveInfinity);
	}

	private bool HasSceneLoaded()
	{
		return sceneLoaded;
	}

	private void SceneLoadCallback(Scene scene, LoadSceneMode loadSceneMode)
	{
		SceneManager.SetActiveScene(scene);
		sceneLoaded = true;
		SceneManager.sceneLoaded -= SceneLoadCallback;
	}

	private IEnumerator RunTest()
	{
		if (status != TestStageStatus.Warming)
		{
			yield break;
		}
		status = TestStageStatus.Running;
		_progressContainerVE.style.opacity = 1f;
		_progressLabel.text = "0";
		_progressBarVe.style.width = 0f;
		bool noIntermediateTime = useFullTimeline && _playableDirector != null;
		if (_playableDirector != null)
		{
			_playableDirector.time = 0.0;
			_playableDirector.Play();
		}
		while (status != TestStageStatus.Stopped && (_recordingIndex < _frameDatas.Count || (useFullTimeline && _playableDirector != null && _playableDirector.state != 0)))
		{
			FrameData currentFrameData = FrameData.GetCurrentFrameData();
			currentFrameData.CaptureFrameTimings();
			currentFrameData.timeLineTime = _playableDirector.time;
			PerformanceTest.instance.SetCurrentTiming(currentFrameData);
			RecordTiming(currentFrameData);
			UpdateGraph();
			int num = (int)(100.0 * _playableDirector.time / _playableDirector.duration);
			_progressLabel.text = num.ToString();
			_progressBarVe.style.width = P(num);
			if (noIntermediateTime)
			{
				yield return null;
			}
			else
			{
				yield return new WaitForSeconds(_intermediateCaptureTime);
			}
		}
	}

	private IEnumerator End()
	{
		_cancelButton.style.opacity = 0f;
		_cancelButton.clicked -= Cancel;
		if (status == TestStageStatus.Running)
		{
			status = TestStageStatus.Finished;
		}
		else if (status != TestStageStatus.Stopped)
		{
			yield break;
		}
		_progressContainerVE.style.opacity = 0f;
		CalculateValues();
		_lowerQuartileLabel.Set(_lowerQuartileFrameData);
		_upperQuartileLabel.Set(_upperQuartileFrameData);
		_medianLabel.Set(_medianFrameData);
		float num = 100f / _maxFrameData.GetValue(_displayedDataType);
		_quartilesMinMaxRangeVE.style.top = P(0f);
		_quartilesMinMaxRangeVE.style.bottom = P(_minFrameData.GetValue(_displayedDataType) * num);
		_quartilesRangeVE.style.top = P(100f - _upperQuartileFrameData.GetValue(_displayedDataType) * num);
		_quartilesRangeVE.style.bottom = P(_lowerQuartileFrameData.GetValue(_displayedDataType) * num);
		if (!_timingVisible)
		{
			_timingsGraphVE.style.display = DisplayStyle.Flex;
			_timingVisible = true;
			UpdateGraph();
		}
		WriteCSV();
		yield return null;
	}

	private void Cancel()
	{
		if (status == TestStageStatus.Waiting || status == TestStageStatus.Warming)
		{
			status = TestStageStatus.Skipped;
		}
		else
		{
			status = TestStageStatus.Stopped;
		}
		_cancelButton.style.opacity = 0f;
		_cancelButton.clicked -= Cancel;
	}

	private void DisableCamerasInScene()
	{
		Camera[] array = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
		foreach (Camera camera in array)
		{
			if (camera.gameObject != _testCamera.gameObject)
			{
				camera.enabled = false;
			}
		}
	}

	public void RefreshDisplayedData()
	{
		if (_uiInitialized && status != 0 && status != TestStageStatus.Skipped && status != TestStageStatus.Warming)
		{
			_timingsUnitLabel.text = ((PerformanceTest.displayedDataType == DataType.FPS) ? "" : "ms");
			_minLabel.Set(_minFrameData);
			_maxLabel.Set(_maxFrameData);
			_avgLabel.Set(_avgFrameData);
			_lowerQuartileLabel.Set(_lowerQuartileFrameData);
			_upperQuartileLabel.Set(_upperQuartileFrameData);
			_medianLabel.Set(_medianFrameData);
			UpdateRangeAndGraph();
		}
	}

	private void UpdateGraph()
	{
		if (_timingVisible && !_timingsGraphVE.isDirty && _uiInitialized && _frameDatas != null && _frameDatas.Count >= 2)
		{
			_timingsGraphVE.SetData(_frameDatas.Select((FrameData v) => v.GetValue(_displayedDataType) / _maxFrameData.GetValue(_displayedDataType)).ToList(), refresh: true);
		}
	}

	private void UpdateRangeAndGraph()
	{
		float num = 100f / _maxFrameData.GetValue(_displayedDataType);
		_minFrameData.GetValue(_displayedDataType);
		float value = _maxFrameData.GetValue(_displayedDataType);
		_quartilesMinMaxRangeVE.style.top = P(0f);
		_quartilesMinMaxRangeVE.style.bottom = P(_minFrameData.GetValue(_displayedDataType) * num);
		foreach (KeyValuePair<FrameData, VisualElement> timingLine in _timingLines)
		{
			float num2 = timingLine.Key.GetValue(_displayedDataType) / value;
			if (num2 < 1f)
			{
				timingLine.Value.style.bottom = P(100f * num2);
				timingLine.Value.style.display = DisplayStyle.Flex;
			}
			else
			{
				timingLine.Value.style.display = DisplayStyle.None;
			}
		}
		UpdateGraph();
	}

	public static Length P(float percentage, bool isNormalizedValue = false)
	{
		return new Length(isNormalizedValue ? (percentage * 100f) : percentage, LengthUnit.Percent);
	}

	public void WriteCSV()
	{
		PerformanceTest.CSVWrinteLine();
		PerformanceTest.CSVWrinteLine("Scene", sceneName);
		PerformanceTest.CSVWrinteLine();
		PerformanceTest.CSVWrinteLine("", "Frame Time", "FPS", "CPU time", "CPU Render Thread Time", "GPU Time");
		_minFrameData.WriteCSV("Minimum");
		_maxFrameData.WriteCSV("Maximum");
		_avgFrameData.WriteCSV("Average");
		_lowerQuartileFrameData.WriteCSV("Lower Quartile");
		_medianFrameData.WriteCSV("Median");
		_upperQuartileFrameData.WriteCSV("Upper Quartile");
		PerformanceTest.CSVWrinteLine();
		PerformanceTest.CSVWrinteLine("Captured frames", _frameDatas.Count().ToString());
		PerformanceTest.CSVWrinteLine("", "Frame Time", "FPS", "CPU time", "CPU Render Thread Time", "GPU Time");
		for (int i = 0; i < _frameDatas.Count(); i++)
		{
			_frameDatas[i].WriteCSV(null, writeTimeLineTime: true);
		}
	}
}
