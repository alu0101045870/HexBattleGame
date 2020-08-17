using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public abstract class Enemy : GameCharacter
{
    protected List<string> targetList_ = new List<string>();
    protected List<string> predatorList_ = new List<string>();

    public Enemy(IEnumerable<string> targetList, IEnumerable<string> predatorList)
    {
        targetList_.AddRange(targetList);
        predatorList_.AddRange(predatorList);
    }

    public virtual void SetParameters()
    {
        throw new NotImplementedException();
    }

    public virtual bool OccupierInPredatorList(HexTile neighbor)
    {
        if (!neighbor.Occupied) return false;

        for (int i = 0; i < predatorList_.Count; i++)
        {
            if (neighbor.Occupier.Species.Equals(predatorList_[i]))
                return true;
        }

        return false;
    }

    public virtual bool OccupierInTargetList(HexTile neighbor)
    {
        if (!neighbor.Occupied) return false;

        for (int i = 0; i < targetList_.Count; i++)
        {
            if (neighbor.Occupier.Species.Equals(targetList_[i]))
                return true;
        }

        return false;
    }

    public virtual bool UnitInPredatorList(GameCharacter igc)
    {
        for (int i = 0; i < predatorList_.Count; i++)
        {
            if (igc.Species.Equals(predatorList_[i]))
                return true;
        }

        return false;
    }

    public virtual bool UnitInTargetList(GameCharacter igc)
    {
        for (int i = 0; i < targetList_.Count; i++)
        {
            if (igc.Species.Equals(targetList_[i]))
                return true;
        }

        return false;
    }


    // ---------------------------------------------------------------------------------------
    /*                                    SENSOR METHODS                                    */
    // ---------------------------------------------------------------------------------------

    public List<float> AdjacencySensor()
    {
        List<float> adjacencySensor = new List<float>();

        HexTile currentTile = BattleMap.Instance.mapTiles[InGamePosition];
        HexTile neighbor;

        for (int dir = 0; dir < 6; dir++)
        {
            // if there is a neighboring tile at dir
            if (currentTile.Neighbors.TryGetValue(dir, out neighbor))
            {
                if (neighbor.Occupied)
                {
                    // if tile is occupied by an enemy or prey
                    if (OccupierInTargetList(neighbor))
                    {
                        adjacencySensor.Add(1f);            // target at dir
                    }
                    else if (OccupierInPredatorList(neighbor))
                    {
                        adjacencySensor.Add(2f);
                    }
                    else
                    {
                        adjacencySensor.Add(-1f);           // no target at dir, but not empty
                    }
                }
                else
                {
                    adjacencySensor.Add(0f);                // empty tile
                }
            }
            else
            {
                adjacencySensor.Add(-1f);                   // obstacle tile
            }
        }

        return adjacencySensor;
    }

    public List<GameCharacter> PredatorsInSightSensor()                // ------------------------ TODO: Sight perception sensor implementation
    {
        List<GameCharacter> predators = new List<GameCharacter>();

        foreach (GameCharacter igc in BattleMap.Instance.battleUnits_)
        {
            if (UnitInPredatorList(igc))
            {
                predators.Add(igc);
            }
        }

        return predators;
    }

    public List<GameCharacter> ObjectivesInSightSensor()              // ------------------------ TODO: Sight perception sensor implementation
    {
        List<GameCharacter> objectives = new List<GameCharacter>();

        foreach (GameCharacter igc in BattleMap.Instance.battleUnits_)
        {
            if (UnitInTargetList(igc))
            {
                objectives.Add(igc);
            }
        }

        return objectives;
    }
}

public abstract class Canis : Enemy
{
    public Canis() : base(new string[] { "Player", "Leporidae" }, new string[] { "Abomination" })
    {
        Species = "Canis";
    }

    // ---------------------------------------------------------------------------------------
    /*                                    SENSOR METHODS                                    */
    // ---------------------------------------------------------------------------------------
}

public abstract class Leporidae : Enemy
{
    public Leporidae() : base(new string[] { "Player" }, new string[] { "Canis" })
    { 
        Species = "Leporidae";
    }

    // ---------------------------------------------------------------------------------------
    /*                                    SENSOR METHODS                                    */
    // ---------------------------------------------------------------------------------------
}
