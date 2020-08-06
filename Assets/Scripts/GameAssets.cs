using System.Collections.Generic;
using UnityEngine;

static public class GameAssets
{

    private static readonly GameObject GO;

    private static Dictionary<string, Sound> Sounds;
    private static Dictionary<string, Mesh> Meshes;
    private static Dictionary<string, UnityEngine.Material> Materials;

    static GameAssets()
    {
        GO = new GameObject("GameAssetObject");
        Object.DontDestroyOnLoad(GO);
        LoadSounds();
        LoadMaterials();
    }

    private static void LoadSounds()
    {
        Sounds = new Dictionary<string, Sound>
        {
            ["Food"] = new Sound(GO.AddComponent<AudioSource>(), "FoodGet1", 0.5f),
            ["PlaySound"] = new Sound(GO.AddComponent<AudioSource>(), "PlaySound", 0.5f),
            ["SnekDance"] = new Sound(GO.AddComponent<AudioSource>(), "SnekDance", 0.1f, true),
            ["MenuMusic"] = new Sound(GO.AddComponent<AudioSource>(), "MenuMusic", 0.3f, true)
        };
    }


    public static Sound GetSound(string _SoundName)
    {
        if (!Sounds.TryGetValue(_SoundName, out Sound SoundTemp)) Debug.LogError("Sound was not found: " + _SoundName);
        return SoundTemp;
    }

    private static void LoadMaterials()
    {
        Materials = new Dictionary<string, UnityEngine.Material>
        {
            ["Arena Grid"] = Resources.Load<UnityEngine.Material>("Arena Grid"),
            ["Player"] = Resources.Load<UnityEngine.Material>("Player"),
            ["Food"] = Resources.Load<UnityEngine.Material>("Food"),
            ["Line"] = Resources.Load<UnityEngine.Material>("Line"),
            ["Tier1"] = Resources.Load<UnityEngine.Material>("Tier1"),
            ["Tier2"] = Resources.Load<UnityEngine.Material>("Tier2"),
            ["Tier3"] = Resources.Load<UnityEngine.Material>("Tier3"),
            ["Tier4"] = Resources.Load<UnityEngine.Material>("Tier4"),
            ["Enemy Outline"] = Resources.Load<UnityEngine.Material>("Enemy Outline"),
            ["Wall"] = Resources.Load<UnityEngine.Material>("Wall")
        };
    }

    private static UnityEngine.Material GetMaterial(string _MaterialName)
    {
        if (!Materials.TryGetValue(_MaterialName, out UnityEngine.Material MaterialTemp)) Debug.LogError("Material was not found: " + _MaterialName);
        return MaterialTemp;
    }

    public static UnityEngine.Material GetTierMaterial(int Tier)
    {

        switch (Tier) {
            case 1:
                return Material.Tier1;
            case 2:
                return Material.Tier2;
            case 3:
                return Material.Tier3;
            case 4:
                return Material.Tier4;
            default:
                Debug.Log("Invalid Tier Passed (Defaulted to 1). Tier: " + Tier);
                return Material.Tier1;
        }

    }

    public static class Material
    {
        public static UnityEngine.Material ArenaGrid { get => GetMaterial("Arena Grid"); }
        public static UnityEngine.Material Player { get => GetMaterial("Player"); }
        public static UnityEngine.Material Food { get => GetMaterial("Food"); }
        public static UnityEngine.Material Line { get => GetMaterial("Line"); }
        public static UnityEngine.Material Tier1 { get => GetMaterial("Tier1"); }
        public static UnityEngine.Material Tier2 { get => GetMaterial("Tier2"); }
        public static UnityEngine.Material Tier3 { get => GetMaterial("Tier3"); }
        public static UnityEngine.Material Tier4 { get => GetMaterial("Tier4"); }
        public static UnityEngine.Material EnemyOutline { get => GetMaterial("Enemy Outline"); }
        public static UnityEngine.Material Wall { get => GetMaterial("Wall"); }
    }

    public class Sound
    {

        // EXPOSE SOUNDS FOR STRONG TYPING
        public static Sound Food { get => GetSound("Food"); }
        public static Sound SnekDance { get => GetSound("SnekDance"); }

        public static Sound PlaySound { get => GetSound("PlaySound"); }

        public static Sound MenuMusic { get => GetSound("MenuMusic"); }

        private AudioSource Source;
        public string ClipName { get; private set; }
        public float Volume
        {
            get { return Source.volume; }
            set { Source.volume = value; }
        }
        public float Pitch
        {
            get { return Source.pitch; }
            set { Source.pitch = value; }
        }

        public bool Loop
        {
            get { return Source.loop; }
            set { Source.loop = value; }
        }



        public Sound(AudioSource Source, string ClipName, float Volume, bool Loop = false, float Pitch = 1.00f)
        {
            this.Source = Source;
            this.ClipName = ClipName;
            this.Volume = Volume;
            this.Pitch = Pitch;
            this.Loop = Loop;
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

}