using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    public enum VolumeType { Master, Music, SoundFX }
    public VolumeType volumeType;

    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();

        // Load the correct saved value
        float savedValue = 1f;
        switch (volumeType)
        {
            case VolumeType.Master:
                savedValue = PlayerPrefs.GetFloat("MasterVolume", 1f);
                break;
            case VolumeType.Music:
                savedValue = PlayerPrefs.GetFloat("MusicVolume", 1f);
                break;
            case VolumeType.SoundFX:
                savedValue = PlayerPrefs.GetFloat("SoundFXVolume", 1f);
                break;
        }

        slider.SetValueWithoutNotify(savedValue); // ✅ prevents OnValueChanged loop
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (SoundMixerManager.instance == null) return;

        switch (volumeType)
        {
            case VolumeType.Master:
                SoundMixerManager.instance.SetMasterVolume(value);
                break;
            case VolumeType.Music:
                SoundMixerManager.instance.SetMusicVolume(value);
                break;
            case VolumeType.SoundFX:
                SoundMixerManager.instance.SetSoundFXVolume(value);
                break;
        }
    }

    private void OnEnable()
    {
        // Refresh the slider whenever the scene loads
        if (slider == null) return;
        Start(); // safely reloads values
    }
}
