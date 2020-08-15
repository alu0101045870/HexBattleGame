using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class Enemy : GameCharacter
{
    protected List<string> targetList_ = new List<string>();
    protected List<string> predatorList_ = new List<string>();

    public Enemy(IEnumerable<string> targetList, IEnumerable<string> predatorList)
    {
        targetList_.AddRange(targetList);
        predatorList_.AddRange(predatorList);
    }

    public override bool OccupierInPredatorList(HexTile neighbor)
    {
        for (int i = 0; i < predatorList_.Count; i++)
        {
            if (neighbor.Occupier.Species.Equals(predatorList_[i]))
                return true;
        }

        return false;
    }

    public override bool OccupierInTargetList(HexTile neighbor)
    {
        for (int i = 0; i < predatorList_.Count; i++)
        {
            if (neighbor.Occupier.Species.Equals(targetList_[i]))
                return true;
        }

        return false;
    }

    public override bool UnitInPredatorList(IGameCharacter igc)
    {
        for (int i = 0; i < predatorList_.Count; i++)
        {
            if (igc.Species.Equals(predatorList_[i]))
                return true;
        }

        return false;
    }

    public override bool UnitInTargetList(IGameCharacter igc)
    {
        for (int i = 0; i < predatorList_.Count; i++)
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

        HexTile currentTile = BattleMap_R.Instance.mapTiles[InGamePosition];
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

    public List<IGameCharacter> PredatorsInSightSensor()                // ------------------------ TODO: Sight perception sensor implementation
    {
        List<IGameCharacter> predators = new List<IGameCharacter>();

        foreach (IGameCharacter igc in BattleMap_R.Instance.battleUnits_)
        {
            if (UnitInPredatorList(igc))
            {
                predators.Add(igc);
            }
        }

        return predators;
    }

    private List<IGameCharacter> ObjectivesInSightSensor()              // ------------------------ TODO: Sight perception sensor implementation
    {
        List<IGameCharacter> objectives = new List<IGameCharacter>();

        foreach (IGameCharacter igc in BattleMap_R.Instance.battleUnits_)
        {
            if (UnitInTargetList(igc))
            {
                objectives.Add(igc);
            }
        }

        return objectives;
    }
}

public class Canis : Enemy
{
    public Canis(IEnumerable<string> targetList, IEnumerable<string> predatorList) : base(targetList, predatorList)
    {
        Species = "Canis";
    }

    // ---------------------------------------------------------------------------------------
    /*                                    SENSOR METHODS                                    */
    // ---------------------------------------------------------------------------------------
}

public class Leporidae : Enemy
{
    public Leporidae() : base(new string[] { "Player" }, new string[] { "Canis" })
    { 
        Species = "Leporidae";
    }

    // ---------------------------------------------------------------------------------------
    /*                                    SENSOR METHODS                                    */
    // ---------------------------------------------------------------------------------------
}


public class RedNosedHare : Unity.MLAgents.Agent, IGameCharacter
{
    private Leporidae inheritedComponent_ = new Leporidae();

    public event Action<float> OnHealthChanged = delegate { };

    // ---------------------------------------------------------------------------------------
    /*                                      PROPERTIES                                      */
    // ---------------------------------------------------------------------------------------

    public string Species
    {
        get => inheritedComponent_.Species;
        set => inheritedComponent_.Species = value;
    }
    public string Name
    {
        get
        {
            return inheritedComponent_.Name;
        }
        set
        {
            inheritedComponent_.Name = value;
            gameObject.name = value;
        }
    }
    public int ID
    {
        get => inheritedComponent_.ID;
        set => inheritedComponent_.ID = value;
    }
    public int TickSpeed
    {
        get => inheritedComponent_.TickSpeed;
        set => inheritedComponent_.TickSpeed = value;
    }
    public int CounterValue
    {
        get => inheritedComponent_.CounterValue;
        set => inheritedComponent_.CounterValue = value;
    }
    public int MaxHP
    {
        get => inheritedComponent_.MaxHP;
        set => inheritedComponent_.MaxHP = value;
    }
    public int LastSkillRank
    {
        get => inheritedComponent_.LastSkillRank;
        set => inheritedComponent_.LastSkillRank = value;
    }
    public Vector2Int InGamePosition
    {
        get => inheritedComponent_.InGamePosition;
        set => inheritedComponent_.InGamePosition = value;
    }
    public Dictionary<string, int> StatValues
    {
        get => inheritedComponent_.StatValues;
    }
    public Dictionary<string, float> StatusEffects
    {
        get => inheritedComponent_.StatusEffects;
    }

    public bool IsActive
    {
        get => inheritedComponent_.IsActive;
        set => inheritedComponent_.IsActive = value;
    }
    public bool ActionOver
    {
        get => inheritedComponent_.ActionOver;
        set => inheritedComponent_.ActionOver = value;
    }

    public GameObject GameObject => gameObject;

    // ---------------------------------------------------------------------------------------
    /*                                    STATS/STATUS                                      */
    // ---------------------------------------------------------------------------------------

    public void InitStatValues()
    {
        inheritedComponent_.InitStatValues();
    }
    public void InitStatusEffects()
    {
        inheritedComponent_.InitStatusEffects();
    }

    public void SetStatusEffectByName(string name, float value)
    {
        inheritedComponent_.SetStatusEffectByName(name, value);
    }
    public void SetStatValueByName(string name, int value)
    {
        SetStatValueByName(name, value);
    }

    public float GetStatusEffectByName(string name)
    {
        return inheritedComponent_.GetStatusEffectByName(name);
    }
    public int GetStatValueByName(string name)
    {
        return inheritedComponent_.GetStatValueByName(name);
    }

    public void SetStatValues(int lps, int str, int mag, int res, int m_res, int act, int mov, int agl, int acc)
    {
        inheritedComponent_.SetStatValues(lps, str, mag, res, m_res, act, mov, agl, acc);
    }
    public void SetStatusEffects(float bravery, float faith, float armor, float shield, float regen, float haste)
    {
        inheritedComponent_.SetStatusEffects(bravery, faith, armor, shield, regen, haste);
    }

    //

    private void SetStatsAndStatus()
    {
        SetStatValues(78, 6, 2, 21, 10, 1, 2, 65, 0);
        SetStatusEffects(1, 1, 1, 1, 1, 1);
    }

    // ---------------------------------------------------------------------------------------
    /*                              AGENT ACTIONS IMPLEMENTATION                            */
    // ---------------------------------------------------------------------------------------

    public void RequestAct()
    {
        RequestDecision();
    }

    public override void Initialize()
    {
        gameObject.tag = "Enemy";

        InitStatValues();                               // Stat initialization
        InitStatusEffects();
        SetStatsAndStatus();

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;

        Debug.Log(GetStatValueByName("AGL"));
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        if (StatusEffects.Count == 0 && StatValues.Count == 0)
            LazyInitialize();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(GetStatValueByName("HP"));

        sensor.AddObservation(inheritedComponent_.AdjacencySensor());
    }

    public override void Heuristic(float[] action)
    {

        int dir = TargetInRange();

        if (GetStatValueByName("HP") < (Mathf.RoundToInt(MaxHP * 0.35f)))
        {
            action[0] = -1f;
            action[1] = 2f;         // Defend when hp drops under a certain threshold
        }
        if (dir != -1)
        {
            action[0] = 0f;
            action[1] = dir;
        }
        else
        {
            action[0] = 1f;
            dir = RunnawayDir();

            if (dir != -1)
                action[1] = dir;
            else
                action[1] = UnityEngine.Random.Range(0, 5);
        }

    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // Exec Skill with given direction

        switch ((int)vectorAction[0])
        {
            case 0: // Attack
                {
                    Attack((int)vectorAction[1]);

                    break;
                }
            case 1: // Move
                {
                    Move((int)vectorAction[1]);
                    break;
                }
            default:
                {
                    Defend((int)vectorAction[1]);
                    break;
                }
        }

        AddReward(-0.1f);

        ActionOver = true;
    }

    public void Reset()
    {
        IsActive = false;
        gameObject.SetActive(false);

        Debug.Log(Unity.MLAgents.Academy.Instance.EpisodeCount);
        EndEpisode();
    }

    // ---------------------------------------------------------------------------------------
    /*                                  AGENT SENSOR METHODS                                */
    // ---------------------------------------------------------------------------------------

    private int TargetInRange()                                         // ------------------------ TODO: Actual implementation vs player scenarios
    {
        HexTile currentTile = BattleMap_R.Instance.mapTiles[InGamePosition];

        return -1;
    }

    private List<IGameCharacter> PredatorsInSightSensor()               // ------------------------ TODO: Sight perception sensor implementation
    {
        List<IGameCharacter> predators = new List<IGameCharacter>();

        foreach (IGameCharacter igc in BattleMap_R.Instance.battleUnits_)
        {
            if (igc.Species == "Canis")
            {
                predators.Add(igc);
            }
        }

        return predators;
    }

    private int RunnawayDir()                                            // ---------------------- TODO: Implemetation for several predator scenarios
    {
        List<IGameCharacter> detectedEnemies = PredatorsInSightSensor();
        HexTile currentTile = BattleMap_R.Instance.mapTiles[InGamePosition];
        HexTile neighbor;

        //
        List<int> oppositedir = HexCalculator.OppositeDir(HexCalculator.GeneralDirectionTowards(this.InGamePosition, detectedEnemies[0].InGamePosition));

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

    // ---------------------------------------------------------------------------------------
    /*                                  AGENT ACTION METHODS                                */
    // ---------------------------------------------------------------------------------------

    void Attack(int dir)
    {
        Debug.Log(Name + "Attacked " + dir + "!");

        IGameCharacter target = BattleMap_R.Instance.mapTiles[HexCalculator.GetNeighborAtDir(InGamePosition, dir)].Occupier;
        float damageApplied;

        if (target != null && inheritedComponent_.UnitInTargetList(target))
        {
            damageApplied = StatCalculator.PhysicalDmgCalc(GetStatValueByName("STR"), 16, target.GetStatValueByName("RES"));
            //Debug.Log(Name + " did " + damageApplied + "damage!");
            target.ReceiveDamage(damageApplied);

        }
        else
        {
            //Debug.Log(Name + " attacked " + dir + "and failed!");

            AddReward(-1f);
        }

    }

    void Move(int dir)
    {
        // First, check if movement is possible
        // - Does the destination tile exist?
        // - Is it free?
        HexTile destinationTile;
        Vector2Int destination;

        if (BattleMap_R.Instance.mapTiles[InGamePosition].Neighbors.TryGetValue(dir, out destinationTile))
        {
            if (!destinationTile.Occupied)
            {
                destination = destinationTile.Position;
                gameObject.GetComponent<Rigidbody>().MovePosition(HexCalculator.CharacterPosition(destination));
                BattleMap_R.Instance.mapTiles[InGamePosition].EmptyTile();

                InGamePosition = destination;
                destinationTile.Occupier = this;

                // -------------------------------
                //Debug.Log(Name + " moved " + dir + "!");
            }
        }
        else
        {
            // Debug.Log(Name + " could NOT move!");
            AddReward(-1f);
        }
    }

    void Defend(int dir)
    {
        Debug.Log(Name + " defended!");
    }

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------

    public void ReceiveDamage(float amount)
    {
        //Debug.Log(Name + "'s MaxHP: " + MaxHP + " - damage taken: " + amount);

        // Get the new health percentage left on target
        SetStatValueByName("HP", GetStatValueByName("HP") - (int)amount);
        float percentageLeft = Mathf.Clamp((float)GetStatValueByName("HP"), 0, MaxHP) / (float)MaxHP;

        // Update calling target's healthbar delegate
        OnHealthChanged(percentageLeft);

        if (GetStatValueByName("HP") < 0)
        {
            Caroussel_R.Instance.actionInfo.WhoDied_.Add(ID);
        }
    }

    public void Die()
    {
        IsActive = false;
        gameObject.SetActive(false);
        SetReward(-5f);
    }

    public void ResetStats()
    {
        SetStatsAndStatus();

        OnHealthChanged(100);
    }
}