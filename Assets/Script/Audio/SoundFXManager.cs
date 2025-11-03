using UnityEngine;
using System.Collections;


public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;
    [SerializeField] private AudioSource soundFXObject;
    private AudioSource loopingSource;
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
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();
        float clipLength = audioSource.clip.length;
        Destroy(audioSource.gameObject, clipLength);
    }

    //for simple loops
    public void PlayLoopingSound(AudioClip clip, Transform spawnTransform, float volume)
    {
        if (loopingSource != null)
        {
            if (loopingSource.isPlaying && loopingSource.clip == clip)
                return;

            StopLoopingSound();
        }

        loopingSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        loopingSource.transform.SetParent(spawnTransform); 
        loopingSource.clip = clip;
        loopingSource.volume = volume;
        loopingSource.loop = true;
        loopingSource.playOnAwake = false;
        loopingSource.PlayScheduled(AudioSettings.dspTime + 0.05f);
    }
    
    public bool IsLoopingSoundActive()
    {
        return loopingSource != null && loopingSource.isPlaying;
    }

    public void StopLoopingSound()
    {
        if (crossfadeCoroutine != null)
        {
            StopCoroutine(crossfadeCoroutine);
            crossfadeCoroutine = null;
        }

        if (loopingSource != null)
        {
            if (loopingSource.isPlaying)
                loopingSource.Stop();

            Destroy(loopingSource.gameObject);
            loopingSource = null;
        }
    }
    public IEnumerator FadeInLoop(AudioClip clip, Transform spawnTransform, float targetVolume, float fadeTime = 0.5f)
    {
        // Create a new looping source
        PlayLoopingSound(clip, spawnTransform, 0f); // start muted

        if (loopingSource == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            loopingSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeTime);
            yield return null;
        }

        loopingSource.volume = targetVolume;
    }

    public IEnumerator FadeOutAndStopLoop(float duration = 0.5f)
    {
        if (loopingSource == null) yield break;

        float startVol = loopingSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            loopingSource.volume = Mathf.Lerp(startVol, 0, time / duration);
            yield return null;
        }

        StopLoopingSound();
    }
    public IEnumerator CrossfadeLoop(AudioClip clip, Transform spawnTransform, float volume, float crossfadeTime = 0.1f)
    {
        double nextStart = AudioSettings.dspTime + 0.1;
        AudioSource a = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        a.clip = clip;
        a.loop = false;
        a.volume = volume;
        a.transform.SetParent(spawnTransform);
        a.PlayScheduled(nextStart);

        while (true)
        {
            nextStart += clip.length - crossfadeTime;

            AudioSource b = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
            b.clip = clip;
            b.volume = 0f;
            b.loop = false;
            b.transform.SetParent(spawnTransform);

            b.PlayScheduled(nextStart);

            StartCoroutine(FadeVolume(b, 0f, volume, crossfadeTime));
            StartCoroutine(FadeVolume(a, volume, 0f, crossfadeTime));

            yield return new WaitForSeconds((float)(clip.length - crossfadeTime));

            Destroy(a.gameObject);
            a = b;
        }
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
        if (loopingSource != null && loopingSource.isPlaying)
            loopingSource.Pause();

        // Optionally, pause all one-shot sounds too:
        foreach (var src in FindObjectsOfType<AudioSource>())
        {
            if (src != loopingSource && src.isPlaying)
                src.Pause();
        }
    }

    public void ResumeAllSounds()
    {
        if (loopingSource != null)
            loopingSource.UnPause();

        foreach (var src in FindObjectsOfType<AudioSource>())
        {
            if (src != loopingSource)
                src.UnPause();
        }
    }
}
