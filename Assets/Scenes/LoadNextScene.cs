using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadNextScene : MonoBehaviour
{
    [SerializeField] private GameObject ls1;
    [SerializeField] private GameObject ls2;
    [SerializeField] private GameObject ls3;

    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        ls1.SetActive(false);
        ls2.SetActive(false);
        ls3.SetActive(false);

        float startTime = Time.time;

        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneToLoad.nextScene);
        operation.allowSceneActivation = false;

        ls1.SetActive(true);
        yield return new WaitForSeconds(1f);
        ls1.SetActive(false);

        ls2.SetActive(true);
        yield return new WaitForSeconds(1f);
        ls2.SetActive(false);

        ls3.SetActive(true);
        yield return new WaitForSeconds(2f);

        float elapsed = Time.time - startTime;
        float minDisplayTime = 4f; 
        if (elapsed < minDisplayTime)
        {
            yield return new WaitForSeconds(minDisplayTime - elapsed);
        }

        // Activate the scene
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }
    }
}

public static class SceneToLoad
{
    public static string nextScene;
}
