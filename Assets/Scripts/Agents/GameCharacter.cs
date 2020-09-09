using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public abstract class GameCharacter : Agent
{
    public event Action<float> OnHealthChanged = delegate { };

    private bool actionOver = false;
    private string species_ = "";
    private string name_ = "";
    private int id_;
    private int factionID_;
    private Vector2Int ingame_position_ = new Vector2Int();

    private int tickspeed_;
    private int counterValue_;
    private int maxHP_;
    private bool isActive_ = true;
    private int lastSkillRank_;
    protected List<int> skillRanks = new List<int>();

    private Dictionary<string, int> statValues_ = new Dictionary<string, int>();                                      /* Range 0 - 255 */
    private Dictionary<string, Pair<float, int>> statusEffects_ = new Dictionary<string, Pair<float, int>>();         /* 0.5f - 1f - 2f */ 

    private BattleMap battleMap;
    private Caroussel caroussel;
    
    protected EnvironmentParameters environmentParameters;
    public int trainingPhase;                                // Training only

    // ---------------------------------------------------------------------------------------
    /*                                      PROPERTIES                                      */
    // ---------------------------------------------------------------------------------------

    public virtual string Species { 
        get => species_; 
        set => species_ = value; 
    }
    public virtual string Name { 
        get => name_;
        set {
            name_ = value;
            gameObject.name = value;
        }
    }
    public virtual int ID { 
        get => id_; 
        set => id_ = value; 
    }
    public virtual int FactionID
    {
        get => factionID_;
        set => factionID_ = value;
    }
    public virtual int TickSpeed {
        get => tickspeed_; 
        set => tickspeed_ = value; 
    }
    public virtual int CounterValue { 
        get => counterValue_; 
        set => counterValue_ = value; 
    }
    public virtual int MaxHP { 
        get => maxHP_; 
        set => maxHP_ = value; 
    }
    public virtual int LastSkillRank { 
        get => lastSkillRank_; 
        set => lastSkillRank_ = value; 
    }
    public virtual Vector2Int InGamePosition { 
        get => ingame_position_; 
        set => ingame_position_ = value; 
    }
    public virtual Dictionary<string, int> StatValues
    {
        get => statValues_;
    }
    public virtual Dictionary<string, Pair<float, int>> StatusEffects
    {
        get => statusEffects_;
    }
    public virtual bool IsActive { 
        get => isActive_; 
        set => isActive_ = value; 
    }
    public virtual bool ActionOver { 
        get => actionOver; 
        set => actionOver = value; 
    }
    public BattleMap BattleMap_ { get => battleMap; set => battleMap = value; }
    public Caroussel Caroussel_ { get => caroussel; set => caroussel = value; }

    public virtual GameObject GameObject()
    {
        return gameObject;
    }

    // ---------------------------------------------------------------------------------------
    /*                                    STATS/STATUS                                      */
    // ---------------------------------------------------------------------------------------

    public virtual void InitStatValues()
    {
        statValues_.Clear();
        statValues_.Add("HP", 0);
        statValues_.Add("STR", 0);                  // Physical strength and damage
        statValues_.Add("MAG", 0);                  // Magic damage
        statValues_.Add("RES", 0);                  // Resistance to physical damage
        statValues_.Add("M.RES", 0);                // Resistance to magical damage
        statValues_.Add("ACT", 0);                  // Number of actions that the unit can take per turn
        statValues_.Add("MOV", 0);                  // How many hexagons can the unit move in one turn
        statValues_.Add("AGL", 0);                  // Agility, speed stat
        statValues_.Add("ACC", 0);                  // Accuracy, hit or miss
    }
    public virtual void InitStatusEffects()
    {
        statusEffects_.Clear();
        statusEffects_.Add("BRAVERY", new Pair<float, int>(1f, 0));          // Enhances or diminishes strength impact
        statusEffects_.Add("FAITH", new Pair<float, int>(1f, 0));            // Enhances or diminishes magic impact
        statusEffects_.Add("ARMOR", new Pair<float, int>(1f, 0));            // Enhances or diminishes resistance
        statusEffects_.Add("SHIELD", new Pair<float, int>(1f, 0));           // Enhances or diminishes magic resistance
        statusEffects_.Add("REGEN", new Pair<float, int>(1f, 0));            // Periodically restores LPs
        statusEffects_.Add("HASTE", new Pair<float, int>(1f, 0));            // Affects speed calculation
    }

    public virtual void SetStatusEffectByName(string name, float value)
    {
        if (!statusEffects_.ContainsKey(name))
            throw new KeyNotFoundException();

        statusEffects_[name].First = value;
        statusEffects_[name].Second = (value.Equals(1f)) ? 0 : 10;
    }
    public virtual void SetStatValueByName(string name, int value)
    {
        if (!statValues_.ContainsKey(name))
            throw new KeyNotFoundException();

        statValues_[name] = value;
    }

    public virtual float GetStatusEffectByName(string name)
    {
        Pair<float, int> value;

        if (!statusEffects_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value.First;
    }
    public virtual int GetStatValueByName(string name)
    {
        int value;

        if (!statValues_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value;
    }
    public virtual int GetStatusCounterByName(string name)
    {
        Pair<float, int> value;

        if (!statusEffects_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value.Second;
    }
    public abstract void SetParameters();

    public virtual void SetStatValues(int lps, int str, int mag, int res, int m_res, int act, int mov, int agl, int acc)
    {
        maxHP_ = lps;
        SetStatValueByName("HP", lps);
        SetStatValueByName("STR", str);
        SetStatValueByName("MAG", mag);
        SetStatValueByName("RES", res);
        SetStatValueByName("M.RES", m_res);
        SetStatValueByName("ACT", act);
        SetStatValueByName("MOV", mov);
        SetStatValueByName("AGL", agl);
        SetStatValueByName("ACC", acc);
    }
    public virtual void SetStatusEffects(float bravery, float faith, float armor, float shield, float regen, float haste)
    {
        SetStatusEffectByName("BRAVERY", bravery);
        SetStatusEffectByName("FAITH", faith);
        SetStatusEffectByName("ARMOR", armor);
        SetStatusEffectByName("SHIELD", shield);
        SetStatusEffectByName("REGEN", regen);
        SetStatusEffectByName("HASTE", haste);
    }
    
    /// <summary>
    ///     Called each Passed Turn from Caroussel
    /// </summary>
    public virtual void DecreaseStatusCounters()
    {
        foreach (string key in statusEffects_.Keys)
        {
            if (!statusEffects_[key].First.Equals(1f))
            {
                statusEffects_[key].Second--;
                if (statusEffects_[key].Second <= 0)
                    statusEffects_[key].First = 1f;
                    // TOTO: Also, trigger some kind of visual event maybe?
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    /*                                   ACTION REQUEST                                     */
    // ---------------------------------------------------------------------------------------

    public abstract void RequestAct();

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------

    public abstract void Die();

    public abstract void Win();

    public abstract void Lose();

    public virtual void ApplyStatusEffect(string name, float mode)
    {
        int duration = 10;

        statusEffects_[name].First = mode;
        statusEffects_[name].Second = duration;
    }

    public virtual void ReceiveDamage(float amount)
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

    public virtual void ResetStats()
    {
        if (BattleMap_ != null && BattleMap_.envDone)
        {
            SetParameters();

            // Healthbar is actually "independent" from HP parameter 
            // so it needs a reset too
            OnHealthChanged(100);
        }
    }

    public abstract void Reset();

    public virtual void SynthethiseSkillRanks()
    {
        if (skillRanks.Count <= 0) {
            LastSkillRank = 3;
            return;
        }

        int maxRank = skillRanks[0];

        for (int i = 1; i < skillRanks.Count; i++)
        {
            if (skillRanks[i] > maxRank)
                maxRank = skillRanks[i];
        }

        LastSkillRank = maxRank;
        skillRanks.Clear();
    }
    public virtual void PostTurnEvents()
    {
        // Apply poison/regen health changes

        // decresase all non-0 counters in statusEffects
        // if one gets to 0, set it's status value to 1 (default status)
        foreach (string key in statusEffects_.Keys)
        {
            if (!statusEffects_[key].First.Equals(1f))
            {
                if (--statusEffects_[key].Second <= 0)
                {
                    statusEffects_[key].First = 1f;
                }
            }
        }
    }

}
