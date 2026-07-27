using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
	[Header("Loading UI")]
	[SerializeField]
	private Image loadingBar;

	[SerializeField]
	private float fillSmoothness = 2f;

	[SerializeField]
	private float minFillDuration = 1f;

	[SerializeField]
	private Canvas loadingCanvas;

	[SerializeField]
	private Camera loadingCamera;

	[Space]
	[Header("Scene Groups")]
	[SerializeField]
	private SceneGroup[] sceneGroups;

	private float targetProgress;

	private float currentProgress;

	private bool isLoading;

	private bool loadingComplete;

	private float loadingStartTime;

	public readonly SceneGroupManager manager = new SceneGroupManager();

	private async void Start()
	{
		await LoadSceneGroup(0);
	}

	private void Update()
	{
		if (isLoading)
		{
			currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, fillSmoothness * Time.deltaTime);
			loadingBar.fillAmount = currentProgress;
			if (loadingComplete && Mathf.Approximately(currentProgress, 1f))
			{
				FinishLoading();
			}
		}
	}

	public async Task LoadSceneGroup(int index)
	{
		loadingBar.fillAmount = 0f;
		currentProgress = 0f;
		targetProgress = 0f;
		loadingComplete = false;
		loadingStartTime = Time.time;
		if (index < 0 || index >= sceneGroups.Length)
		{
			Debug.LogError($"Invalid scene group index: {index}");
			return;
		}
		EnableLoadingCanvas(enable: true);
		LoadingProgress loadingProgress = new LoadingProgress();
		loadingProgress.progressed += UpdateTargetProgress;
		try
		{
			await manager.LoadScenes(sceneGroups[index], loadingProgress);
			loadingComplete = true;
			float num = Time.time - loadingStartTime;
			if (num < minFillDuration)
			{
				await Task.Delay((int)((minFillDuration - num) * 1000f));
			}
			targetProgress = 1f;
		}
		catch (Exception ex)
		{
			Debug.LogError("Error loading scene group: " + ex.Message);
			FinishLoading();
		}
	}

	private void EnableLoadingCanvas(bool enable)
	{
		isLoading = enable;
		loadingCanvas.gameObject.SetActive(enable);
		loadingCamera.gameObject.SetActive(enable);
	}

	private void FinishLoading()
	{
		EnableLoadingCanvas(enable: false);
		loadingComplete = false;
	}

	private void UpdateTargetProgress(float progress)
	{
		targetProgress = Mathf.Clamp01(progress);
		if (targetProgress < currentProgress)
		{
			currentProgress = targetProgress;
		}
	}
}
