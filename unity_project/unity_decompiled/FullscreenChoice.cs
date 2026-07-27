using UnityEngine;

public class FullscreenChoice : MonoBehaviour
{
	private void Awake()
	{
		if (Object.FindObjectsOfType<FullscreenChoice>().Length > 1)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			Object.DontDestroyOnLoad(this);
		}
	}

	private void Update()
	{
		if (ShouldToggleMaximize())
		{
			Screen.fullScreen = !Screen.fullScreen;
		}
	}

	private bool ShouldToggleMaximize()
	{
		return Input.GetKeyDown(KeyCode.F11);
	}
}
