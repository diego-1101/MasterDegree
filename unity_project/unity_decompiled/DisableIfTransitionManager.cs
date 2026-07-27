using UnityEngine;

public class DisableIfTransitionManager : MonoBehaviour
{
	private void Start()
	{
		if (SceneTransitionManager.IsAvailable())
		{
			Object.Destroy(base.gameObject);
		}
	}
}
