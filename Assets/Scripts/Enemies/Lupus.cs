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

    //private List<GameObject> preyList_ = new List<GameObject>();
    //private List<GameObject> playerList_ = new List<GameObject>();

    // Action variables
    private List<TargetInformation> targets_ = new List<TargetInformation>();

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
    public bool HasMoved { 
        get { return inheritedComponent_.HasMoved; } 
        set { inheritedComponent_.HasMoved = value; } 
    }
    public List<Skill> Skills
    {
        get { return inheritedComponent_.Skills; }
        set { inheritedComponent_.Skills = value; }
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
   
    // ---------------------------------------------------------------------------------------
    /*                            AGENT ACTIONS IMPLEMENTATION                              */
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

        Skills = new List<Skill> { new Bite(), new Move(), new Defend() };

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;

        HasMoved = false;
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
        float[] action = new float[2];

        // [0] Skill Index
        // [1] Direction in which the skill is aimed

        targets_.Clear();

        // Last two skills are Movement and Defend, which don't require target check for this heuristic approach
        for (int s_index = 0; s_index < Skills.Count - 2; s_index++)
        {
            foreach (IGameCharacter unit in BattleMap.Instance.turnCaroussel.GetBattleUnits())
            {
                if (!unit.Equals(this))
                {
                    int dir = HexCalculator.InLineRange(GetInGamePosition(), unit.GetInGamePosition(), Skills[s_index].Range);
                    
                    if (dir != -1)      // target in range at a certain dir!
                    {
                        targets_.Add(
                            new TargetInformation(unit, dir, HexCalculator.DistanceBetween(GetInGamePosition(), unit.GetInGamePosition()))
                        );
                    }
                }
            }

            if (targets_.Count > 0)
            {
                action[0] = s_index;
                break;
            }
        }
        
        if (targets_.Count > 1)                         // decide between targets and extract DIRECTION [1]
        {
            int target_index = 0;
            int min_dist = targets_[0].distance;

            for (int i = 1; i < targets_.Count; i++)
            {
                if (min_dist > targets_[i].distance)
                {
                    target_index = i;
                    min_dist = targets_[i].distance;
                }
            }

            action[1] = targets_[target_index].dir;
        }
        else if (targets_.Count == 1)                   // only one possible target
        {
            action[1] = targets_[0].dir;
        }
        else if(HasMoved)                                         
        {
            // if already moved, defend
            action[0] = Skills.Count - 1;
            action[1] = -1;
        }
        else
        {
            // Effectively, Move in a random direction
            action[0] = Skills.Count - 2;
            action[1] = HexCalculator.RandomDir();
        }

        return action;
    }

    public void RequestAct()
    {
        RequestAction();
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        // Exec Skill with given direction

        TakeAction(Mathf.FloorToInt(vectorAction[0]), Mathf.FloorToInt(vectorAction[1]));

    }

    private void TakeAction(int act1, int act2)
    {
        StartCoroutine(Skills[act1].Exec(this, act2));

        LastSkillRank = Skills[act1].Rank; 

    }

    // ---------------------------------------------------------------------------------------
    /*                            CLASS METHODS IMPLEMENTATION                              */
    // ---------------------------------------------------------------------------------------

    // TODO: Refactor
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

