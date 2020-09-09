using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;
using System;

public class MatriarchHare : Leporidae
{
    void Awake()
    {
        Name = "MatriarchHare";
    }

    // ---------------------------------------------------------------------------------------
    /*                                      PROPERTIES                                      */
    // ---------------------------------------------------------------------------------------

    public override void SetParameters()
    {
        // Add unique skills
        skills_.Add(new Tuple<Action<int>, int>(MothersEmbrace, 4));

        if (StatValues.Count <= 0 && StatusEffects.Count <= 0)
        {
            InitStatValues();                               // Stat initialization
            InitStatusEffects();
        }

        SetStatValues(128, 7, 2, 21, 10, 2, 2, 65, 0);
        SetStatusEffects(1, 1, 1, 1, 1, 1);

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;

        // settings based on environment parameters
        // environmentParameters.GetWithDefault("matriarch_skillset", 0.0f);
    }

    // ---------------------------------------------------------------------------------------
    /*                              AGENT ACTIONS IMPLEMENTATION                            */
    // ---------------------------------------------------------------------------------------

    public override void Initialize()
    {
        base.Initialize();

        environmentParameters = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        if (BattleMap_ != null && BattleMap_.envDone)
            SetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(GetStatValueByName("HP"));

        sensor.AddObservation(AdjacencySensor());

        sensor.AddObservation(ProximitySensor());

        sensor.AddObservation(DistanceTowardsClosestPredator());

        sensor.AddObservation(NumberOfLivingAllies());
    }

    public override void Heuristic(float[] action)
    {
        int attackDir = TargetInRange();
        int protectDir = AllyInRange();

        if (protectDir != -1)           // Prioritizes children protection
        {
            action[0] = protectDir;
            action[1] = 3f;
        }
        else if (GetStatValueByName("HP") < (Mathf.RoundToInt(MaxHP * 0.35f)))
        {
            action[0] = -1f;
            action[1] = 2f;         // Defend when hp drops under a certain threshold
        }
        else if (attackDir != -1)
        {
            action[0] = 0f;
            action[1] = attackDir;
        }
        else
        {
            action[0] = 1f;
            attackDir = RunnawayDir();

            if (attackDir != -1)
                action[1] = attackDir;
            else
                action[1] = HexCalculator.RandomDir();
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // Exec chosen Skill with given direction
        skills_[(int)vectorAction[0]].Item1.Invoke((int)vectorAction[1]);
        skillRanks.Add(skills_[(int)vectorAction[0]].Item2);

        AddReward(-0.1f);
        ActionOver = true;
    }

    // ---------------------------------------------------------------------------------------
    /*                                   AGENT UNIQUE SKILLS                                */
    // ---------------------------------------------------------------------------------------

    void MothersEmbrace (int dir)
    {
        GameCharacter ally;
        HexTile neighborTile;

        if (BattleMap_.mapTiles.TryGetValue(HexCalculator.GetNeighborAtDir(InGamePosition, dir), out neighborTile))
        {
            ally = BattleMap_.mapTiles[HexCalculator.GetNeighborAtDir(InGamePosition, dir)].Occupier;

            if (ally != null)
            {
                ally.ApplyStatusEffect("ARMOR", 1.5f);

                if (!UnitInFaction(ally))
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

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------
}
