using System;
using System.Collections.Generic;
using UnityEngine;


public class Pathfinder
{

    private List<PathFinderNode> OpenList;
    private List<PathFinderNode> ClosedList;

    private PathFinderNode[,] grid;
    private readonly int width;
    private readonly int height;
    private readonly Vector2Int MapOffset;
    private readonly int AvoidTier;

    public Pathfinder(int AvoidTier = 0)
    {
        this.width = GameController.TileDimensionInt;
        this.height = GameController.TileDimensionInt;
        this.MapOffset = new Vector2Int(GameController.TileIndexMin, GameController.TileIndexMin);
        this.AvoidTier = AvoidTier;
        grid = new PathFinderNode[height, width];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                grid[j, i] = new PathFinderNode(this, i, j);
            }
        }
    }

    public bool IsNodeOpen(PathFinderNode Node)
    {
        int MapX = Node.X + MapOffset.x;
        int MapY = Node.Y + MapOffset.y;
        if (GameController.CollisionMap[MapY, MapX]) {

           Vector2Int MapPosition = new Vector2Int(MapX, MapY);
           if (GameController.Food.Contains(MapPosition)) return true;
           if (GameController.Eggs.TryGetValue(MapPosition, out int EggTier) && (EggTier < AvoidTier || AvoidTier == 0)) return true;
           return false;
        }  else
        {
            return true;
        }
    }

    public List<PathFinderNode> FindPath(int StartX, int StartY, int EndX, int EndY)
    {
        PathFinderNode StartNode = grid[StartY - MapOffset.y, StartX - MapOffset.x];
        PathFinderNode EndNode = grid[EndY - MapOffset.y, EndX - MapOffset.x];

        OpenList = new List<PathFinderNode> { StartNode };
        ClosedList = new List<PathFinderNode>();

        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                PathFinderNode PathNode = grid[y, x];
                PathNode.GCost = int.MaxValue;
                PathNode.CalculateFCost();
                PathNode.CameFromNode = null;
            }
        }

        StartNode.GCost = 0;
        StartNode.HCost = CalculateDistanceCost(StartNode, EndNode);
        StartNode.CalculateFCost();

        while(OpenList.Count > 0)
        {
            PathFinderNode CurrentNode = GetLowestFCostNode(OpenList);
            if (CurrentNode == EndNode)
            {
                return CalculatePath(EndNode);
            }

            OpenList.Remove(CurrentNode);
            ClosedList.Add(CurrentNode);

            foreach (PathFinderNode NeighborNode in GetNeighorNodes(CurrentNode))
            {
                if (ClosedList.Contains(NeighborNode)) continue;

                if (!IsNodeOpen(NeighborNode))
                {
                    if (!ClosedList.Contains(NeighborNode))
                    {
                        ClosedList.Add(NeighborNode);
                    }
                    continue;
                }
                else
                {
                    // Debug.Log("Open Path");
                }

                int TentativeGCost = CurrentNode.GCost + 1;
                if (TentativeGCost < NeighborNode.GCost)
                {
                    NeighborNode.CameFromNode = CurrentNode;
                    NeighborNode.GCost = TentativeGCost;
                    NeighborNode.HCost = CalculateDistanceCost(NeighborNode, EndNode);
                    NeighborNode.CalculateFCost();
                }

                if (!OpenList.Contains(NeighborNode))
                {
                    OpenList.Add(NeighborNode);
                }

            }

        }

        return null;

    }

    private List<PathFinderNode> GetNeighorNodes(PathFinderNode CurrentNode)
    {
        List<PathFinderNode> NeighborNodes = new List<PathFinderNode>();
        if (CurrentNode.X > 0)
        {
            NeighborNodes.Add(grid[CurrentNode.Y, CurrentNode.X - 1]);
        }
        if (CurrentNode.X < width - 1)
        {
            NeighborNodes.Add(grid[CurrentNode.Y, CurrentNode.X + 1]);
        }
        if (CurrentNode.Y > 0)
        {
            NeighborNodes.Add(grid[CurrentNode.Y - 1, CurrentNode.X]);
        }
        if (CurrentNode.Y < height - 1)
        {
            NeighborNodes.Add(grid[CurrentNode.Y + 1, CurrentNode.X]);
        }
        return NeighborNodes;
    }

    private List<PathFinderNode> CalculatePath(PathFinderNode EndNode)
    {

        List<PathFinderNode> NodePathList = new List<PathFinderNode>();
        PathFinderNode Node = EndNode;
        NodePathList.Insert(0, EndNode);
        while (Node.CameFromNode != null)
        {
            Node = Node.CameFromNode;
            NodePathList.Insert(0, Node);
        }

        return NodePathList;
    }

    private int CalculateDistanceCost(PathFinderNode a, PathFinderNode b)
    {
        int DeltaX = Math.Abs(a.X - b.X);
        int DeltaY = Math.Abs(a.Y - b.Y);
        return DeltaX + DeltaY;
    }

    private PathFinderNode GetLowestFCostNode(List<PathFinderNode> PathNodeList)
    {
        PathFinderNode LowestFCostNode = PathNodeList[0];
        for (int i = 0; i < PathNodeList.Count; i++)
        {
            if(PathNodeList[i].FCost < LowestFCostNode.FCost)
            {
                LowestFCostNode = PathNodeList[i];
            }
        }
        return LowestFCostNode;
    }

}
