using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;
    [SerializeField] private AudioSource soundFXObject;
    private Dictionary<Transform, AudioSource> activeLoops = new Dictionary<Transform, AudioSource>();

    private Coroutine crossfadeCoroutine;
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        if (audioClip == null || soundFXObject == null) return;

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.loop = false;
        audioSource.Play();

        Destroy(audioSource.gameObject, audioClip.length);
    }


    //for simple loops
    public void PlayLoopingSound(AudioClip clip, Transform spawnTransform, float volume)
    {
        if (clip == null || spawnTransform == null) return;

        // Stop if same clip already looping for this transform
        if (activeLoops.TryGetValue(spawnTransform, out AudioSource existing))
        {
            if (existing != null && existing.isPlaying && existing.clip == clip)
                return;

            StopLoopingSound(spawnTransform);
        }

        AudioSource src = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        src.clip = clip;
        src.volume = volume;
        src.loop = true;
        src.playOnAwake = false;
        src.transform.SetParent(spawnTransform);
        src.Play();

        activeLoops[spawnTransform] = src;
    }
        

    public void StopLoopingSound(Transform spawnTransform)
    {
        if (spawnTransform == null) return;

        if (activeLoops.TryGetValue(spawnTransform, out AudioSource src))
        {
            if (src != null)
            {
                src.Stop();
                Destroy(src.gameObject);
            }
            activeLoops.Remove(spawnTransform);
        }
    }
    public IEnumerator FadeInLoop(AudioClip clip, Transform spawnTransform, float targetVolume, float fadeTime = 0.5f)
    {
        PlayLoopingSound(clip, spawnTransform, 0f);

        if (!activeLoops.TryGetValue(spawnTransform, out AudioSource src) || src == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            src.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeTime);
            yield return null;
        }

        src.volume = targetVolume;
    }
    public IEnumerator FadeOutAndStopLoop(Transform spawnTransform, float duration = 0.5f)
    {
        if (!activeLoops.TryGetValue(spawnTransform, out AudioSource src) || src == null) yield break;

        float startVol = src.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            if (src != null)
                src.volume = Mathf.Lerp(startVol, 0, time / duration);
            yield return null;
        }

        StopLoopingSound(spawnTransform);
    }
    private IEnumerator FadeVolume(AudioSource src, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
    }
    public void PauseAllSounds()
    {
        // Pause active loops
        foreach (var kvp in activeLoops)
        {
            if (kvp.Value != null && kvp.Value.isPlaying)
                kvp.Value.Pause();
        }

        // Optionally pause all other AudioSources in the scene
        foreach (var src in FindObjectsOfType<AudioSource>())
        {
            if (!activeLoops.ContainsValue(src) && src.isPlaying)
                src.Pause();
        }
    }

    public void ResumeAllSounds()
    {
        // Resume loops
        foreach (var kvp in activeLoops)
        {
            if (kvp.Value != null)
                kvp.Value.UnPause();
        }

        // Resume one-shots
        foreach (var src in FindObjectsOfType<AudioSource>())
        {
            if (!activeLoops.ContainsValue(src))
                src.UnPause();
        }
    }
    void LateUpdate()
    {
        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in activeLoops)
        {
            if (kvp.Key == null || kvp.Value == null)
                toRemove.Add(kvp.Key);
        }

        foreach (var t in toRemove)
            activeLoops.Remove(t);
    }
    public bool HasActiveLoop(Transform t)
    {
        if (t == null) return false;
        if (activeLoops.TryGetValue(t, out AudioSource src))
        {
            return src != null && src.isPlaying;
        }
        return false;
    }
}
