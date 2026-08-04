using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static async void Init()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Bootstrapper...");
        await SceneManager.LoadSceneAsync("LoadingScene", LoadSceneMode.Single);
    }
}
