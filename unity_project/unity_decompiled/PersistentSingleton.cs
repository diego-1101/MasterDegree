using UnityEngine;

public class PersistentSingleton<T> : MonoBehaviour where T : Component
{
	public bool AutoUnparentOnAwake = true;

	protected static T instance;

	public static bool HasInstance => instance != null;

	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Object.FindAnyObjectByType<T>();
				if (instance == null)
				{
					instance = new GameObject(typeof(T).Name + " Auto-Generated").AddComponent<T>();
				}
			}
			return instance;
		}
	}

	public static T TryGetInstance()
	{
		if (!HasInstance)
		{
			return null;
		}
		return instance;
	}

	protected virtual void Awake()
	{
		InitializeSingleton();
	}

	protected virtual void InitializeSingleton()
	{
		if (Application.isPlaying)
		{
			if (AutoUnparentOnAwake)
			{
				base.transform.SetParent(null);
			}
			if (instance == null)
			{
				instance = this as T;
				Object.DontDestroyOnLoad(base.gameObject);
			}
			else if (instance != this)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
