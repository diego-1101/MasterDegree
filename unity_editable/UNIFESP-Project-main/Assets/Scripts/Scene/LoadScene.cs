using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    private void Awake()
    {
        ReleaseCursor();
    }

    private void OnEnable()
    {
        ReleaseCursor();
    }

    async public void NextScene(int group)
    {
        ReleaseCursor();
        SceneLoader loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
        {
            await loader.LoadSceneGroup(group);
            return;
        }

        Debug.LogWarning("SceneLoader not found. Loading experiment scenes directly.");
        StartCoroutine(LoadSceneGroupFallback(group));
    }

    private System.Collections.IEnumerator LoadSceneGroupFallback(int group)
    {
        if (group == 0)
        {
            yield return SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Single);
            yield break;
        }

        string experimentScene = group == 1 ? "Experimento Um" : "Experimento Dois";
        string uiScene = group == 1 ? "Game UI" : "Game UI 2";

        yield return SceneManager.LoadSceneAsync(experimentScene, LoadSceneMode.Single);
        yield return SceneManager.LoadSceneAsync(uiScene, LoadSceneMode.Additive);
        ReleaseCursor();
    }

    private void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
