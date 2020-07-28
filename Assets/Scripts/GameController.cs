using UnityEngine;
using System.Collections.ObjectModel;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{

    [SerializeField] public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    [HideInInspector] public static Dictionary<string, Mesh> Meshes { get; private set; } = new Dictionary<string, Mesh>();

    public readonly static bool AIMODE = false;
    private const bool RenderCollissionMapFlag = true;
    [SerializeField] private float TIME_SCALE = 1f; 

    static Vector2 Movement = Vector2.zero;
    static readonly float ArenaGameSize = 10f;
    static readonly int MAX_TILE_SIZE = 101;
    static readonly int CENTER_TILE_INDEX = (MAX_TILE_SIZE - 1) / 2;
    public static bool[,] CollisionMap { get; set; } = new bool[MAX_TILE_SIZE, MAX_TILE_SIZE];

    [HideInInspector] public static int TileDimensionInt { get; private set; }
    [HideInInspector] public static int TileIndexMax { get; private set; }
    [HideInInspector] public static int TileIndexMin { get; private set; }
    static float TileDimensionFloat;
    static Vector2 RenderOffset;
    static float GridSize;

    public static Collection<Vector2Int> Food { get; private set; } = new Collection<Vector2Int>();
    public static Dictionary<Vector2Int, int> Eggs { get; private set; } = new Dictionary<Vector2Int, int>();

    static PlayerInputMap Inputs;
    static Snake PlayerSnake;
    static List<Snake> EnemySnakes = new List<Snake>();

    static Dictionary<Vector2Int, Wobble> Wobbles = new Dictionary<Vector2Int, Wobble>();

    // Wobble Settings
    readonly static float TestWobbleDissipation = 1.05f;
    static public Sound Sound_Food { get; private set; }

    public struct Sound
    {
        public AudioSource source { get; private set; }
        readonly float volume;
        readonly float pitch;

        public Sound(AudioSource source, string clipName, float volume, float pitch)
        {
            this.source = source;
            this.volume = volume;
            this.pitch = pitch;
            source.clip = Resources.Load<AudioClip>(clipName);
        }

        public void Play()
        {
            source.Play();
        }
    }

    public static Vector2 GetMovement()
    {
        return Movement;
    }

    void DefineSounds()
    {
        Sound_Food = new Sound(gameObject.AddComponent<AudioSource>(), "FoodGet1", 0.5f, 0f);
    }

    void Awake()
    {
        LoadMaterials();
        DefineSounds();
        SetTileDimension(9f);

        PlayerSnake = new Snake(true, new Vector2Int(CENTER_TILE_INDEX, CENTER_TILE_INDEX), 0, 0);
        
        if (AIMODE)
        {
            AddEnemySnake(new Vector2Int(TileIndexMin, CENTER_TILE_INDEX), 1);
            AddEnemySnake(new Vector2Int(TileIndexMax, CENTER_TILE_INDEX), 1);
            AddEnemySnake(new Vector2Int(CENTER_TILE_INDEX, TileIndexMin), 1);
            AddEnemySnake(new Vector2Int(CENTER_TILE_INDEX, TileIndexMax), 1);
        }


        Inputs = new PlayerInputMap();
        Inputs.Enable();
        Inputs.Player.Move.performed += ctx => Movement = ctx.ReadValue<Vector2>();

        PlaceRandomFood();

    }

    static void SetTileDimension(float Amount)
    {
        TileDimensionFloat = Amount;
        TileDimensionInt = (int)TileDimensionFloat - ((int)TileDimensionFloat + 1) % 2;
        GridSize = ArenaGameSize / TileDimensionFloat;
        // Vector2 GridOffset = new Vector2(GridSize / 2, GridSize / 2);
        TileIndexMin = CENTER_TILE_INDEX - (TileDimensionInt - 1) / 2;
        TileIndexMax = CENTER_TILE_INDEX + (TileDimensionInt - 1) / 2;
        RenderOffset = new Vector2(-GridSize * CENTER_TILE_INDEX, -GridSize * CENTER_TILE_INDEX);
        DefineMeshes();
    }

    static public void GrowArena()
    {
        SetTileDimension(TileDimensionFloat += 18f / (TileDimensionFloat * TileDimensionFloat));
    }

    public void Update()
    {

        PlayerSnake.UpdateSnake();
        List<int> DeadSnakes = new List<int>();
        for (int i = 0; i < EnemySnakes.Count; i++)
        {
            if (EnemySnakes[i].Dead)
            {
                DeadSnakes.Add(i);
            }
            else
            {
                EnemySnakes[i].UpdateSnake();
            }
        }
        for (int i = DeadSnakes.Count - 1; i >= 0; i--)
        {
            EnemySnakes.RemoveAt(DeadSnakes[i]);
        }

    }

    public void FixedUpdate()
    {

        PlayerSnake.FixedUpdateSnake();
        for (int i = 0; i < EnemySnakes.Count; i++)
        {
            EnemySnakes[i].FixedUpdateSnake();
        }

        // UPDATE WOBBLES
        List<Vector2Int> WobblesToRemove = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, Wobble> Entry in Wobbles) { 
            if (!Entry.Value.UpdateWobble())
            {
                WobblesToRemove.Add(Entry.Key);
            }
        }

        foreach (Vector2Int Wobble in WobblesToRemove) Wobbles.Remove(Wobble);

        Time.timeScale = TIME_SCALE;

    }

    public void OnPostRender()
    {

        // RENDER OPEN SPOTS
        for (int i = TileIndexMin; i <= TileIndexMax; i++)
        {
            for (int j = TileIndexMin; j <= TileIndexMax; j++)
            {
                DrawMeshOnGrid(Meshes["ArenaSquareMesh"], Materials["Arena Grid"], new Vector2Int(i, j), Quaternion.identity);
            }
        }

        // RENDER SNAKES
        PlayerSnake.RenderSnake();
        for (int i = 0; i < EnemySnakes.Count; i++)
        {
            EnemySnakes[i].RenderSnake();
        }

        // RENDER THE FOOD
        foreach (Vector2Int FoodPosition in Food) {
            DrawMeshOnGrid(Meshes["EngorgedMesh"], Materials["Food"], FoodPosition, Quaternion.identity);
        }

        // RENDER THE EGGS
        if (Eggs.Count > 0)
        {
            foreach (KeyValuePair<Vector2Int, int> EggKeyPair in Eggs)
            {
                int Tier = EggKeyPair.Value;
                if (!Meshes.TryGetValue("EngorgedMesh", out Mesh mesh)) Debug.LogError("Missing mesh");
                if (!Materials.TryGetValue("Tier" + Tier, out Material mat)) Debug.LogError("Missing material: " + "Tier" + Tier);
                DrawMeshOnGrid(mesh, mat, EggKeyPair.Key, Quaternion.identity);
            }
        }

        // RENDER COLLISSION MAP
        if (RenderCollissionMapFlag)
        {

            for (int i = TileIndexMin; i <= TileIndexMax; i++)
            {
                for (int j = TileIndexMin; j <= TileIndexMax; j++)
                {
                    if (CollisionMap[j, i])
                    {
                        Vector2 GridCenterPosition = new Vector2(GridSize * i, GridSize * j) + RenderOffset;
                        Debug.DrawRay(GridCenterPosition + new Vector2(-GridSize / 2, GridSize / 2), Vector2.right, Color.red);
                        Debug.DrawRay(GridCenterPosition + new Vector2(GridSize / 2, GridSize / 2), Vector2.down, Color.red);
                        Debug.DrawRay(GridCenterPosition + new Vector2(GridSize / 2, -GridSize / 2), Vector2.left, Color.red);
                        Debug.DrawRay(GridCenterPosition + new Vector2(-GridSize / 2, -GridSize / 2), Vector2.up, Color.red);
                    }
                }
            }
        }

    }

    public static void PlaceEgg(Vector2Int Position, int Tier)
    {
        Eggs[Position] = Tier;
        CollisionMap[Position.y, Position.x] = true;
    }


    //
    //  MESH DEFINITIONS
    //

    static void DefineMeshes()
    {

        Meshes["ArenaSquareMesh"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(-GridSize / 10, GridSize / 10),
                new Vector3(GridSize / 10, GridSize / 10),
                new Vector3(GridSize / 10, -GridSize / 10),
                new Vector3(-GridSize / 10, -GridSize / 10)
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        Meshes["SquareMesh"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(-GridSize / 2.5f, GridSize / 2.5f),
                new Vector3(GridSize / 2.5f, GridSize / 2.5f),
                new Vector3(GridSize / 2.5f, -GridSize / 2.5f),
                new Vector3(-GridSize / 2.5f, -GridSize / 2.5f)
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        Meshes["SmallSquareMesh"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(-GridSize / 3, GridSize / 3),
                new Vector3(GridSize / 3, GridSize / 3),
                new Vector3(GridSize / 3, -GridSize / 3),
                new Vector3(-GridSize / 3, -GridSize / 3)
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        Meshes["TinySquareMesh"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(-GridSize / 4, GridSize / 4),
                new Vector3(GridSize / 4, GridSize / 4),
                new Vector3(GridSize / 4, -GridSize / 4),
                new Vector3(-GridSize / 4, -GridSize / 4)
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        Meshes["TinySquareMesh"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(-GridSize / 4, GridSize / 4),
                new Vector3(GridSize / 4, GridSize / 4),
                new Vector3(GridSize / 4, -GridSize / 4),
                new Vector3(-GridSize / 4, -GridSize / 4)
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        float SnakeLineMeshSize = 0.025f;
        Meshes["SnakeLineMesh"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(0, GridSize * SnakeLineMeshSize),
                new Vector3(GridSize, GridSize * SnakeLineMeshSize),
                new Vector3(GridSize, -GridSize * SnakeLineMeshSize),
                new Vector3(0 / 4, -GridSize * SnakeLineMeshSize)
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        Meshes["SnakeHeadMesh"] = new Mesh()
        {
            vertices = new Vector3[5]
            {
                new Vector3(-GridSize / 3, GridSize / 3),
                new Vector3(GridSize / 6, GridSize / 3),
                new Vector3(GridSize / 6, -GridSize / 3),
                new Vector3(-GridSize / 3, -GridSize / 3),
                new Vector3(GridSize / 3, 0)
            },
            triangles = new int[9] { 0, 1, 2, 0, 2, 3, 1, 4, 2 }
        };

        Meshes["EngorgedMesh"] = Meshes["SmallSquareMesh"];

    }

    public static void PlaceRandomFood()
    {

        Collection<Vector2Int> EligibleSpots = new Collection<Vector2Int>();
        for (int i = TileIndexMin; i <= TileIndexMax; i++)
        {
            for (int j = TileIndexMin; j <= TileIndexMax; j++)
            {
                if (!CollisionMap[j, i]) EligibleSpots.Add(new Vector2Int(i, j));
            }
        }

        Vector2Int SelectedSpot = EligibleSpots[UnityEngine.Random.Range(0, EligibleSpots.Count)];
        Food.Add(SelectedSpot);
        CollisionMap[SelectedSpot.y, SelectedSpot.x] = true;

    }

    
    public static Snake AddEnemySnake(Vector2Int Position, int Tier = 0, Snake.MoveDir Facing = Snake.MoveDir.Right)
    {
        EnemySnakes.Add(new Snake(false, new Vector2Int(Position.x, Position.y), 0, Tier,  Facing));
        return EnemySnakes[EnemySnakes.Count-1];
    }

    public static void AddExplosionToGrid(Vector2Int Position)
    {
        AddParticleEffectToGrid("SnakeExplosion", Position, 1f);
        AddWobbleForce(Position, 1f);
    }

    public static void AddParticleEffectToGrid(string ParticleName, Vector2Int ParticlePositionInt, float LifeTime)
    {
        Vector2 ParticlePositionFloat = new Vector2(ParticlePositionInt.x * GridSize, ParticlePositionInt.y * GridSize) + GetWobbleOffset(ParticlePositionInt) + RenderOffset;

        GameObject ParticleControllerObject = Instantiate(Resources.Load<GameObject>("ParticleController"));
        if (ParticleControllerObject == null)
        {
            Debug.LogError("Particle Object Resource Not Found.");
        }
        ParticleControllerObject.GetComponent<ParticleController>().StartParticle(ParticleName, ParticlePositionFloat, LifeTime);
    }

    //
    //  WOBBLE FUNCTIONS
    //

    public static void AddWobbleForce(Vector2Int ForcePosition, float Intensity, bool IsAttractive = false)
    {
        for (int i = TileIndexMin; i <= TileIndexMax; i++)
        {
            for (int j = TileIndexMin; j <= TileIndexMax; j++)
            {
                if (!(i == ForcePosition.x && j == ForcePosition.y))
                {
                    Vector2 TileVectorFloat = new Vector2(i, j);
                    Vector2Int TileVectorInt = new Vector2Int(i, j);
                    Vector2 TangentVector = (ForcePosition - TileVectorFloat) * (IsAttractive ? 1f : -1f);
                    float Distance = TangentVector.magnitude;
                    TangentVector.Normalize();
                    CreateWibble(TileVectorInt, TangentVector, Intensity / Mathf.Pow(Distance, TestWobbleDissipation));
                }
            }
        }
    }
    private static void CreateWibble(Vector2Int WibblePosition, Vector2 WibbleDirection, float WibbleIntensity)
    {
        Wobble wobble;
        while (!Wobbles.TryGetValue(WibblePosition, out wobble))
        {
            Wobbles.Add(WibblePosition, new Wobble());
        }

        wobble.AddWibble(WibbleDirection, WibbleIntensity);
    }

    public static Vector2 GetWobbleOffset(Vector2Int Position)
    {
        Wobble wobble; 
        if (Wobbles.TryGetValue(Position, out wobble))
        {
            return wobble.Offset;
        }
        else
        {
            return Vector2.zero;
        }
    }

    public static void DrawMeshOnGrid(Mesh mesh, Material mat, Vector2Int pos, Quaternion quat, bool AffectedByWobble = true)
    {
        if (mat.SetPass(0))
        {
            Graphics.DrawMeshNow(
                mesh,
                new Vector2(pos.x * GridSize, pos.y * GridSize) + (AffectedByWobble ? GetWobbleOffset(new Vector2Int(pos.x, pos.y)) : Vector2.zero) + RenderOffset,
                quat);
        }
    }

    public static Snake.MoveDir OppositeDirection(Snake.MoveDir Facing)
    {

        switch (Facing)
        {
            case Snake.MoveDir.Up:
                return Snake.MoveDir.Down;
            case Snake.MoveDir.Right:
                return Snake.MoveDir.Left;
            case Snake.MoveDir.Down:
                return Snake.MoveDir.Up;
            case Snake.MoveDir.Left:
                return Snake.MoveDir.Right;
        }

        Debug.Log("Unrecognized Direction.");
        return Snake.MoveDir.Right;
    }

    void LoadMaterials()
    {
        Materials["Arena Grid"] = Resources.Load<Material>("Arena Grid");
        Materials["Player"]     = Resources.Load<Material>("Player");
        Materials["Food"]       = Resources.Load<Material>("Food");
        Materials["Line"]       = Resources.Load<Material>("Line");
        Materials["Tier1"]      = Resources.Load<Material>("Tier1");
        Materials["Tier2"]      = Resources.Load<Material>("Tier2");
        Materials["Tier3"]      = Resources.Load<Material>("Tier3");
        Materials["Tier4"]      = Resources.Load<Material>("Tier4");
    }
}
