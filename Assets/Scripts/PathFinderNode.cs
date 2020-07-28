using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinderNode
{

    private Pathfinder parent;
    public int X { get; private set; }
    public int Y { get; private set; }
    public int FCost { get; private set; }

    public int GCost;
    public int HCost;

    public PathFinderNode CameFromNode;

    public PathFinderNode(Pathfinder parent, int x, int y){
        this.parent = parent;
        this.X = x;
        this.Y = y;
    }

    public override string ToString() {
        return X + ", " + Y;
    }

    public void CalculateFCost()
    {
        FCost = GCost + HCost;
    }

}
