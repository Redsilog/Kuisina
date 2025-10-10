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
        if (loopingSource != null) return;
        loopingSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        loopingSource.clip = clip;
        loopingSource.volume = volume;
        loopingSource.loop = true;
        loopingSource.Play();
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
            loopingSource.Stop();
            Destroy(loopingSource.gameObject);
            loopingSource = null;
        }

        foreach (var source in FindObjectsOfType<AudioSource>())
        {
            if (source != null && source.gameObject.name.Contains("[Loop]"))
            {
                Destroy(source.gameObject);
            }
        }
    }

    //for stuff that needs to be repeated over the clip length
    public void PlayLoopWithCrossfade(AudioClip clip, Transform spawnTransform, float volume, float fadeTime)
    {
        if (crossfadeCoroutine != null) return;
        crossfadeCoroutine = StartCoroutine(LoopWithCrossfade(clip, spawnTransform, volume, fadeTime));
    }

    private IEnumerator LoopWithCrossfade(AudioClip clip, Transform spawnTransform, float volume, float fadeTime)
    {
        while (true)
        {
            AudioSource a = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
            a.gameObject.name = soundFXObject.name + " [Loop]";
            a.clip = clip;
            a.volume = volume;
            a.Play();

            yield return new WaitForSeconds(clip.length - fadeTime);

            AudioSource b = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
            b.gameObject.name = soundFXObject.name + " [Loop]";
            b.clip = clip;
            b.volume = 0;
            b.Play();

            // crossfade
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                b.volume = Mathf.Lerp(0, volume, t / fadeTime);
                a.volume = Mathf.Lerp(volume, 0, t / fadeTime);
                yield return null;
            }

            Destroy(a.gameObject);
        }
    }
}
