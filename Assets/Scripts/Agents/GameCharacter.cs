using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class ActionInfo
{
    // turn owner
    // skillrank
    GameCharacter turnOwner = null;
    int skillRank_ = 3;

    bool hasteApplied_ = false;

    List<int> whoDied_ = new List<int>();

    public GameCharacter TurnOwner { get => turnOwner; set => turnOwner = value; }
    public int SkillRank_ { get => skillRank_; set => skillRank_ = value; }
    public bool HasteApplied_ { get => hasteApplied_; set => hasteApplied_ = value; }
    public List<int> WhoDied_ { get => whoDied_; set => whoDied_ = value; }

    public void Reset()
    {
        turnOwner = null;
        skillRank_ = 3;
        hasteApplied_ = false;
        whoDied_.Clear();
    }

    //   ~~ IDEAS ~~          => Unsure as to where this should be implemented
    // Damage applied:
    // Damage receiver:
    // Damage taken:
    // Status/Stats change
}


public interface IGameChar
{
    event Action<float> OnHealthChanged;
}

public abstract class GameCharacter : Agent
{
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
    private Dictionary<string, float> statusEffects_ = new Dictionary<string, float>();         /* 0.5f - 1f - 2f */

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
    public virtual Dictionary<string, float> StatusEffects
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
        statusEffects_.Add("BRAVERY", 1f);          // Enhances or diminishes strength impact
        statusEffects_.Add("FAITH", 1f);            // Enhances or diminishes magic impact
        statusEffects_.Add("ARMOR", 1f);            // Enhances or diminishes resistance
        statusEffects_.Add("SHIELD", 1f);           // Enhances or diminishes magic resistance
        statusEffects_.Add("REGEN", 1f);            // Periodically restores LPs
        statusEffects_.Add("HASTE", 1f);            // Affects speed calculation
    }

    public virtual void SetStatusEffectByName(string name, float value)
    {
        if (!statusEffects_.ContainsKey(name))
            throw new KeyNotFoundException();

        statusEffects_[name] = value;
    }
    public virtual void SetStatValueByName(string name, int value)
    {
        if (!statValues_.ContainsKey(name))
            throw new KeyNotFoundException();

        statValues_[name] = value;
    }

    public virtual float GetStatusEffectByName(string name)
    {
        float value;

        if (!statusEffects_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value;
    }
    public virtual int GetStatValueByName(string name)
    {
        int value;

        if (!statValues_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value;
    }

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

    // ---------------------------------------------------------------------------------------
    /*                               CAROUSSEL ACTION REQUEST                               */
    // ---------------------------------------------------------------------------------------

    public abstract void RequestAct();

    // ---------------------------------------------------------------------------------------
    /*                                 BATTLE LOOP EVENTS                                   */
    // ---------------------------------------------------------------------------------------

    public abstract void Die();

    public abstract void ReceiveDamage(float amount);

    public abstract void Reset();

    public abstract void ResetStats();
}
