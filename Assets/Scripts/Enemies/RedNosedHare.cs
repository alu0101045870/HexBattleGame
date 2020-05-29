using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedNosedHare : Agent, IGameCharacter, IEnemy, ILeporidae
{
    // ---------------------------------------------------------------------------------------
    /*                                      AGENT DATA                                      */
    // ---------------------------------------------------------------------------------------
    private string name_ = "Red-Nosed Hare";

    private Leporidae inheritedComponent_;

    private List<GameObject> playerList = new List<GameObject>();
    private List<GameObject> targets = new List<GameObject>();

    // ---------------------------------------------------------------------------------------
    /*                              INTERFACE IMPLEMENTATION                                */
    // ---------------------------------------------------------------------------------------

    public int TickSpeed {
        get { return inheritedComponent_.TickSpeed; }
        set { inheritedComponent_.TickSpeed = value; }
    }
    public int CounterValue {
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
    public string Name
    {
        get { return name_; }
        set
        {
            name_ = value;
            GetGameObject().name = value;
        }
    }
    public List<Skill> Skillset
    {
        get { return inheritedComponent_.Skillset; }
        set { inheritedComponent_.Skillset = value; }
    }

    public bool HasMoved
    {
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
    public void DetectUnitsOfInterest()                 // TODO: Add Predator list
    {
        playerList.AddRange(GameObject.FindGameObjectsWithTag("Player"));
    }       

    // ---------------------------------------------------------------------------------------
    /*                            AGENT ACTIONS IMPLEMENTATION                              */
    // ---------------------------------------------------------------------------------------

    public IEnumerator Action()
    {
        yield return new WaitForSeconds(1f);
    }

    public void RequestAct()
    {
        RequestAction();
    }

  

    // ---------------------------------------------------------------------------------------
    /*                            CLASS METHODS IMPLEMENTATION                              */
    // ---------------------------------------------------------------------------------------

    void Awake()
    {
        inheritedComponent_ = new Leporidae();
        inheritedComponent_.Init();

        Name = name_;
        SetGeneralTag(gameObject);

        // Stat initialization
        // LP & Stat Values
        SetStatValues(74, 7, 1, 1, 1, 2, 2, 59, 0);         // Change
        // Status effects
        SetStatusEffects(1, 1, 1, 1, 1, 1);

        // Initialize SkillSet
        Skillset.Add(new Bite());
        Skillset.Add(new Move(GetStatValueByName("MOV")));

        // TickSpeed & LastSkillRank (default 3)
        TickSpeed = StatCalculator.CalculateTickSpeed(GetStatValueByName("AGL"));
        LastSkillRank = 3;
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
