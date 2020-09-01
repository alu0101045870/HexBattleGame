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
    private int lastSkillRank_;
    private bool isActive_ = true;

    private Dictionary<string, int> statValues_ = new Dictionary<string, int>();                /* Range 0 - 255 */
    private Dictionary<string, Pair<float, int>> statusEffects_ = new Dictionary<string, Pair<float, int>>();         /* 0.5f - 1f - 2f */ 

    private BattleMap battleMap;
    private Caroussel caroussel;

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

    public virtual void SetStatusEffectByName(string name, Pair<float, int> status)
    {
        if (!statusEffects_.ContainsKey(name))
            throw new KeyNotFoundException();

        statusEffects_[name] = status;
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
        SetStatusEffectByName("BRAVERY", new Pair<float, int>(bravery, (bravery.Equals(1f)) ? 0 : 10));
        SetStatusEffectByName("FAITH", new Pair<float, int>(faith, (faith.Equals(1f)) ? 0 : 10));
        SetStatusEffectByName("ARMOR", new Pair<float, int>(armor, (armor.Equals(1f)) ? 0 : 10));
        SetStatusEffectByName("SHIELD", new Pair<float, int>(shield, (shield.Equals(1f)) ? 0 : 10));
        SetStatusEffectByName("REGEN", new Pair<float, int>(regen, (regen.Equals(1f)) ? 0 : 10));
        SetStatusEffectByName("HASTE", new Pair<float, int>(haste, (haste.Equals(1f)) ? 0 : 10));
    }

    // ---------------------------------------------------------------------------------------
    /*                               CAROUSSEL ACTION REQUEST                               */
    // ---------------------------------------------------------------------------------------

    public abstract void RequestAct();

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------

    public abstract void Die();

    public abstract void Win();

    public abstract void Lose();

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
        SetParameters();

        // Healthbar is actually "independent" from HP parameter 
        // so it needs a reset too
        OnHealthChanged(100);
    }

    public abstract void Reset();
}
