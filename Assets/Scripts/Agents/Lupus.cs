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
        InitStatValues();                               // Stat initialization
        InitStatusEffects();
        SetParameters();

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;
    }

    // ---------------------------------------------------------------------------------------
    /*                                      PROPERTIES                                      */
    // ---------------------------------------------------------------------------------------

    public override void SetParameters()
    {
        //
        SetStatValues(74, 7, 1, 1, 1, 2, 2, 59, 0);
        SetStatusEffects(1, 1, 1, 1, 1, 1);
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

        if (StatusEffects.Count == 0 && StatValues.Count == 0)
        {
            InitAgent();
            Debug.Log(Academy.Instance.EpisodeCount);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(GetStatValueByName("HP"));

        sensor.AddObservation(AdjacencySensor());

        //sensor.AddObservation(ProximitySensor());
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
    /*                                  AGENT SENSOR METHODS                                */
    // ---------------------------------------------------------------------------------------

    /// <summary>
    ///     Utilization of hex mathematics to deduce general direction towards a certain tile
    /// </summary>
    /// <returns>
    ///     A suboptimal movement dir towards closest target
    /// </returns>
    private int ChaseDir()                                            // ---------------------- TODO: Implemetation for several objective scenarios
    {
        List<GameCharacter> detectedEnemies = ObjectivesInSightSensor();
        HexTile currentTile = BattleMap_.mapTiles[InGamePosition];

        //  TODO: Which is my most desirable prey?
        int dir = HexCalculator.GeneralDirectionTowards(this.InGamePosition, detectedEnemies[0].InGamePosition);

        return dir;
    }

    /// <summary>
    /// Checks one tile ahead in every direction in search for enemies
    /// </summary>
    /// <returns> Direction the enemy is in [0, 5]. If none were found, returns -1.</returns>
    private int TargetInRange()
    {
        HexTile currentTile = BattleMap_.mapTiles[InGamePosition];
        HexTile neighbor;

        for (int i = 0; i < 6; i++)
        {
            // if tile is occupied by an enemy or prey
            if (currentTile.Neighbors.TryGetValue(i, out neighbor))
            {

                if (OccupierInTargetList(neighbor))            // => Extract method (generalization)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    // ---------------------------------------------------------------------------------------
    /*                                  AGENT ACTION METHODS                                */
    // ---------------------------------------------------------------------------------------

    void Attack(int dir)
    {
        // Calculate damage on target
        GameCharacter target = BattleMap_.mapTiles[HexCalculator.GetNeighborAtDir(InGamePosition, dir)].Occupier;
        float damageApplied;

        if (target != null)
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

        if (BattleMap_.mapTiles[InGamePosition].Neighbors.TryGetValue(dir, out destinationTile))
        {
            if (!destinationTile.Occupied)
            {
                destination = destinationTile.Position;
                gameObject.GetComponent<Rigidbody>().position = HexCalculator.CharacterPosition(destination);

                BattleMap_.mapTiles[InGamePosition].EmptyTile();

                InGamePosition = destination;
                destinationTile.Occupier = this;

                // -------------------------------
                //Debug.Log(Name + " moved " + dir + "!");
            }
        }
        else
        {
            Debug.Log(Name + " could NOT move!");

            AddReward(-1f);
        }
    }

    void Defend(int dir)
    {
        //Debug.Log(Name + " defended!");
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

        if (GetStatValueByName("HP") <= 0)
        {
            Caroussel_.actionInfo.WhoDied_.Add(ID);
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
