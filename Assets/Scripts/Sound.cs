using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sound
{
    private AudioSource source;
    private string SoundLabel;
    private float volume;
    private float pitch;

    public Sound(AudioSource source, string clipName, float volume, float pitch = 1f)
    {
        this.source = source;
        SoundLabel = clipName;
        SetVolume(volume);
        SetPitch(pitch);
        source.clip = Resources.Load<AudioClip>(clipName);
    }

    public void Play()
    {
        source.Play();
    }

    public void Stop()
    {
        source.Stop();
    }

    public void SetPitch(float pitch)
    {
        this.pitch = pitch;
        source.pitch = pitch;
    }
    public void SetVolume(float volume)
    {
        this.volume = volume;
        source.volume = volume;
    }

}
