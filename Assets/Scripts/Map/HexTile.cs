using Unity.MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    [SerializeField] private bool occupied_;
    [SerializeField] private GameCharacter occupier_;
    [SerializeField] private Vector2Int position;
    
    private Dictionary<int, HexTile> neighbors = new Dictionary<int, HexTile>();

    public Vector2Int Position
    {
        get { return position; }
        set { position = value; }
    }

    public Dictionary<int, HexTile> Neighbors
    {
        get { return neighbors; }
        set { neighbors = value; }
    }

    public bool Occupied
    {
        get { return occupied_; }
    }

    public GameCharacter Occupier
    {
        get
        {
            return occupier_;
        }
        set
        {
            if (!Occupied)
            {
                occupied_ = true;
                occupier_ = value;
            }
            else
            {
                throw new System.Exception("Tile is already occupied!");
            }
        }
    }

    public void AddNeighbor(int dir, HexTile neighbor)
    {
        neighbors.Add(dir, neighbor);
    }

    public void EmptyTile()
    {
        occupied_ = false;
        occupier_ = null;
    }
}


