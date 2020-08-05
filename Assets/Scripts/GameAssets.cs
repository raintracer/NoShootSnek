using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

static public class GameAssets
{

    private static readonly GameObject GO;

    private static readonly Dictionary<string, Sound> Sounds;
    private static readonly Dictionary<string, Mesh> Meshes;
    private static readonly Dictionary<string, Material> Materials;

    static GameAssets()
    {
        GO = new GameObject("GameAssetObject");
        Object.DontDestroyOnLoad(GO);

        Sounds = new Dictionary<string, Sound>
        {
            [SoundName.Food.Value] = new Sound(GO.AddComponent<AudioSource>(), "FoodGet1", 0.5f),
            [SoundName.SnekDance.Value] = new Sound(GO.AddComponent<AudioSource>(), "SnekDance", 0.1f)
        };

        Sounds = new Dictionary<string, Sound>
        {
            [SoundName.Food.Value] = new Sound(GO.AddComponent<AudioSource>(), "FoodGet1", 0.5f),
            [SoundName.SnekDance.Value] = new Sound(GO.AddComponent<AudioSource>(), "SnekDance", 0.1f)
        };

    }

    public static Sound GetSound(SoundName _SoundName)
    {
        if (!Sounds.TryGetValue(_SoundName.Value, out Sound SoundTemp)) Debug.LogError("Sound was not found: " + _SoundName.Value);
        return SoundTemp;
    }

}

public class SoundName
{
    public string Value;
    private SoundName(string Value) { this.Value = Value; }

    public static SoundName Food { get => new SoundName("Food"); }
    public static SoundName SnekDance { get => new SoundName("SnekDance"); }

}

public class Sound
{
    private AudioSource Source;
    public string ClipName { get; private set; }
    public float Volume {
        get { return Source.volume; }
        set { Source.volume = value; } 
    }
    public float Pitch
    {
        get { return Source.pitch; }
        set { Source.pitch = value; }
    }

    public Sound(AudioSource Source, string ClipName, float Volume, float Pitch = 1.00f)
    {
        this.Source = Source;
        this.ClipName = ClipName;
        this.Volume = Volume;
        this.Pitch = Pitch;
        this.Source.clip = Resources.Load<AudioClip>(ClipName);
    }

    public void Play()
    {
        Source.Play();
    }

    public void Stop()
    {
        Source.Stop();
    }

}