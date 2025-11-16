using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadNextScene : MonoBehaviour
{
    public float minDisplayTime = 3f;

    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        float startTime = Time.time;

        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneToLoad.nextScene);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float elapsed = Time.time - startTime;

            if (operation.progress >= 0.9f && elapsed >= minDisplayTime)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}

public static class SceneToLoad
{
    public static string nextScene;
}
