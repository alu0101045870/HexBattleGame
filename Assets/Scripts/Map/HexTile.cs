using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    private Vector2Int position;
    public List<HexTile> neighbors = new List<HexTile>();

    public Vector2Int Position
    {
        get { return position; }
        set { position = value; }
    }

    public List<HexTile> Neighbors
    {
        get { return neighbors; }
        set { neighbors = value; }
    }

    public void AddNeighbor(GameObject neighbor)
    {
        neighbors.Add(neighbor.GetComponent<HexTile>());
    }

    // EVALUATE THE OPTION OF HAVING AN "OCCUPIED" FLAG 
    // AND A REFERENCE TO THE UNIT INSIDE THE CLASS
}


