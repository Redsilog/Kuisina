using UnityEngine;
using UnityEngine.SceneManagement;

public class music : MonoBehaviour
{
    private static music instance;
    private AudioSource audioSource;

    public AudioClip menuMusic;
    public AudioClip level1Music;
    public AudioClip level2Music;
    public AudioClip level3Music;
    public AudioClip level4Music;
    public AudioClip level5Music;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();

            PlayMusicForScene(SceneManager.GetActiveScene().name);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main Menu")
        {
            PlayMusicForScene("Main Menu");
        }
        else
        {
            string[] levelScenes = {"Main Level 1", "Main Level 2", "Main Level 3", "Main Level 4", "Main Level 5" };

            if (System.Array.Exists(levelScenes, level => level == scene.name))
            {
                PlayMusicForScene(scene.name);
            }
        }
    }

    private void PlayMusicForScene(string sceneName)
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        switch (sceneName)
        {
            case "Main Menu":
                audioSource.clip = menuMusic;
                break;
            case "Main Level 1":
                audioSource.clip = level1Music;
                break;
            case "Main Level 2":
                audioSource.clip = level2Music;
                break;
            case "Main Level 3":
                audioSource.clip = level3Music;
                break;
            case "Main Level 4":
                audioSource.clip = level4Music;
                break;
            case "Main Level 5":
                audioSource.clip = level5Music;
                break;
            default:
                Debug.LogWarning("No music set for this scene: " + sceneName);
                return;
        }

        audioSource.Play();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}