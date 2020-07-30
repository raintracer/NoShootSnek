
// #define DisplayDeathMessagesFlag

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Snake
{

    public Vector2Int HeadPosition { get; private set; }
    private int Length;

    bool DisplayDeathMessagesFlag = false;

    public LinkedList<SnakeBody> Bodies { get; private set; }

    private bool Dying = false;
    public bool Dead { get; private set; } = false;

    private const float PlayerBaseMoveSpeed = 0.05f;
    private readonly float MoveSpeed;
    private float SpeedMultiplier = 1;
    private float MoveFraction = 0f;

    public bool IsPlayer { get; private set; }
    private bool PathChecked = false;
    private bool Forging = false;

    private MoveDir Facing;

    private bool Engorged;
    private SnakeBody.FoodType Digestion = SnakeBody.FoodType.None;
    private int FoodTier;

    private Snake ForgingSnake;
    private readonly int Tier;
    
    public enum MoveDir
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
        None = 4
    }

    public class SnakeBody
    {

        public Vector2Int Position { get; private set; }
        public bool Engorged { get; set; }
        public FoodType Digestion { get; set; }
        public int FoodTier { get; set; }
        public MoveDir FacingTail { get; set; }
        public MoveDir FacingHead { get; set; }


        public enum FoodType
        {
            None, Food, Egg
        }

        public SnakeBody(Vector2Int Position, MoveDir FacingHead, bool Engorged = false)
        {
            this.Position = Position;
            this.Engorged = Engorged;
            this.FacingHead = FacingHead;
            Digestion = FoodType.None;
        }

        public MoveDir GetFacingHead() { return FacingHead; }

    }

    public Snake(bool IsPlayer, Vector2Int HeadPosition, int Length = 0, int Tier = 0, MoveDir Facing = MoveDir.Right, bool Forging = false)
    {

        this.HeadPosition = HeadPosition;
        this.Forging = Forging;
        this.Facing = Facing;
        this.Length = Length;
        this.IsPlayer = IsPlayer;
        this.Tier = Tier;

        GameController.CollisionMap[HeadPosition.y, HeadPosition.x] = true;
        Bodies = new LinkedList<SnakeBody>();

        if (IsPlayer)
        {
            MoveSpeed = PlayerBaseMoveSpeed;
        }
        if (!IsPlayer)
        {
            switch (Tier)
            {
                case 1:
                    MoveSpeed = PlayerBaseMoveSpeed / 2f;
                    break;
                case 2:
                    MoveSpeed = PlayerBaseMoveSpeed / 1.5f;
                    break;
                case 3:
                    MoveSpeed = PlayerBaseMoveSpeed;
                    break;
                case 4:
                    MoveSpeed = PlayerBaseMoveSpeed * 1.2f;
                    break;
                default:
                    Debug.LogError("Snake has invalid Tier: " + Tier);
                    break;
            }
        }

    }

    public void Feed(Vector2Int EatPosition, SnakeBody.FoodType TypeEaten)
    {
        if (TypeEaten == SnakeBody.FoodType.Food) {
            if (GameController.Food.Contains(EatPosition)) {
                GameController.Food.Remove(EatPosition);
            }
            if (IsPlayer || GameController.AIMODE)
            {
                GameController.GrowArena();
            }
            GameController.Sound_Food.Play();
            Length++;
            GameController.PlaceRandomFood();
        }

        if (TypeEaten == SnakeBody.FoodType.Egg)
        {
            GameController.Eggs.TryGetValue(EatPosition, out FoodTier);
            if (FoodTier == 0)
            {
                Debug.LogError("Snake has eaten invalid egg position: " + EatPosition);
            }
            else { 
                GameController.Eggs.Remove(EatPosition);
            }
        }

        Engorged = true;
        Digestion = TypeEaten;

        GameController.AddWobbleForce(HeadPosition, 0.3f, true);

    }

    public void UpdateSnake()
    {
        if (IsPlayer)
        {

            //if (GameController.GetMovement().x > 0)
            //{
            //    if (Bodies.First == null || GameController.OppositeDirection(Bodies.First.Value.FacingHead) != MoveDir.Right)
            //        Facing = MoveDir.Right;
            //}
            //else if (GameController.GetMovement().x < 0)
            //{
            //    if (Bodies.First == null || GameController.OppositeDirection(Bodies.First.Value.FacingHead) != MoveDir.Left)
            //        Facing = MoveDir.Left;
            //}
            //else if (GameController.GetMovement().y > 0)
            //{
            //    if (Bodies.First == null || GameController.OppositeDirection(Bodies.First.Value.FacingHead) != MoveDir.Up)
            //        Facing = MoveDir.Up;
            //}
            //else if (GameController.GetMovement().y < 0)
            //{
            //    if (Bodies.First == null || GameController.OppositeDirection(Bodies.First.Value.FacingHead) != MoveDir.Down)
            //        Facing = MoveDir.Down;
            //}

            MoveDir TempMovementFacing = GetMovementFacing();
            if (TempMovementFacing != MoveDir.None)
            {
                if (Bodies.First == null || GameController.OppositeDirection(Bodies.First.Value.FacingHead) != TempMovementFacing) Facing = TempMovementFacing;
            }
        }
    }

    public void FixedUpdateSnake()
    {


        if (Dead || Forging) { return; }

        // AFFECTED PLAYER SPEED
        SpeedMultiplier = 1;
        if (Dying)
        {
            SpeedMultiplier = 5;
        }
        else if (IsPlayer)
        {
            switch (Facing)
            {
                case MoveDir.Right:
                    if (GameController.GetMovement().x > 0)
                    {
                        SpeedMultiplier = 3f;
                    }
                    else if (GameController.GetMovement().x < 0)
                    {
                        SpeedMultiplier = 0.5f;
                    }
                    break;
                case MoveDir.Down:
                    if (GameController.GetMovement().y < 0)
                    {
                        SpeedMultiplier = 3f;
                    }
                    else if (GameController.GetMovement().y > 0)
                    {
                        SpeedMultiplier = 0.5f;
                    }
                    break;
                case MoveDir.Left:
                    if (GameController.GetMovement().x < 0)
                    {
                        SpeedMultiplier = 3f;
                    }
                    else if (GameController.GetMovement().x > 0)
                    {
                        SpeedMultiplier = 0.5f;
                    }
                    break;
                case MoveDir.Up:
                    if (GameController.GetMovement().y > 0)
                    {
                        SpeedMultiplier = 3f;
                    }
                    else if (GameController.GetMovement().y < 0)
                    {
                        SpeedMultiplier = 0.5f;
                    }
                    break;
            }
        }

        MoveFraction += MoveSpeed * SpeedMultiplier;

        if (MoveFraction > 0.5f && !PathChecked) { 

            // RUN PATHFINDING FOR AI SNAKES
            if (!IsPlayer || GameController.AIMODE)
            {
                DetermineFacing();
            }

            PathChecked = true;

        }


        if (MoveFraction >= 2) Debug.LogWarning("A snake MoveFraction is over 2. This will lead to pathfinding errors. Player: " + IsPlayer);

        while (MoveFraction > 1)
        {

            PathChecked = false;
            MoveFraction -= 1f;
            

            if (Dying)
            {

                GameController.AddExplosionToGrid(Bodies.Last.Value.Position);
                RemoveTail();

                if (Bodies.First == null) Dead = true;

                return;

            }

            Vector2Int NewHeadPosition = HeadPosition;
            switch (Facing)
            {
                case MoveDir.Up:
                    NewHeadPosition.y++;
                    break;
                case MoveDir.Right:
                    NewHeadPosition.x++;
                    break;
                case MoveDir.Left:
                    NewHeadPosition.x--;
                    break;
                case MoveDir.Down:
                    NewHeadPosition.y--;
                    break;
                default:
                    break;
            }

            // DETERMINE IF THE SNAKE MUST DIE
            bool DeathCondition = false;
            if (NewHeadPosition.y < GameController.TileIndexMin
                || NewHeadPosition.x < GameController.TileIndexMin
                || NewHeadPosition.y > GameController.TileIndexMax
                || NewHeadPosition.x > GameController.TileIndexMax)
            {
                if (DisplayDeathMessagesFlag) Debug.Log("Snake Death: Left valid tile area.");
                DeathCondition = true;
            }
            if(GameController.CollisionMap[NewHeadPosition.y, NewHeadPosition.x])
            {
                if (IsPlayer) {
                    if(!GameController.Food.Contains(NewHeadPosition) && !GameController.Eggs.ContainsKey(NewHeadPosition))
                    {
                        if (DisplayDeathMessagesFlag) Debug.Log("Snake Death: Player hit block with no food or eggs: " + NewHeadPosition);
                        DeathCondition = true;
                    }
                }
                else {
                    if(!GameController.Food.Contains(NewHeadPosition))
                    {
                        if (!GameController.Eggs.ContainsKey(NewHeadPosition))
                        {
                            if (DisplayDeathMessagesFlag) Debug.Log("Snake Death: AI hit spot with no food or eggs present: " + NewHeadPosition);
                            DeathCondition = true;
                        }
                        else
                        {
                            if (GameController.Eggs.TryGetValue(NewHeadPosition, out int EggTier))
                            {
                                if (EggTier >= Tier)
                                {
                                    if (DisplayDeathMessagesFlag) Debug.Log("Snake Death: AI hit an egg of tier equal or higher. Position: " + NewHeadPosition + ", Snake Tier: " + Tier + ", Egg Tier: " + EggTier);
                                    DeathCondition = true;
                                }
                            }
                            else
                            {
                                Debug.LogError("Game Tried to access a key that wasn't there.");
                            }
                        }
                    }
                }
            }
            if (DeathCondition) {
                StartDying();
                GameController.AddExplosionToGrid(NewHeadPosition);
                return;
            }

            GameController.CollisionMap[HeadPosition.y, HeadPosition.x] = false;
            GameController.CollisionMap[NewHeadPosition.y, NewHeadPosition.x] = true;
            AddTail(HeadPosition, Facing);
            HeadPosition = NewHeadPosition;

            if (Engorged && Bodies.First != null)
            {
                Bodies.First.Value.Engorged = true;
                Bodies.First.Value.Digestion = Digestion;
                Bodies.First.Value.FoodTier = FoodTier;
                Engorged = false;
                Digestion = SnakeBody.FoodType.None;
            }

            if (GameController.Food.Contains(NewHeadPosition))
            {
                Feed(NewHeadPosition, SnakeBody.FoodType.Food);
            }
            else if (GameController.Eggs.ContainsKey(NewHeadPosition))
            {
                Feed(NewHeadPosition, SnakeBody.FoodType.Egg);
            }

            while (Bodies.Count > Length)
            {
                RemoveTail();
            }

        }

    }

    public void ForgeSnake(int SnakeTier)
    {
        if (ForgingSnake != null && ForgingSnake.Tier != SnakeTier)
        {
            DeforgeSnake();
        }
        if (ForgingSnake == null)
        {
            ForgingSnake = GameController.AddEnemySnake(Bodies.Last.Value.Position, SnakeTier, Bodies.Last.Value.FacingTail);
            ForgingSnake.Forging = true;
        }
        else
        {
            ForgingSnake.AddTail(Bodies.Last.Value.Position, Bodies.Last.Value.FacingTail, false);
            ForgingSnake.Length++;
        }
    }

    public void DeforgeSnake()
    {
        if (ForgingSnake != null)
        {
            ForgingSnake.Forging = false;
            ForgingSnake = null;
        }
    }

    public void RemoveTail()
    {

        bool Forged = false;

        if (Dying)
        {
            GameController.PlaceEgg(Bodies.Last.Value.Position, IsPlayer ? Tier + 1 : Tier);
        }
        else if (Bodies.Last.Value.Engorged)
        {
            switch (Bodies.Last.Value.Digestion)
            {
                case SnakeBody.FoodType.Food:
                    if (Tier + 1 <= 4)
                    {
                        GameController.PlaceEgg(Bodies.Last.Value.Position, Tier + 1);
                    }
                    else
                    {
                        GameController.PlaceWall(Bodies.Last.Value.Position);
                    }
                    break;
                case SnakeBody.FoodType.Egg:
                    if (IsPlayer)
                    {
                        ForgeSnake(Bodies.Last.Value.FoodTier);
                        Forged = true;
                    }
                    else
                    {
                        GameController.PlaceEgg(Bodies.Last.Value.Position, Bodies.Last.Value.FoodTier);
                    }
                    break;
                case SnakeBody.FoodType.None:
                    Debug.LogError("Body was engorged with no food type.");
                    break;
                default:
                    Debug.LogError("No recognized food type: " + Bodies.Last.Value.Digestion);
                    break;
            }

        } 
        else
        {
            
            GameController.CollisionMap[Bodies.Last.Value.Position.y, Bodies.Last.Value.Position.x] = false;
        }

        if (ForgingSnake != null && !Forged)
        {
            DeforgeSnake();
        }

        Bodies.RemoveLast();
    }

    public void AddTail(Vector2Int BodyPosition, MoveDir BodyFacing, bool AddFirst = true)
    {
        if (AddFirst)
        {
            Bodies.AddFirst(new SnakeBody(BodyPosition, BodyFacing));
            if(Bodies.First.Next != null)
            {
                Bodies.First.Value.FacingTail = GameController.OppositeDirection(Bodies.First.Next.Value.FacingHead);
            }
        }
        else
        {
            Bodies.AddLast(new SnakeBody(BodyPosition, BodyFacing));
        }
        GameController.CollisionMap[BodyPosition.y, BodyPosition.x] = true;
    }

    public void DetermineFacing()
    {

        Pathfinder Path;
        List<PathFinderNode> PathNodes;
        int PathTier = 1;
        do
        {
            Path = new Pathfinder(PathTier);
            PathNodes = Path.FindPath(HeadPosition.x, HeadPosition.y, GameController.Food[0].x, GameController.Food[0].y);
            PathTier++;
        } while (PathNodes == null && PathTier <= Tier);

        if (PathNodes != null)
        {
            if (PathNodes.Count < 2)
            {
                throw new Exception("Path with no index 1, only one node. Food Position: " + GameController.Food[0] + " HeadPos: " + HeadPosition);
            }

            PathFinderNode Node = PathNodes[1];
            Vector2Int TargetPosition = new Vector2Int(Node.X, Node.Y) + new Vector2Int(GameController.TileIndexMin, GameController.TileIndexMin);

            if (TargetPosition.x > HeadPosition.x)
            {
                Facing = MoveDir.Right;
            }
            else if (TargetPosition.x < HeadPosition.x)
            {
                Facing = MoveDir.Left;
            }
            else if (TargetPosition.y > HeadPosition.y)
            {
                Facing = MoveDir.Up;
            }
            else if (TargetPosition.y < HeadPosition.y)
            {
                Facing = MoveDir.Down;
            }
        }
    }

    public void StartDying()
    {
        Dying = true;
        DeforgeSnake();
        AddTail(HeadPosition, Facing, true);
    }

    public void Die()
    {

        if (IsPlayer && !GameController.AIMODE)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        GameController.CollisionMap[HeadPosition.y, HeadPosition.x] = false;
        GameController.AddExplosionToGrid(HeadPosition);

    }

    public void RenderSnake()
    {

        if (Dead) return;

        // DRAW TAIL
        LinkedListNode<SnakeBody> node = Bodies.Last;

        Material mat = GameController.Materials[IsPlayer ? "Player" : "Tier" + Tier];
        Mesh mesh;
        Quaternion quat;

        if (node != null)
        {

            while (node != null)
            {

                // DRAW SNAKE TRAIL LINE
                switch (node.Value.FacingHead)
                {
                    case MoveDir.Right:
                        quat = Quaternion.identity;
                        break;
                    case MoveDir.Up:
                        quat = Quaternion.Euler(0, 0, 90f);
                        break;
                    case MoveDir.Left:
                        quat = Quaternion.Euler(0, 0, 180f);
                        break;
                    case MoveDir.Down:
                        quat = Quaternion.Euler(0, 0, 270f);
                        break;
                    default:
                        quat = Quaternion.identity;
                        Debug.LogError("Drawing snake trace line: Invalid Facing direction detected");
                        break;
                }

                GameController.DrawMeshOnGrid(GameController.Meshes["SnakeLineMesh"], GameController.Materials["Line"], node.Value.Position, quat);


                // DRAW SNAKE BODY

                Mesh OutlineMesh;
                if (node.Value.Engorged)
                {
                    mesh = GameController.Meshes["EngorgedMesh"];
                    OutlineMesh = GameController.Meshes["SnakeEngorgedBodyOutline"];
                }
                else
                {
                    mesh = GameController.Meshes["SnakeBody"];
                    OutlineMesh = GameController.Meshes["SnakeBodyOutline"];
                }

                GameController.DrawMeshOnGrid(mesh, mat, node.Value.Position, Quaternion.identity);

                if (!IsPlayer)
                {
                    // DRAW ENEMY SNAKE OUTLINE
                    GameController.DrawMeshOnGrid(OutlineMesh, GameController.Materials["Enemy Outline"], node.Value.Position, Quaternion.Euler(0f, 0f, 0f));
                    GameController.DrawMeshOnGrid(OutlineMesh, GameController.Materials["Enemy Outline"], node.Value.Position, Quaternion.Euler(0f, 0f, 90f));
                    GameController.DrawMeshOnGrid(OutlineMesh, GameController.Materials["Enemy Outline"], node.Value.Position, Quaternion.Euler(0f, 0f, 180f));
                    GameController.DrawMeshOnGrid(OutlineMesh, GameController.Materials["Enemy Outline"], node.Value.Position, Quaternion.Euler(0f, 0f, 270f));
                }
                
                if (node.Value.Engorged)
                {
                    // DRAW EATEN MATERIAL
                    Material EatenMat;
                    if(node.Value.Digestion == SnakeBody.FoodType.Food)
                    {
                        EatenMat = GameController.Materials["Food"];
                    } else
                    {
                        EatenMat = GameController.Materials["Tier" + node.Value.FoodTier];
                    }
                    GameController.DrawMeshOnGrid(GameController.Meshes["ArenaSquareMesh"], EatenMat, node.Value.Position, Quaternion.Euler(0f, 0f, 0f));
                }

                node = node.Previous;
            }
        }

        switch (Facing)
        {
            case MoveDir.Right:
                quat = Quaternion.identity;
                break;
            case MoveDir.Up:
                quat = Quaternion.Euler(0, 0, 90f);
                break;
            case MoveDir.Left:
                quat = Quaternion.Euler(0, 0, 180f);
                break;
            case MoveDir.Down:
                quat = Quaternion.Euler(0, 0, 270f);
                break;
            default:
                quat = Quaternion.identity;
                Debug.LogError("Drawing snake trace line: Invalid Facing direction detected");
                break;
        }

        // DRAW HEAD
        if (!Dying)
        {
            GameController.DrawMeshOnGrid(GameController.Meshes["SnakeHeadMesh"], mat, HeadPosition, quat);
            GameController.DrawMeshOnGrid(GameController.Meshes["SnakeSnoutOutline"], GameController.Materials["Enemy Outline"], HeadPosition, quat);
        }
        

    }

    public static MoveDir GetMovementFacing()
    {
        if (GameController.GetMovement().x > 0)
        {
            return MoveDir.Right;
        }
        else if (GameController.GetMovement().x < 0)
        {
            return MoveDir.Left;
        }
        else if (GameController.GetMovement().y > 0)
        {
            return MoveDir.Up;
        }
        else if (GameController.GetMovement().y < 0)
        {
            return MoveDir.Down;
        }
        else
        {
            return MoveDir.None;
        }
    }

}
