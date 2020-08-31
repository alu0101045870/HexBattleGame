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
        if (!igc.IsActive) 
            return false;

        for (int i = 0; i < predatorList_.Count; i++)
        {
            if (igc.Species.Equals(predatorList_[i]))
                return true;
        }

        return false;
    }

    public virtual bool UnitInTargetList(GameCharacter igc)
    {
        if (!igc.IsActive)
            return false;
     
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

    protected Vector2Int ClosestObjective(List<GameCharacter> detectedEnemies)
    {
        return HexCalculator.ClosestPosition(InGamePosition, detectedEnemies);
    }

    /// <summary>
    /// Checks one tile ahead in every direction in search for enemies
    /// </summary>
    /// <returns> Direction the enemy is in [0, 5]. If none were found, returns -1.</returns>
    protected int TargetInRange()
    {
        HexTile currentTile = BattleMap_.mapTiles[InGamePosition];
        HexTile neighbor;

        for (int i = 0; i < 6; i++)
        {
            // if tile is occupied by an enemy or prey
            if (currentTile.Neighbors.TryGetValue(i, out neighbor))
            {
                if (OccupierInTargetList(neighbor))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    ///     Utilization of hex mathematics to deduce general direction towards a certain tile
    /// </summary>
    /// <returns>
    ///     A suboptimal movement dir towards closest target
    /// </returns>
    protected int ChaseDir()
    {
        List<GameCharacter> detectedEnemies = ObjectivesInSightSensor();

        // Choose most desirable prey: proximity criteria
        if (detectedEnemies.Count <= 0)
            return -1;

        Vector2Int preyPosition = ClosestObjective(detectedEnemies);
        HexTile currentTile = BattleMap_.mapTiles[InGamePosition];
        HexTile neighbor;

        List<int> dirList = HexCalculator.ForwardDir(HexCalculator.GeneralDirectionTowards(this.InGamePosition, preyPosition));

        for (int i = 0; i < dirList.Count; i++)
        {
            if (currentTile.Neighbors.TryGetValue(dirList[i], out neighbor))
                if (!neighbor.Occupied)
                    return dirList[i];
        }

        // If all are occupied, "failed chasing"
        return -1;
    }

    protected int RunnawayDir()
    {
        List<GameCharacter> detectedEnemies = PredatorsInSightSensor(); //

        // Choose most inminent predator: proximity criteria
        if (detectedEnemies.Count <= 0)
            return -1;

        Vector2Int predPosition = ClosestObjective(detectedEnemies);
        HexTile currentTile = BattleMap_.mapTiles[InGamePosition];
        HexTile neighbor;

        List<int> oppositedir = HexCalculator.OppositeDir(HexCalculator.GeneralDirectionTowards(this.InGamePosition, predPosition));    //

        // Check directions available
        for (int i = 0; i < oppositedir.Count; i++)
        {
            if (currentTile.Neighbors.TryGetValue(oppositedir[i], out neighbor))
                if (!neighbor.Occupied)
                    return oppositedir[i];          // return most optimal escape route (if possible)
        }

        // If all are occupied, "failed escape"
        return -1;
    }

    protected List<float> AdjacencySensor()
    {
        List<float> adjacencySensor = new List<float>();

        HexTile currentTile = BattleMap_.mapTiles[InGamePosition];
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
                        adjacencySensor.Add(0.5f);            // target at dir
                    }
                    else if (OccupierInPredatorList(neighbor))
                    {
                        adjacencySensor.Add(1f);
                    }
                    else
                    {
                        adjacencySensor.Add(-0.5f);           // no target at dir, but not empty
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

    protected List<float> ProximitySensor()
    {
        List<float> proximitySensor = new List<float>(new float[]{ 0f, 0f, 0f, 0f, 0f, 0f});
        List<GameCharacter> detectedObjectives = ObjectivesInSightSensor();
        List<GameCharacter> detectedPredators = PredatorsInSightSensor();
        int dir;

        for (int i = 0; i < detectedObjectives.Count; i++)
        {
            dir = HexCalculator.GeneralDirectionTowards(InGamePosition, detectedObjectives[i].InGamePosition);
            proximitySensor[dir] += 1f;
        }

        for (int i = 0; i < detectedPredators.Count; i++)
        {
            dir = HexCalculator.GeneralDirectionTowards(InGamePosition, detectedPredators[i].InGamePosition);
            proximitySensor[dir] -= 1f;
        }

        return proximitySensor;
    }

    protected float DistanceTowardsClosestPredator()
    {
        List<GameCharacter> detectedEnemies = PredatorsInSightSensor(); //

        // Choose most inminent predator: proximity criteria
        if (detectedEnemies.Count <= 0)
            return -1;

        Vector2Int predPosition = ClosestObjective(detectedEnemies);

        return HexCalculator.OffsetDistance(InGamePosition, predPosition);
    }

    protected float DistanceTowardsClosestTarget()
    {
        List<GameCharacter> detectedEnemies = ObjectivesInSightSensor();

        // Choose most desirable prey: proximity criteria
        if (detectedEnemies.Count <= 0)
            return -1;

        Vector2Int preyPosition = ClosestObjective(detectedEnemies);

        return HexCalculator.OffsetDistance(InGamePosition, preyPosition);
    }

    protected List<GameCharacter> PredatorsInSightSensor()                // ------------------------ TODO: Sight perception sensor implementation
    {
        List<GameCharacter> predators = new List<GameCharacter>();

        foreach (GameCharacter igc in BattleMap_.battleUnits_)
        {
            if (UnitInPredatorList(igc))
            {
                predators.Add(igc);
            }
        }

        return predators;
    }

    protected List<GameCharacter> ObjectivesInSightSensor()              // ------------------------ TODO: Sight perception sensor implementation
    {
        List<GameCharacter> objectives = new List<GameCharacter>();

        foreach (GameCharacter igc in BattleMap_.battleUnits_)
        {
            if (UnitInTargetList(igc))
            {
                objectives.Add(igc);
            }
        }

        return objectives;
    }

    // ---------------------------------------------------------------------------------------
    /*                                     AGENT METHODS                                    */
    // ---------------------------------------------------------------------------------------

    public override void RequestAct()
    {
        ActionOver = false;
        RequestDecision();
    }

    public override void Initialize()
    {
        gameObject.tag = "Enemy";
        ActionOver = false;
    }

    public override void Reset()
    {
        IsActive = false;
        gameObject.SetActive(false);

        //Debug.Log(Unity.MLAgents.Academy.Instance.EpisodeCount);
        EndEpisode();
    }

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------

    public override void Die()
    {
        Caroussel_.actionInfo.WhoDied_.Add(ID);
        BattleMap_.factions[FactionID][ID] = false;

        IsActive = false;
        gameObject.SetActive(false);
        BattleMap_.mapTiles[InGamePosition].EmptyTile();
    }

    public override void Win()
    {
        AddReward(5f);
    }

    public override void Lose()
    {
        AddReward(-5f);
    }
}

public abstract class Canis : Enemy
{
    protected List<Action<int>> skills_ = new List<Action<int>>();

    public Canis() : base(new string[] { "Player", "Leporidae" }, new string[] { "Abomination" })
    {
        Species = "Canis";

        skills_.Add(Attack);        //[0]
        skills_.Add(Move);          //[1]
        skills_.Add(Defend);        //[2]
    }

    // ---------------------------------------------------------------------------------------
    /*                                  AGENT ACTION METHODS                                */
    // ---------------------------------------------------------------------------------------

    protected void Attack(int dir)
    {
        // Calculate damage on target
        const int ATTACK_DMG_CONSTANT = 16;
        GameCharacter target;
        HexTile neighborTile;
        float damageApplied;

        if (BattleMap_.mapTiles.TryGetValue(HexCalculator.GetNeighborAtDir(InGamePosition, dir), out neighborTile))
        {
            target = BattleMap_.mapTiles[HexCalculator.GetNeighborAtDir(InGamePosition, dir)].Occupier;

            if (target != null)
            {
                // Add Bravery check (*1.5 attack input)
                damageApplied = StatCalculator.PhysicalDmgCalc(GetStatValueByName("STR"), ATTACK_DMG_CONSTANT, target.GetStatValueByName("RES"));
                target.ReceiveDamage(damageApplied);

                if (!UnitInTargetList(target))
                    AddReward(-1f);
                else
                    AddReward(1f);

                return;
            }
        }

        // Position at dir has no occupier!
        // Position at dir has no tile!
        AddReward(-0.5f);
    }

    protected void Move(int dir)
    {
        // First, check if movement is possible
        // - Does the destination tile exist?
        // - Is it free?
        HexTile destinationTile;
        Vector2Int destination;

        if (BattleMap_.mapTiles[InGamePosition].Neighbors.TryGetValue(dir, out destinationTile))
        {
            if (!destinationTile.Occupied)
            {
                destination = destinationTile.Position;

                Vector3 igPosition = HexCalculator.CharacterPosition(destination);
                gameObject.GetComponent<Rigidbody>().position = igPosition;
                gameObject.transform.position = igPosition;

                BattleMap_.mapTiles[InGamePosition].EmptyTile();

                InGamePosition = destination;
                destinationTile.Occupier = this;

                return;
            }
        }

        // Position at dir has no tile to move at!
        // Position at dir is occupied!
        AddReward(-0.5f);
    }

    protected void Defend(int dir)
    {
        AddReward(-0.1f);
    }
    
    // ---------------------------------------------------------------------------------------
    /*                                    SENSOR METHODS                                    */
    // ---------------------------------------------------------------------------------------
}

public abstract class Leporidae : Enemy
{
    protected List<Action<int>> skills_ = new List<Action<int>>();

    public Leporidae() : base(new string[] { "Player" }, new string[] { "Canis" })
    { 
        Species = "Leporidae";

        skills_.Add(Attack);        //[0]
        skills_.Add(Move);          //[1]
        skills_.Add(Defend);        //[2]
    }

    // ---------------------------------------------------------------------------------------
    /*                                  AGENT ACTION METHODS                                */
    // ---------------------------------------------------------------------------------------

    void Attack(int dir)
    {
        // Calculate damage on target
        const int ATTACK_DMG_CONSTANT = 16;
        GameCharacter target;
        HexTile neighborTile;
        float damageApplied;

        if (BattleMap_.mapTiles.TryGetValue(HexCalculator.GetNeighborAtDir(InGamePosition, dir), out neighborTile))
        {
            target = BattleMap_.mapTiles[HexCalculator.GetNeighborAtDir(InGamePosition, dir)].Occupier;

            if (target != null)
            {
                // Add Bravery check (*1.5 attack input)
                damageApplied = StatCalculator.PhysicalDmgCalc(GetStatValueByName("STR"), ATTACK_DMG_CONSTANT, target.GetStatValueByName("RES"));
                target.ReceiveDamage(damageApplied);

                if (!UnitInTargetList(target))
                    AddReward(-1f);

                return;
            }
        }

        // Position at dir has no occupier!
        // Position at dir has no tile!
        AddReward(-0.5f);
    }

    void Move(int dir)
    {
        // First, check if movement is possible
        // - Does the destination tile exist?
        // - Is it free?
        HexTile destinationTile;
        Vector2Int destination;

        if (BattleMap_.mapTiles[InGamePosition].Neighbors.TryGetValue(dir, out destinationTile))
        {
            if (!destinationTile.Occupied)
            {
                destination = destinationTile.Position;

                Vector3 igPosition = HexCalculator.CharacterPosition(destination);
                gameObject.GetComponent<Rigidbody>().position = igPosition;
                gameObject.transform.position = igPosition;

                BattleMap_.mapTiles[InGamePosition].EmptyTile();

                InGamePosition = destination;
                destinationTile.Occupier = this;

                return;
            }
        }

        // Position at dir has no tile to move at!
        // Position at dir is occupied!
        AddReward(-0.5f);
    }

    void Defend(int dir)
    {
        // if (IsAdjacentTo("Leporidae"))
        //      Heal

        AddReward(-0.01f);
    }

    // ---------------------------------------------------------------------------------------
    /*                                    SENSOR METHODS                                    */
    // ---------------------------------------------------------------------------------------
}
