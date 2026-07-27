using Benchmarking;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class SceneMetaData : MonoBehaviour
{
	public Light mainLight;

	public Material skybox;

	public Cubemap reflection;

	[Tooltip("This is used if the scene loaded needs the player to be locked in place")]
	public Transform CameraLockTransform;

	public Transform WorldUpTransform;

	public Transform SpawnTransform;

	public PlayableDirector FlythroughDirector;

	public PlayableDirector SequenceDirector;

	public GameObject Root;

	public Scene Scene;

	public GameObject TerminalLoader;

	public bool FogEnabled;

	public bool PostProcessingEnabled;

	public bool StartActive;

	public int RendererIndex;

	private void Start()
	{
		if (SceneTransitionManager.IsAvailable() && !PerformanceTest.RunningBenchmark)
		{
			SetUp();
		}
	}

	private void SetUp()
	{
		Scene = base.gameObject.scene;
		GameObject[] rootGameObjects = Scene.GetRootGameObjects();
		foreach (GameObject gameObject in rootGameObjects)
		{
			if (gameObject != base.gameObject && (!(gameObject == Root) || !StartActive))
			{
				gameObject.SetActive(value: false);
			}
		}
		SceneTransitionManager.RegisterScene(Scene.name, this);
	}
}
