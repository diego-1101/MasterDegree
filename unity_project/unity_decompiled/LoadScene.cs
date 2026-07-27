using UnityEngine;

public class LoadScene : MonoBehaviour
{
	public async void NextScene(int group)
	{
		await Object.FindAnyObjectByType<SceneLoader>().LoadSceneGroup(group);
	}
}
