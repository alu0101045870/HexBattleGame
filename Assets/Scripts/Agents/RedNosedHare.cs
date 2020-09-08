using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class RedNosedHare : Leporidae
{
    void Awake()
    {
        Name = "RedNosedHare";
    }

    // ---------------------------------------------------------------------------------------
    /*                                      PROPERTIES                                      */
    // ---------------------------------------------------------------------------------------

    public override void SetParameters()
    {
        if (StatValues.Count <= 0 && StatusEffects.Count <= 0)
        {
            InitStatValues();                               // Stat initialization
            InitStatusEffects();
        }

        SetStatValues(78, 3, 2, 21, 10, 1, 1, 65, 0);
        SetStatusEffects(1, 1, 1, 1, 1, 1);

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;

        // settings based on environment parameters
        // ...
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
        //Debug.Log(Academy.Instance.EpisodeCount);  
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

    // No additional skills

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------
}