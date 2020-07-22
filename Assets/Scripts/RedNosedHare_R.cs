﻿using MLAgents;
using MLAgents.Sensors;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedNosedHare_R : Agent, IGameCharacter
{
    private string species_ = "Leporidae";
    private string name_ = "RedNosedHare";
    private Vector2Int ingame_position_ = new Vector2Int();

    private int tickspeed_;
    private int counterValue_;
    private int hp_;
    private int lastSkillRank_;

    private Dictionary<string, int> statValues_ = new Dictionary<string, int>();                /* Range 0 - 255 */
    private Dictionary<string, float> statusEffects_ = new Dictionary<string, float>();         /* 0.5f - 1f - 2f */

    // ---------------------------------------------------------------------------------------
    /*                            INHERITED COMPONENT METHODS                               */
    // ---------------------------------------------------------------------------------------

    private void InitStatValues()
    {
        statValues_.Add("STR", 0);                  // Physical strength and damage
        statValues_.Add("MAG", 0);                  // Magic damage
        statValues_.Add("RES", 0);                  // Resistance to physical damage
        statValues_.Add("M.RES", 0);                // Resistance to magical damage
        statValues_.Add("ACT", 0);                  // Number of actions that the unit can take per turn
        statValues_.Add("MOV", 0);                  // How many hexagons can the unit move in one turn
        statValues_.Add("AGL", 0);                  // Agility, speed stat
        statValues_.Add("ACC", 0);                  // Accuracy, hit or miss
    }
    private void InitStatusEffects()
    {
        statusEffects_.Add("BRAVERY", 1f);          // Enhances or diminishes strength impact
        statusEffects_.Add("FAITH", 1f);            // Enhances or diminishes magic impact
        statusEffects_.Add("ARMOR", 1f);            // Enhances or diminishes resistance
        statusEffects_.Add("SHIELD", 1f);           // Enhances or diminishes magic resistance
        statusEffects_.Add("REGEN", 1f);            // Periodically restores LPs
        statusEffects_.Add("HASTE", 1f);            // Affects speed calculation
    }

    public string Species
    {
        get { return species_; }
        set { species_ = value; }
    }
    public string Name
    {
        get { return name_; }
        set
        {
            name_ = value;
            gameObject.name = value;
        }
    }
    public int TickSpeed
    {
        get { return tickspeed_; }
        set { tickspeed_ = value; }
    }
    public int CounterValue
    {
        get { return counterValue_; }
        set { counterValue_ = value; }
    }
    public int HP
    {
        get { return hp_; }
        set { hp_ = value; }
    }
    public int LastSkillRank
    {
        get { return lastSkillRank_; }
        set { lastSkillRank_ = value; }
    }
    public Vector2Int InGamePosition
    {
        get { return ingame_position_; }
        set { ingame_position_ = value; }
    }

    // ---------------------------------------------------------------------------------------

    public float GetStatusEffectByName(string name)
    {
        float value;

        if (!statusEffects_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value;
    }
    public void SetStatusEffectByName(string name, float value)
    {
        if (!statusEffects_.ContainsKey(name))
            throw new KeyNotFoundException();

        statusEffects_[name] = value;
    }

    public int GetStatValueByName(string name)
    {
        int value;

        if (!statValues_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value;
    }
    public void SetStatValueByName(string name, int value)
    {
        if (!statValues_.ContainsKey(name))
            throw new KeyNotFoundException();

        statValues_[name] = value;
    }

    private void SetStatValues(int lps, int str, int mag, int res, int m_res, int act, int mov, int agl, int acc)
    {
        hp_ = lps;
        SetStatValueByName("STR", str);
        SetStatValueByName("MAG", mag);
        SetStatValueByName("RES", res);
        SetStatValueByName("M.RES", m_res);
        SetStatValueByName("ACT", act);
        SetStatValueByName("MOV", mov);
        SetStatValueByName("AGL", agl);
        SetStatValueByName("ACC", acc);
    }

    private void SetStatusEffects(float bravery, float faith, float armor, float shield, float regen, float haste)
    {
        SetStatusEffectByName("BRAVERY", bravery);
        SetStatusEffectByName("FAITH", faith);
        SetStatusEffectByName("ARMOR", armor);
        SetStatusEffectByName("SHIELD", shield);
        SetStatusEffectByName("REGEN", regen);
        SetStatusEffectByName("HASTE", haste);
    }

    // ---------------------------------------------------------------------------------------
    /*                            AGENT ACTIONS IMPLEMENTATION                              */
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

        SetStatValues(78, 6, 2, 1, 10, 2, 2, 65, 0);
        SetStatusEffects(1, 1, 1, 1, 1, 1);

        //Skills = new List<Skill> { new Bite(), new Move(), new Defend() };

        // TickSpeed & LastSkillRank (default 3)
        tickspeed_ = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        lastSkillRank_ = 3;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Own HP       => Add in later versions
        // Actions

        // target in range (line)
        //      - for each different skill range

        sensor.AddObservation(1f);

    }

    public override float[] Heuristic()
    {
        // [skill - direction] 
        float[] action = new float[2];

        int dir = TargetInRange();

        if (dir != -1)
        {
            action[0] = 0f;
            action[1] = dir;
        }
        else
        {
            action[0] = 1f;
            action[1] = Random.Range(0, 5);         // Random dir
        }

        return action;
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
    }

    // ---------------------------------------------------------------------------------------
    /*                                  AGENT ACTION METHODS                                */
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Checks one tile ahead in every direction in search for enemies
    /// </summary>
    /// <returns> Direction the enemy is in [0, 5]. If none were found, returns -1.</returns>
    private int TargetInRange()
    {
        HexTile currentTile = BattleMap_R.Instance.mapTiles[ingame_position_];

        return -1;
    }

    void Attack(int dir)
    {
        Debug.Log(Name + "Attacked " + dir + "!");

    }

    void Move(int dir)
    {
        // First, check if movement is possible
        // - Does the destination tile exist?
        // - Is it free?
        HexTile destinationTile;
        Vector2Int destination;

        if (BattleMap_R.Instance.mapTiles[ingame_position_].Neighbors.TryGetValue(dir, out destinationTile))
        {
            if (!destinationTile.Occupied)
            {
                destination = destinationTile.Position;
                gameObject.GetComponent<Rigidbody>().MovePosition(HexCalculator.CharacterPosition(destination));

                destinationTile.OccupiedBy = this;
                this.gameObject.transform.parent = destinationTile.transform;

                BattleMap_R.Instance.mapTiles[ingame_position_].EmptyTile();
                ingame_position_ = destination;

                // -----------------------------------
                Debug.Log(Name + " moved " + dir + "!");
            }
        }
        else
        {
            Debug.Log(Name + " could NOT move!");
        }
    }

    void Defend(int dir)
    {
        Debug.Log(Name + " defended!");
    }
}
