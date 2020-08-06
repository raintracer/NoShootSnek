using UnityEngine;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditorInternal;

public class GameController : MonoBehaviour
{
    [HideInInspector] public static Dictionary<string, Mesh> Meshes { get; private set; } = new Dictionary<string, Mesh>();

    [HideInInspector] public readonly static bool AIMODE = false;
    private const bool RenderCollissionMapFlag = true;
    [SerializeField] private float TIME_SCALE = 1f;

    public static Vector2 Movement = Vector2.zero;
    readonly static float ArenaGameSize = 10f;
    static readonly int MAX_TILE_SIZE = 101;
    static readonly int CENTER_TILE_INDEX = (MAX_TILE_SIZE - 1) / 2;
    public static bool[,] CollisionMap { get; set; }

    private static int HighScore {
        get { return PlayerPrefs.GetInt("HighScore"); }
        set { PlayerPrefs.SetInt("HighScore", value); }
    }


    [HideInInspector] public static int TileDimensionInt { get; private set; }
    [HideInInspector] public static int TileIndexMax { get; private set; }
    [HideInInspector] public static int TileIndexMin { get; private set; }
    static float TileDimensionFloat;
    static Vector2 RenderOffset;
    static float GridSize;

    public static Collection<Vector2Int> Food { get; private set; }
    public static Dictionary<Vector2Int, int> Eggs { get; private set; }

    public static Collection<Vector2Int> Walls { get; private set; }

    static PlayerInputMap Inputs;
    static Snake 
        PlayerSnake;
    static List<Snake> EnemySnakes;
    static Dictionary<Vector2Int, Wobble> Wobbles;

    // Wobble Settings
    readonly static float TestWobbleDissipation = 1.05f;

    IEnumerator DeathCoroutine = null;

    private static int Score;

    private static GameObject ScoreTextObject;

    public static void AddScore()
    {
        Score++;
        HighScore = Mathf.Max(HighScore, Score);
        ScoreTextObject.GetComponent<TextMeshProUGUI>().text = Score.ToString();
    }

    public static void UpdateProgressBar()
    {
        GameObject ProgressBarBG = GameObject.Find("Canvas/ProgressBar/Background");
        GameObject ProgressBarFG = GameObject.Find("Canvas/ProgressBar/Foreground");
        ProgressBarFG.transform.localScale = new Vector3(   ProgressBarBG.transform.localScale.x, 
                                                            ProgressBarBG.transform.localScale.y * ((TileDimensionFloat - 1) % 2) / 2, 
                                                            ProgressBarBG.transform.localScale.z);
    }

    public static Vector2 GetMovement()
    {
        return Movement;
    }

    void Awake()
    {

        // INITIALIZE STATIC PROPERTIES
        CollisionMap = new bool[MAX_TILE_SIZE, MAX_TILE_SIZE];
        Food = new Collection<Vector2Int>();
        Eggs = new Dictionary<Vector2Int, int>();
        Walls = new Collection<Vector2Int>();
        EnemySnakes = new List<Snake>();
        Wobbles = new Dictionary<Vector2Int, Wobble>();
        Score = 0;

        ScoreTextObject = GameObject.Find("ScoreText");
        if (ScoreTextObject == null)
        {
            Debug.LogError("ScoreText object not found.");
        }

        GameAssets.Sound.SnekDance.Play();

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
        PlaceWall(new Vector2Int(49, 49));

        UpdateProgressBar();

    }



    static void SetTileDimension(float Amount)
    {
        TileDimensionFloat = Amount;
        TileDimensionInt = (int)TileDimensionFloat - ((int)TileDimensionFloat + 1) % 2;
        GridSize = ArenaGameSize / TileDimensionFloat;
        TileIndexMin = CENTER_TILE_INDEX - (TileDimensionInt - 1) / 2;
        TileIndexMax = CENTER_TILE_INDEX + (TileDimensionInt - 1) / 2;
        RenderOffset = new Vector2(-GridSize * CENTER_TILE_INDEX, -GridSize * CENTER_TILE_INDEX);
        DefineMeshes();
    }

    static public void GrowArena()
    {
        SetTileDimension(TileDimensionFloat += 18f / (TileDimensionFloat * TileDimensionFloat));
        UpdateProgressBar();
    }

    public void Update()
    {

        PlayerSnake.UpdateSnake();
        if (PlayerSnake.Dead && DeathCoroutine == null)
        {
            DeathCoroutine = PlayerDeadScript();
            StartCoroutine(DeathCoroutine);
        }

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

        // Handle Music Changes
        float PitchDampen = 200f;
        GameAssets.Sound.SnekDance.Pitch = 1 + (TIME_SCALE - 1) / PitchDampen;

    }

    public void OnPostRender()
    {

        // RENDER OPEN SPOTS
        for (int i = TileIndexMin; i <= TileIndexMax; i++)
        {
            for (int j = TileIndexMin; j <= TileIndexMax; j++)
            {
                DrawMeshOnGrid(Meshes["ArenaSquareMesh"], GameAssets.Material.ArenaGrid, new Vector2Int(i, j), Quaternion.identity);
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
            DrawMeshOnGrid(Meshes["EngorgedMesh"], GameAssets.Material.Food, FoodPosition, Quaternion.identity);
        }

        // RENDER THE EGGS
        if (Eggs.Count > 0)
        {
            foreach (KeyValuePair<Vector2Int, int> EggKeyPair in Eggs)
            {
                int Tier = EggKeyPair.Value;
                if (!Meshes.TryGetValue("EngorgedMesh", out Mesh mesh)) Debug.LogError("Missing mesh");
                Material mat = GameAssets.GetTierMaterial(Tier);
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

        // RENDER WALLS
        // DRAW SNAKE BODY

        foreach (Vector2Int WallPosition in Walls)
        {
            
            DrawMeshOnGrid(Meshes["EngorgedMesh"], GameAssets.Material.Wall, WallPosition, Quaternion.identity);

            // DRAW WALL OUTLINE
            Material OutlineMaterial = GameAssets.Material.EnemyOutline;
            DrawMeshOnGrid(Meshes["SnakeEngorgedBodyOutline"], OutlineMaterial, WallPosition, Quaternion.Euler(0f, 0f, 0f));
            DrawMeshOnGrid(Meshes["SnakeEngorgedBodyOutline"], OutlineMaterial, WallPosition, Quaternion.Euler(0f, 0f, 90f));
            DrawMeshOnGrid(Meshes["SnakeEngorgedBodyOutline"], OutlineMaterial, WallPosition, Quaternion.Euler(0f, 0f, 180f));
            DrawMeshOnGrid(Meshes["SnakeEngorgedBodyOutline"], OutlineMaterial, WallPosition, Quaternion.Euler(0f, 0f, 270f));
        
        }
        
    }

    public static void PlaceEgg(Vector2Int Position, int Tier)
    {
        Eggs[Position] = Tier;
        CollisionMap[Position.y, Position.x] = true;
    }

    public static void PlaceWall(Vector2Int Position)
    {
        Walls.Add(Position);
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

        Meshes["SnakeBody"] = new Mesh()
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

        float OutlinePixelThickness = 0.05f;
        Meshes["SnakeBodyOutline"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(-GridSize / 4, GridSize / 4),
                new Vector3(GridSize / 4, GridSize / 4),
                new Vector3(GridSize / 4, GridSize / 4 - OutlinePixelThickness),
                new Vector3(-GridSize / 4, GridSize / 4 - OutlinePixelThickness),
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        Meshes["SnakeEngorgedBodyOutline"] = new Mesh()
        {
            vertices = new Vector3[4]
            {
                new Vector3(-GridSize / 3, GridSize / 3),
                new Vector3(GridSize / 3, GridSize / 3),
                new Vector3(GridSize / 3, GridSize / 3 - OutlinePixelThickness),
                new Vector3(-GridSize / 3, GridSize / 3 - OutlinePixelThickness),
            },
            triangles = new int[6] { 0, 1, 2, 0, 2, 3 }
        };

        Meshes["SnakeSnoutOutline"] = new Mesh()
        {
            vertices = new Vector3[6]
            {
                new Vector3(GridSize / 6, GridSize / 3),
                new Vector3(GridSize / 3, 0),
                new Vector3(GridSize / 6, -GridSize / 3),
                new Vector3(GridSize / 6 - OutlinePixelThickness, GridSize / 3),
                new Vector3(GridSize / 3 - OutlinePixelThickness, 0),
                new Vector3(GridSize / 6 - OutlinePixelThickness, -GridSize / 3),
            },
            triangles = new int[12] { 0, 1, 3, 1, 4, 3, 1, 2, 4, 2, 5, 4 }
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
                new Vector3(pos.x * GridSize, pos.y * GridSize) + (Vector3)(AffectedByWobble ? GetWobbleOffset(new Vector2Int(pos.x, pos.y)) : Vector2.zero) + (Vector3)RenderOffset,
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
            case Snake.MoveDir.None:
                return Snake.MoveDir.None;
        }

        Debug.Log("Unrecognized Direction.");
        return Snake.MoveDir.Right;
    }

    

    IEnumerator PlayerDeadScript()
    {
        float TimeDelay = 0.25f;
        float TimeIncrement = TimeDelay / 5f;
        float MenuTimeDelay = 2f;
        TIME_SCALE = 3;

        while (EnemySnakes.Count > 0)
        {
            yield return new WaitForSeconds(TimeDelay); 
            TIME_SCALE += TimeIncrement;
        }

        TIME_SCALE = 1;
        yield return new WaitForSeconds(MenuTimeDelay);
        SceneManager.LoadScene(0); //Load Menu
    }

}
