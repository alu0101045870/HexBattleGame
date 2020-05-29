using UnityEngine;
using MLAgents;
using System.Collections;
using System.Collections.Generic;
using MLAgents.Sensors;

public class Lupus : Agent, IGameCharacter, IEnemy, ICanid
{
    // ---------------------------------------------------------------------------------------
    /*                                      AGENT DATA                                      */
    // ---------------------------------------------------------------------------------------

    private string name_ = "Lupus";

    private Canid inheritedComponent_;

    private List<GameObject> preyList_ = new List<GameObject>();
    private List<GameObject> playerList_ = new List<GameObject>();

    // Action variables
    private List<GameObject> targets_ = new List<GameObject>();
    private Skill usedSkill_;

    // ---------------------------------------------------------------------------------------
    /*                              INTERFACE IMPLEMENTATION                                */
    // ---------------------------------------------------------------------------------------

    public int TickSpeed {
        get { return inheritedComponent_.TickSpeed; }
        set { inheritedComponent_.TickSpeed = value;  }
    }
    public int CounterValue
    {
        get { return inheritedComponent_.CounterValue; }
        set { inheritedComponent_.CounterValue = value; }
    }
    public int LP
    {
        get { return inheritedComponent_.LP; }
        set { inheritedComponent_.LP = value; }
    }
    public int LastSkillRank
    {
        get { return inheritedComponent_.LastSkillRank; }
        set { inheritedComponent_.LastSkillRank = value; }
    }
    public string Name {
        get { return name_; }
        set { 
            name_ = value;
            GetGameObject().name = value;
        }
    }
    public List<Skill> Skillset
    {
        get { return inheritedComponent_.Skillset; }
        set { inheritedComponent_.Skillset = value; }
    }
    public bool HasMoved { 
        get { return inheritedComponent_.HasMoved; } 
        set { inheritedComponent_.HasMoved = value; } 
    }

    public float GetStatusEffectByName(string name)
    {
        return inheritedComponent_.GetStatusEffectByName(name);
    }
    public void SetStatusEffectByName(string name, float value)
    {
        inheritedComponent_.SetStatusEffectByName(name, value);
    }
    public int GetStatValueByName(string name)
    {
        return inheritedComponent_.GetStatValueByName(name);
    }
    public void SetStatValueByName(string name, int value)
    {
        inheritedComponent_.SetStatValueByName(name, value);
    }
    public void SetGeneralTag(GameObject go)
    {
        inheritedComponent_.SetGeneralTag(go);
    }
    public void SetInGamePosition(GameObject go, int posX, int posY)
    {
        inheritedComponent_.SetInGamePosition(go, posX, posY);
    }
    public Vector2Int GetInGamePosition()
    {
        return inheritedComponent_.GetInGamePosition();
    }
    public GameObject GetGameObject()
    {
        return gameObject;
    }
    public string GetFamily()
    {
        return inheritedComponent_.GetType().Name;
    }
    public void DetectUnitsOfInterest()
    {
        // Interest in: 
        // Player -> Enemy
        // Leporidae -> Prey

        playerList_.AddRange(GameObject.FindGameObjectsWithTag("Player"));
        preyList_.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));

        for (int i = 0; i < preyList_.Count; i++)
        {
            IGameCharacter prey = preyList_[i].GetComponent<IGameCharacter>();

            if (!InPreyList(prey))
            {
                preyList_.RemoveAt(i);
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    /*                            AGENT ACTIONS IMPLEMENTATION                              */
    // ---------------------------------------------------------------------------------------

    public override void CollectObservations(VectorSensor sensor)
    {
        //

        // Observations needed:
        /*
            - Some sort of knowledge of other units positions
                + Could be an array indicating the hex distance between us
                + Maybe filter out the non-relevant ones

            - Have I already used my movement?   [BOOLEAN]
            - My Status Effects
            - My Stat Values
            
            - My Position ????
         
         
         */
    }

    public override float[] Heuristic()
    {
        var action = new float[11];     // [0] -> used skill // [1 - 10] -> targets

        // --------------------------------------------


        for (int i = 0; i < Skillset.Count; i++)
        {
            if (Skillset[i].AreTargetsInRange(GetInGamePosition(), preyList_, targets_))
            { 

                break;
            }
            else if (Skillset[i].AreTargetsInRange(GetInGamePosition(), playerList_, targets_))
            {

                break;
            }

        }

        // check for movement last, as it is the least rewarding one

        return action;
    }

    public void RequestAct()
    {
        RequestAction();
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // 1. set usedSkill_
        usedSkill_ = new Defend();
        // 2. set targets_


        // 3. call usedSkill_.Exec with targets 

        StartCoroutine(usedSkill_.Exec(this, targets_));
    }

    // ---------------------------------------------------------------------------------------
    /*                            CLASS METHODS IMPLEMENTATION                              */
    // ---------------------------------------------------------------------------------------

    public override void Initialize()
    {
        inheritedComponent_ = new Canid();
        inheritedComponent_.Init();

        Name = name_;
        SetGeneralTag(gameObject);

        // Stat initialization
        // LP & Stat Values
        SetStatValues(74, 7, 1, 1, 1, 2, 2, 59, 0);
        // Status effects
        SetStatusEffects(1, 1, 1, 1, 1, 1);

        // Initialize SkillSet
        Skillset.Add(new Bite());
        //Skillset.Add(new Move(GetStatValueByName("MOV")));

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;

        HasMoved = false;
    }

    private bool InPreyList(IGameCharacter potentialPrey)
    {
        return (potentialPrey.GetFamily().Equals("Leporidae"));
    }

    private void SetStatValues(int lps, int str, int mag, int res, int m_res, int act, int mov, int agl, int acc)
    {
        LP = lps;
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
}

