using System;
using System.Collections.Generic;
using Benchmarking;
using StarterAssets;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
	[Tooltip("Enable to debug the transition effect")]
	[SerializeField]
	private bool m_OverrideTransition;

	[SerializeField]
	[Range(0f, 1f)]
	private float m_ManualTransition;

	[Tooltip("The amount of time it takes to transition between two scenes")]
	[SerializeField]
	private float m_TransitionTime;

	private Camera m_MainCamera;

	private Camera m_ScreenCamera;

	private CharacterController m_Player;

	private PlayerManager m_CameraManager;

	private bool m_InitialSceneLoad;

	private static SceneTransitionManager instance;

	[Tooltip("Layers to render when in a location")]
	[SerializeField]
	private LayerMask locationLayer;

	[Tooltip("Layers to render when in the terminal")]
	[SerializeField]
	private LayerMask m_TerminalLayer;

	private bool InTerminal = true;

	private bool m_EneableCharacterInFollowingFrame;

	private SceneLoad m_Loader;

	private Transform spawnTransform;

	private Vector3 m_PositionAtLock;

	private Quaternion m_RotationAtLock;

	private bool InTransition;

	private bool CoolingOff;

	private float ElapsedTimeInTransition;

	private Dictionary<string, SceneMetaData> registeredScenes;

	private SceneMetaData screenScene;

	private SceneMetaData currentScene;

	private int m_ScenesLoading;

	private Vector3 m_CameraPosition;

	private Quaternion m_CameraRotation;

	private MediaSceneLoader m_MediaSceneLoader;

	private int m_TransitionAmountShaderProperty;

	private bool m_ScreenOff;

	private void Awake()
	{
		if (PerformanceTest.RunningBenchmark)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		SetupSingleton();
		SetupReferences();
		SetupInitialState();
	}

	public void SetupSingleton()
	{
		if (instance != null)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
	}

	public void SetupReferences()
	{
		m_Player = GameObject.Find("PlayerCapsule")?.GetComponent<CharacterController>();
		if (m_Player == null)
		{
			Debug.Log("Couldn't find character controller");
		}
		m_CameraManager = m_Player.transform.parent.GetComponent<PlayerManager>();
		m_MainCamera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
		if (m_MainCamera == null)
		{
			Debug.Log("Couldn't find Main Camera");
		}
		m_ScreenCamera = GameObject.FindGameObjectWithTag("ScreenCamera").GetComponent<Camera>();
		if (m_ScreenCamera == null)
		{
			Debug.Log("Couldn't find Screen Camera");
		}
		m_ScreenCamera.GetComponent<Camera>().enabled = false;
		m_TransitionAmountShaderProperty = Shader.PropertyToID("_TransitionAmount");
	}

	public void SetupInitialState()
	{
		InTerminal = true;
		m_InitialSceneLoad = true;
		registeredScenes = new Dictionary<string, SceneMetaData>();
		m_ScreenOff = true;
		RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
	}

	private void Update()
	{
		if (m_EneableCharacterInFollowingFrame)
		{
			m_Player.GetComponent<FirstPersonController>().enabled = true;
			m_EneableCharacterInFollowingFrame = false;
		}
		float num = (m_OverrideTransition ? m_ManualTransition : (ElapsedTimeInTransition / m_TransitionTime));
		if (InTransition)
		{
			ElapsedTimeInTransition += Time.deltaTime;
			if (ElapsedTimeInTransition > m_TransitionTime)
			{
				TriggerTeleport();
			}
			ElapsedTimeInTransition = Mathf.Min(m_TransitionTime, ElapsedTimeInTransition);
		}
		else
		{
			ElapsedTimeInTransition -= Time.deltaTime * 3f;
			if (ElapsedTimeInTransition < 0f && CoolingOff)
			{
				CoolingOff = false;
			}
			ElapsedTimeInTransition = Mathf.Max(0f, ElapsedTimeInTransition);
		}
		if (m_Loader != null && !CoolingOff)
		{
			float num2 = num * num;
			m_Loader.SetVolumeWeights(1f - num2);
		}
		Shader.SetGlobalFloat(m_TransitionAmountShaderProperty, num);
	}

	private void TriggerTeleport()
	{
		InTransition = false;
		if (m_Loader != null)
		{
			m_Loader.SetVolumeWeights(1f);
		}
		if ((bool)m_MediaSceneLoader)
		{
			CinemachineTeleport();
		}
		else
		{
			Teleport();
		}
		m_Loader = null;
		CoolingOff = true;
	}

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		bool flag = camera.CompareTag("MainCamera");
		if (flag || !(screenScene == null))
		{
			if (camera.cameraType == CameraType.SceneView)
			{
				ToggleMainLight(currentScene, value: true);
				ToggleMainLight(screenScene, value: false);
			}
			else
			{
				ToggleMainLight(currentScene, flag);
				ToggleMainLight(screenScene, !flag);
			}
			SceneMetaData sceneMetaData = ((flag || camera.cameraType == CameraType.SceneView) ? currentScene : screenScene);
			RenderSettings.fog = sceneMetaData.FogEnabled;
			RenderSettings.skybox = sceneMetaData.skybox;
			if (sceneMetaData.reflection != null)
			{
				RenderSettings.customReflectionTexture = sceneMetaData.reflection;
			}
			if (!flag && camera.cameraType == CameraType.Game)
			{
				camera.GetComponent<OffsetCamera>().UpdateWithOffset();
			}
		}
	}

	private void ToggleMainLight(SceneMetaData scene, bool value)
	{
		if (scene != null && scene.mainLight != null)
		{
			scene.mainLight.enabled = value;
		}
	}

	public static void CinemachineTeleport()
	{
		instance.InTerminal = !instance.InTerminal;
		instance.UpdateCullingMasks();
		Transform transform = instance.m_MediaSceneLoader.transform;
		if (!instance.InTerminal)
		{
			transform.position = instance.m_ScreenCamera.GetComponent<OffsetCamera>().GetOffset();
			instance.m_MediaSceneLoader.GetTerminalSceneLoader().SetCurrentVolume(instance.m_Loader.GetDestinationVolume());
			instance.screenScene.TerminalLoader.SetActive(value: true);
		}
		else
		{
			transform.position = Vector3.zero;
			instance.m_Loader = instance.m_MediaSceneLoader.GetTerminalSceneLoader();
		}
		SceneTransitionManager sceneTransitionManager = instance;
		SceneTransitionManager sceneTransitionManager2 = instance;
		SceneMetaData sceneMetaData = instance.currentScene;
		SceneMetaData sceneMetaData2 = instance.screenScene;
		sceneTransitionManager.screenScene = sceneMetaData;
		sceneTransitionManager2.currentScene = sceneMetaData2;
	}

	public static void Teleport()
	{
		if (!instance.currentScene || !instance.screenScene)
		{
			Debug.LogError("Can't teleport without two scenes enabled");
			return;
		}
		instance.InTerminal = !instance.InTerminal;
		instance.UpdateCullingMasks();
		Transform transform = instance.m_Player.transform;
		transform.GetComponent<FirstPersonController>().enabled = false;
		bool num = instance.screenScene.CameraLockTransform != null;
		bool flag = instance.currentScene.CameraLockTransform != null;
		if (num)
		{
			instance.m_PositionAtLock = transform.position;
			instance.m_RotationAtLock = transform.rotation;
			Transform cameraLockTransform = instance.screenScene.CameraLockTransform;
			transform.parent.parent = cameraLockTransform;
			transform.position = cameraLockTransform.position;
			transform.rotation = cameraLockTransform.rotation;
			instance.m_Player.enabled = false;
			instance.m_MainCamera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
			DisableScene(instance.currentScene);
		}
		else
		{
			Vector3 vector = instance.m_MainCamera.transform.position - instance.m_Player.transform.position;
			transform.position = instance.m_ScreenCamera.transform.position - vector;
			instance.m_ScreenCamera.GetComponent<OffsetCamera>().ToggleOffset();
			if (flag)
			{
				Transform parent = transform.parent;
				parent.rotation = Quaternion.identity;
				parent.parent = null;
				UnityEngine.Object.DontDestroyOnLoad(parent);
				transform.rotation = instance.m_RotationAtLock;
				transform.position = instance.m_PositionAtLock;
				instance.m_Player.enabled = true;
				EnableScene(instance.screenScene);
			}
			instance.m_MainCamera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
		}
		instance.m_CameraManager.FlythroughDirector = instance.screenScene.FlythroughDirector;
		instance.m_MainCamera.GetComponent<CinemachineBrain>().WorldUpOverride = instance.screenScene.WorldUpTransform;
		UniversalAdditionalCameraData component = instance.m_MainCamera.GetComponent<UniversalAdditionalCameraData>();
		component.renderPostProcessing = instance.screenScene.PostProcessingEnabled;
		component.SetRenderer(instance.screenScene.RendererIndex);
		instance.m_EneableCharacterInFollowingFrame = true;
		SceneManager.SetActiveScene(instance.screenScene.Scene);
		RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
		SceneTransitionManager sceneTransitionManager = instance;
		SceneTransitionManager sceneTransitionManager2 = instance;
		SceneMetaData sceneMetaData = instance.currentScene;
		SceneMetaData sceneMetaData2 = instance.screenScene;
		sceneTransitionManager.screenScene = sceneMetaData;
		sceneTransitionManager2.currentScene = sceneMetaData2;
		instance.SetTerminalLoaderAndDirector(instance.screenScene, isActive: false);
		instance.SetTerminalLoaderAndDirector(instance.currentScene, isActive: true);
	}

	private void UpdateCullingMasks()
	{
		if (instance.InTerminal)
		{
			instance.m_MainCamera.cullingMask |= instance.m_TerminalLayer;
			instance.m_ScreenCamera.cullingMask |= instance.locationLayer;
			instance.m_MainCamera.cullingMask ^= instance.locationLayer;
			instance.m_ScreenCamera.cullingMask ^= instance.m_TerminalLayer;
		}
		else
		{
			instance.m_MainCamera.cullingMask ^= instance.m_TerminalLayer;
			instance.m_ScreenCamera.cullingMask ^= instance.locationLayer;
			instance.m_MainCamera.cullingMask |= instance.locationLayer;
			instance.m_ScreenCamera.cullingMask |= instance.m_TerminalLayer;
		}
	}

	private void SetTerminalLoaderAndDirector(SceneMetaData scene, bool isActive)
	{
		if (scene.TerminalLoader != null)
		{
			scene.TerminalLoader.SetActive(isActive);
		}
		if (scene.SequenceDirector != null)
		{
			scene.SequenceDirector.time = 0.0;
			scene.SequenceDirector.enabled = isActive;
			if (isActive)
			{
				scene.SequenceDirector.Play();
			}
		}
	}

	private void OnEnable()
	{
		RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	public static void RegisterScene(string name, SceneMetaData metaData)
	{
		instance.registeredScenes.Add(name, metaData);
		if (instance.currentScene == null)
		{
			instance.currentScene = metaData;
		}
	}

	public static void NotifySceneLoading()
	{
		instance.m_ScenesLoading++;
		instance.m_Player.GetComponent<FirstPersonController>().enabled = false;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		instance.m_ScenesLoading--;
		if (instance.m_ScenesLoading == 0)
		{
			instance.m_Player.GetComponent<FirstPersonController>().enabled = true;
		}
	}

	public static void EnableScene(SceneLoad sceneLoader)
	{
		if (IsAvailable())
		{
			SceneMetaData sceneMetaData = instance.registeredScenes[sceneLoader.SceneName];
			if (sceneMetaData == null)
			{
				throw new Exception("Trying to enable unregistered scene");
			}
			Debug.Log("Enabling this scene: " + sceneMetaData.Scene.name);
			instance.m_Loader = sceneLoader;
			instance.m_InitialSceneLoad = false;
			instance.screenScene = sceneMetaData;
			instance.currentScene.TerminalLoader = sceneLoader.ControllPanel;
			LightProbes.TetrahedralizeAsync();
			sceneMetaData.Root.SetActive(value: true);
			if (sceneMetaData.SequenceDirector != null)
			{
				sceneMetaData.SequenceDirector.time = 0.0;
				sceneMetaData.SequenceDirector.Play();
			}
			if (sceneMetaData.SpawnTransform != null)
			{
				instance.m_ScreenCamera.GetComponent<OffsetCamera>().SetOffset(sceneMetaData.SpawnTransform.position - instance.m_Loader.ReferencePoint.position);
			}
			if (sceneLoader.screen != null)
			{
				sceneLoader.screen.TurnScreenOn();
			}
			int renderer = ((sceneMetaData.RendererIndex <= 1) ? 1 : sceneMetaData.RendererIndex);
			instance.m_ScreenCamera.GetComponent<UniversalAdditionalCameraData>().SetRenderer(renderer);
			instance.m_ScreenCamera.GetComponent<Camera>().enabled = true;
			instance.m_ScreenOff = false;
		}
	}

	public static void EnableScene(SceneMetaData sceneMetaData)
	{
		sceneMetaData.Root.SetActive(value: true);
		instance.m_ScreenCamera.GetComponent<Camera>().enabled = true;
	}

	public static void DisableScene(SceneLoad sceneLoader)
	{
		if (!IsAvailable())
		{
			return;
		}
		SceneMetaData sceneMetaData = instance.registeredScenes[sceneLoader.SceneName];
		if (sceneMetaData == instance.currentScene)
		{
			Debug.Log("Trying to disable current scene");
			return;
		}
		Debug.Log("Disabling this scene: " + sceneMetaData.Scene.name);
		LightProbes.TetrahedralizeAsync();
		if (sceneLoader.screen != null)
		{
			sceneLoader.screen.TurnScreenOff(delegate
			{
				if (instance.m_ScreenOff)
				{
					sceneMetaData.Root.SetActive(value: false);
					instance.m_ScreenCamera.GetComponent<Camera>().enabled = false;
				}
			});
		}
		instance.m_ScreenOff = true;
	}

	public static void DisableScene(SceneMetaData sceneMetaData)
	{
		sceneMetaData.Root.SetActive(value: false);
		instance.m_ScreenCamera.GetComponent<Camera>().enabled = false;
	}

	public static void DisableLoadedScene()
	{
		if (!(instance.m_Loader == null))
		{
			DisableScene(instance.m_Loader);
		}
	}

	public static void StartTransition()
	{
		instance.InTransition = true;
	}

	public static void StartTransition(MediaSceneLoader mediaSceneLoader)
	{
		if (IsAvailable())
		{
			instance.m_MediaSceneLoader = mediaSceneLoader;
			if (!instance.InTerminal)
			{
				instance.m_Loader = instance.currentScene.TerminalLoader.GetComponentInChildren<SceneLoad>();
			}
			StartTransition();
		}
	}

	public static void StopTransition()
	{
		instance.InTransition = false;
	}

	public static bool DissolveNeeded()
	{
		if (instance != null)
		{
			return instance.ElapsedTimeInTransition > 0.001f;
		}
		return false;
	}

	public static bool IsLoaded(string sceneName)
	{
		return instance.registeredScenes.ContainsKey(sceneName);
	}

	public static RenderTexture GetScreenRT()
	{
		return instance.m_ScreenCamera.activeTexture;
	}

	public static bool HasNotLoadedScene()
	{
		return instance.m_InitialSceneLoad;
	}

	public static bool IsAvailable()
	{
		return instance != null;
	}

	public static GameObject GetMainCamera()
	{
		return instance.m_MainCamera.gameObject;
	}

	public static SceneMetaData GetCurrentSceneData()
	{
		return instance.currentScene;
	}

	public static bool IsInTerminal()
	{
		return instance.InTerminal;
	}
}
