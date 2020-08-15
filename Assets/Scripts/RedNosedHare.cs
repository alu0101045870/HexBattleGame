﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class RedNosedHare : Leporidae
{
    public event Action<float> OnHealthChanged = delegate { };

    // ---------------------------------------------------------------------------------------
    /*                                      PROPERTIES                                      */
    // ---------------------------------------------------------------------------------------

    public override void SetParameters()
    {
        SetStatValues(78, 6, 2, 21, 10, 1, 2, 65, 0);
        SetStatusEffects(1, 1, 1, 1, 1, 1);
    }

    // ---------------------------------------------------------------------------------------
    /*                              AGENT ACTIONS IMPLEMENTATION                            */
    // ---------------------------------------------------------------------------------------

    public override void RequestAct()
    {
        RequestDecision();
    }

    public override void Initialize()
    {
        gameObject.tag = "Enemy";

        InitStatValues();                               // Stat initialization
        InitStatusEffects();
        SetParameters();

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

        sensor.AddObservation(AdjacencySensor());
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

    public override void Reset()
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

        if (target != null && UnitInTargetList(target))
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
                //destinationTile.Occupier = this;

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

    public override void ReceiveDamage(float amount)
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

    public override void Die()
    {
        IsActive = false;
        gameObject.SetActive(false);
        SetReward(-5f);
    }

    public override void ResetStats()
    {
        SetParameters();

        OnHealthChanged(100);
    }
}