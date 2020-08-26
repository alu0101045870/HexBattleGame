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

    public override void Initialize()
    {
        base.Initialize();

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
                action[1] = HexCalculator.RandomDir();
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // Exec chosen Skill with given direction
        skills_[(int)vectorAction[0]].Invoke((int)vectorAction[1]);

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

    public override void ResetStats()
    {
        SetParameters();

        // Healthbar is actually "independent" from HP parameter 
        // so it needs a reset too
        OnHealthChanged(100);
    }

}
