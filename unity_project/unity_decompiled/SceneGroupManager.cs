using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGroupManager
{
	private SceneGroup activeSceneGroup;

	public event Action<string> OnSceneLoaded = delegate
	{
	};

	public event Action<string> OnSceneUnloaded = delegate
	{
	};

	public event Action OnSceneGroupLoaded = delegate
	{
	};

	public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false)
	{
		activeSceneGroup = group;
		List<string> loadedScenes = new List<string>();
		await UnloadScenes();
		int sceneCount = SceneManager.sceneCount;
		for (int i = 0; i < sceneCount; i++)
		{
			loadedScenes.Add(SceneManager.GetSceneAt(i).name);
		}
		int count = activeSceneGroup.data.Count;
		AsyncOperationGroup operationGroup = new AsyncOperationGroup(count);
		for (int j = 0; j < count; j++)
		{
			SceneData sceneData = group.data[j];
			if (reloadDupScenes || !loadedScenes.Contains(sceneData._name))
			{
				AsyncOperation item = SceneManager.LoadSceneAsync(sceneData.sceneReference.Path, LoadSceneMode.Additive);
				operationGroup.operations.Add(item);
				this.OnSceneLoaded(sceneData._name);
			}
		}
		while (!operationGroup.isDone)
		{
			progress?.Report(operationGroup.progress);
			await Task.Delay(100);
		}
		Scene sceneByName = SceneManager.GetSceneByName(activeSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
		if (sceneByName.IsValid())
		{
			SceneManager.SetActiveScene(sceneByName);
		}
		this.OnSceneGroupLoaded();
	}

	public async Task UnloadScenes()
	{
		List<string> list = new List<string>();
		string name = SceneManager.GetActiveScene().name;
		for (int num = SceneManager.sceneCount - 1; num > 0; num--)
		{
			Scene sceneAt = SceneManager.GetSceneAt(num);
			if (sceneAt.isLoaded)
			{
				string name2 = sceneAt.name;
				if (!name2.Equals(name) && !name2.Equals("Bootstrapper"))
				{
					list.Add(name2);
				}
			}
		}
		AsyncOperationGroup operationGroup = new AsyncOperationGroup(list.Count);
		foreach (string item in list)
		{
			AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(item);
			if (asyncOperation != null)
			{
				operationGroup.operations.Add(asyncOperation);
				this.OnSceneUnloaded(item);
			}
		}
		while (!operationGroup.isDone)
		{
			await Task.Delay(100);
		}
		await Resources.UnloadUnusedAssets();
	}
}
