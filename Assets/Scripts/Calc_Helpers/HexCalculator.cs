/*    --------------------------------------------------------------------------------------------
 *    ============================================================================================
 *                                      
 * 
 *     
 *    ============================================================================================
 *    --------------------------------------------------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// This class decides a conceptual Hexagonal Tile position in the real world
/// based on given coordinates, appart from other properties like neighbors, size, etc.
/// -- It is the "concept" of a tile and abstracts from the math bound to its geometry
/// </summary>
/// <see cref="https://www.redblobgames.com/grids/hexagons/"/>
public static class HexCalculator 
{
    // ------------------------------------- ATTRIBUTES ------------------------------------------
    // ===========================================================================================

    static readonly float H_MULT = Mathf.Sqrt(3);
    static readonly float SIZE = 1f;

    // ------------------------------------- PROPERTIES ------------------------------------------
    // ===========================================================================================

    private static float HexWidth => 2 * SIZE;

    private static float HexHeight { get { return H_MULT * SIZE; } }
    private static float VertOffset { get { return HexHeight; } }
    private static float HorzOffset { get { return HexWidth * 0.75f; } }

    private static Vector2Int[][] axial_directions_odd_q = {
        
        // .......................................................
        new Vector2Int[]{       
                            new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(1, 1),
                            new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(0, 1)   
        },
        // ..........................Odd cols, All rows..............................
        new Vector2Int[]{
                            new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1),
                            new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
        }
    };

    // ----------------------------------- PROPER METHODS ----------------------------------------
    // ===========================================================================================

    // These tiles are disposed on a "flat bottom" fashion
    public static Vector3 Position(int col, int row)
    {

        // Q + R + S = 0 ==> S = -Q - R
        int Q = col;
        int R = row;
        int S = -(Q + R);

        // if Q % 2 == 0 => Extra offset is not added, which leads to the characteristic "saw pattern" 
        // TODO: Revise if possible to avoid % operation for optimization
        return new Vector3(
            Q * HorzOffset, 
            0,
            R * VertOffset + ((Q % 2) * HexHeight / 2)
            );
    }

    public static Vector3 CharacterPosition(int col, int row)
    {
        // Q + R + S = 0 ==> S = -Q - R
        int Q = col;
        int R = row;
        int S = -(Q + R);

        // if Q % 2 == 0 => Extra offset is not added, which leads to the characteristic "saw pattern" 
        // TODO: Revise if possible to avoid % operation for optimization
        return new Vector3(
            Q * HorzOffset,
            0.8f,
            R * VertOffset + ((Q % 2) * HexHeight / 2)
            );
    }

    public static bool IsNeighbor(Vector2Int callerPos, Vector2Int compPos)
    {
        Vector2Int[] axial_directions;
        if (callerPos.y % 2 == 0)
            axial_directions = axial_directions_odd_q[1];
        else
            axial_directions = axial_directions_odd_q[0];

        foreach (Vector2Int dir in axial_directions)
        {
            if (callerPos + dir == compPos)
                return true;
        }

        return false;
    }


    // TODO:
    // CHECK METHOD OVERHEAD WITH PROFILER
    //
    public static void SetNeighborsInMap(Dictionary<Vector2Int, GameObject> tiles)
    {
        List<Vector2Int> keys = tiles.Keys.ToList<Vector2Int>();
        GameObject tile;

        foreach (Vector2Int key in keys)
        {
            Vector2Int[] axial_directions;
            if (key.y % 2 == 0)
                axial_directions = axial_directions_odd_q[1];
            else
                axial_directions = axial_directions_odd_q[0];
                
            foreach (Vector2Int dir in axial_directions)
            {
                Vector2Int temp = key + dir;
                if (tiles.TryGetValue(temp, out tile))
                {
                    //Debug.Log(key + " + " + dir + " := " + tile.GetComponent<HexTile>().Position);
                    tiles[key].GetComponent<HexTile>().AddNeighbor(tile);
                }
            }
            //Debug.LogWarning("------------------------");
        }
    }
}
