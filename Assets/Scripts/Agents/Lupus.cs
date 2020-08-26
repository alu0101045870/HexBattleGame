using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lupus : Canis, IGameChar
{
    public event Action<float> OnHealthChanged = delegate { };

    void Awake() 
    {
        Name = "Lupus";    
    }

    private void InitAgent()
    {
        SetParameters();
    }

    // ---------------------------------------------------------------------------------------
    /*                                      PROPERTIES                                      */
    // ---------------------------------------------------------------------------------------

    public override void SetParameters()
    {
        InitStatValues();                               // Stat initialization
        InitStatusEffects();

        SetStatValues(78, 7, 1, 21, 1, 2, 1, 59, 0);
        SetStatusEffects(1, 1, 1, 1, 1, 1);

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;
    }

    // ---------------------------------------------------------------------------------------
    /*                              AGENT ACTIONS IMPLEMENTATION                            */
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

        InitAgent();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        InitAgent();
        //Debug.Log(Academy.Instance.EpisodeCount);
        
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(GetStatValueByName("HP"));

        sensor.AddObservation(AdjacencySensor());

        sensor.AddObservation(ProximitySensor());

        sensor.AddObservation(DistanceTowardsClosestTarget());
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
            dir = ChaseDir();

            if (dir != -1)
                action[1] = dir;
            else
                action[1] = Random.Range(0, 5);
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

        //Debug.Log(Academy.Instance.EpisodeCount);
        EndEpisode();
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
                else
                    AddReward(1f);

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
        AddReward(-0.1f);
    }

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------

    public override void ReceiveDamage(float amount)
    {
        //Debug.Log(Name + "'s MaxHP: " + MaxHP + " - damage taken: " + amount);
        AddReward(-0.5f);

        // Get the new health percentage left on target
        SetStatValueByName("HP", GetStatValueByName("HP") - (int)amount);
        float percentageLeft = Mathf.Clamp((float)GetStatValueByName("HP"), 0, MaxHP) / (float)MaxHP;

        // Update calling target's healthbar delegate
        OnHealthChanged(percentageLeft);

        if (GetStatValueByName("HP") <= 0)
        {
            Die();
            AddReward(-5.0f);
        }
    }

    public override void Die()
    {
        Caroussel_.actionInfo.WhoDied_.Add(ID);
        BattleMap_.factions[FactionID][ID] = false;

        IsActive = false;
        gameObject.SetActive(false);
    }

    public override void Win()
    {
        AddReward(5f);
    }

    public override void ResetStats()
    {
        SetParameters();

        OnHealthChanged(100);
    }

}
