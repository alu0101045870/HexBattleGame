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
using Random = UnityEngine.Random;

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

    public static float HexWidth => 2 * SIZE;

    public static float HexHeight { get { return H_MULT * SIZE; } }
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
            R * VertOffset + ((Q & 1) * HexHeight / 2)
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
            R * VertOffset + ((Q & 1) * HexHeight / 2)
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
            R * VertOffset + ((Q & 1) * HexHeight / 2)
            );
    }

    public static bool IsNeighbor(Vector2Int callerPos, Vector2Int compPos)
    {
        Vector2Int[] axial_directions;
        if ((callerPos.y & 1) == 0)
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
            if ((key.y & 1) == 0)
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

    public static Vector2Int GetNeighborAtDir(Vector2Int current_coords, int dir)
    {
        Vector2Int[] axial_directions;

        if ((current_coords.y & 1) == 0)
            axial_directions = axial_directions_odd_q[1];
        else
            axial_directions = axial_directions_odd_q[0];

        return current_coords + axial_directions[dir];
    }

    // ---------------------------------          ------------------------------------------------

    public static int GeneralDirectionTowards(Vector2Int pos1, Vector2Int pos2)
    {
        int generalDir = -1;

        if ((pos1.y & 1) == 0)                     // even tile
        {
            if ((pos1.x == pos2.x && pos1.y < pos2.y) || (pos1.x < pos2.x && pos1.y < pos2.y))
            {
                generalDir = 2;
            }
            else if ((pos1.x == pos2.x && pos1.y > pos2.y) || (pos1.x < pos2.x && pos1.y > pos2.y))
            {
                generalDir = 0;
            }
            else if (pos1.x > pos2.x && pos1.y < pos2.y)
            {
                generalDir = 5;
            }
            else if (pos1.x > pos2.x && pos1.y > pos2.y)
            {
                generalDir = 3;
            }
            else if (pos1.x > pos2.x && pos1.y == pos2.y)
            {
                generalDir = 4;
            }
            else if (pos1.x < pos2.x && pos1.y == pos2.y)
            {
                generalDir = 1;
            }
        }
        else                                    // odd tile
        {
            if ((pos1.x == pos2.x && pos1.y < pos2.y) || (pos1.x > pos2.x && pos1.y < pos2.y))
            {
                generalDir = 5;
            }
            else if ((pos1.x == pos2.x && pos1.y > pos2.y) || (pos1.x > pos2.x && pos1.y > pos2.y))
            {
                generalDir = 3;
            }
            else if (pos1.x < pos2.x && pos1.y < pos2.y)
            {
                generalDir = 2;
            }
            else if (pos1.x < pos2.x && pos1.y > pos2.y)
            {
                generalDir = 0;
            }
            else if (pos1.x > pos2.x && pos1.y == pos2.y)
            {
                generalDir = 4;
            }
            else if (pos1.x < pos2.x && pos1.y == pos2.y)
            {
                generalDir = 1;
            }
        }

        return generalDir;
    }

    public static List<int> OppositeDir(int dir)
    {
        List<int> oppositedir = new List<int>();

        switch (dir)
        {
            case 0:
                oppositedir.AddRange(new int[] { 5, 4, 2 });
                break;
            case 1:
                oppositedir.AddRange(new int[] { 4, 3, 5 });
                break;
            case 2:
                oppositedir.AddRange(new int[] { 3, 4, 0 });
                break;
            case 3:
                oppositedir.AddRange(new int[] { 2, 1, 5 });
                break;
            case 4:
                oppositedir.AddRange(new int[] { 1, 0, 2 });
                break;
            case 5:
                oppositedir.AddRange(new int[] { 0, 1, 3 });
                break;
            default:
                oppositedir.Add(Random.Range(0, 5));
                break;
        }

        return oppositedir;
    }

    // ------------------------ COORDINATE TYPE TRANSLATION METHODS ------------------------------

    /*
     function cube_to_oddq(cube):
    var col = cube.x
    var row = cube.z + (cube.x - (cube.x&1)) / 2
    return OffsetCoord(col, row)

function oddq_to_cube(hex):
    var x = hex.col
    var z = hex.row - (hex.col - (hex.col&1)) / 2
    var y = -x-z
    return Cube(x, y, z)
         */

    public static Vector2Int CubeToOffset (Vector3Int cubeCoords)
    {
        Vector2Int offsetCoords = new Vector2Int();
        
        
        return offsetCoords;
    }

    public static Vector3Int OffsetToCube (Vector2Int offsetCoords)
    {
        Vector3Int cubeCoords = new Vector3Int();

        return cubeCoords;
    }

    // --------------------------------- DISTANCE METHODS ----------------------------------------

    public static int CubeDistance (Vector3Int cubeCoords1, Vector3Int cubeCoords2)
    {
        return (
            Mathf.Abs(cubeCoords1.x - cubeCoords2.x) 
            + Mathf.Abs(cubeCoords1.y - cubeCoords2.y) 
            + Mathf.Abs(cubeCoords1.z - cubeCoords2.z)
            ) 
            / 2;
    }

    public static int OffsetDistance (Vector2Int offsetCoords1, Vector2Int offsetCoords2)
    {
        return CubeDistance(OffsetToCube(offsetCoords1), OffsetToCube(offsetCoords2));
    }

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
        throw new NotImplementedException();
    }
}
