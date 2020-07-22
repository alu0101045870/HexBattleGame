/*    --------------------------------------------------------------------------------------------
 *    ============================================================================================
 *                                      
 * 
 *     
 *    ============================================================================================
 *    --------------------------------------------------------------------------------------------
 */

using System;
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

    public static Vector3 CharacterPosition(Vector2Int destination)
    {
        int col = destination.y;
        int row = destination.x;

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

    public static int RandomDir()
    {
        return UnityEngine.Random.Range(0, 5);
    }

    
    public static void SetNeighborsInMap(Dictionary<Vector2Int, HexTile> tiles)
    {
        List<Vector2Int> keys = tiles.Keys.ToList<Vector2Int>();
        HexTile tile;

        foreach (Vector2Int key in keys)
        {
            Vector2Int[] axial_directions;
            if (key.y % 2 == 0)
                axial_directions = axial_directions_odd_q[1];
            else
                axial_directions = axial_directions_odd_q[0];
                
            for (int i = 0; i < axial_directions.Length; i++)
            {
                Vector2Int temp = key + axial_directions[i];
                if (tiles.TryGetValue(temp, out tile))
                {
                    //Debug.Log(key + " + " + axial_directions[i] + " := " + tile.GetComponent<HexTile>().Position + " | " + i);
                    tiles[key].AddNeighbor(i, tile);
                }
            }
            //Debug.LogWarning("------------------------");
        }
    }

    // Movement helper
    public static HexTile GetNeighborAtDir(Dictionary<Vector2Int, GameObject> tiles, 
        Vector2Int current_coords, int dir)
    {
        GameObject neighbor;
        Vector2Int[] axial_directions;

        if (current_coords.y % 2 == 0)
            axial_directions = axial_directions_odd_q[1];
        else
            axial_directions = axial_directions_odd_q[0];
       
        if (tiles.TryGetValue(current_coords + axial_directions[dir], out neighbor)) {
            return neighbor.GetComponent<HexTile>();
        }
        
        return null;
        
    }

    public static Vector2Int GetNeighborAtDir(Vector2Int current_coords, int dir)
    {
        Vector2Int[] axial_directions;

        if (current_coords.y % 2 == 0)
            axial_directions = axial_directions_odd_q[1];
        else
            axial_directions = axial_directions_odd_q[0];

        return current_coords + axial_directions[dir];
    }

    // ---------------------------------          ------------------------------------------------

    // Given two positions, return travel direction from the first to the second (if adjacent)
    public static int GetAdjacentTravelDirection(Vector2Int orig, Vector2Int dest)
    {


        // If positions are not adjacent at all, 
        return -1;
    }

    // --------------------------------- DISTANCE METHODS ----------------------------------------

    // Check wether or not a certain position is in a given range param. distance to another
    // DOES account for obstacles and holes in map
    // Effectively, checks wether there is a path of (at most) a certain length that can be traced
    //  between two positions
    public static bool InMapRange(Vector2Int orig, Vector2Int dest, int range)
    {
        throw new NotImplementedException();
    }

    // Check wether or not a certain position is in a given range param. distance to another
    // Does NOT account for obstacles or holes in map
    public static bool InEuclideanRange(Vector2Int orig, Vector2Int dest, int range)
    {
        throw new NotImplementedException();
    }

    // Check wether or not a certain position is in a given range param. distance to another
    // Goes in a straight line, ignoring obstacles and holes
    public static int InLineRange(Vector2Int orig, Vector2Int dest, int range)
    {
        Vector2Int trace = new Vector2Int();

        for (int dir = 0; dir < 6; dir++)
        {
            trace.x = orig.x;
            trace.y = orig.y;

            for (int j = 0; j < range; j++)
            {
                trace = GetNeighborAtDir(trace, dir);

                if (trace == dest)
                    return dir;

            }
        }

        // Not found in any direction at range
        return -1;
    }


    public static int DistanceBetween(Vector2Int orig, Vector2Int dest)
    {
        return -1;
    }
}
