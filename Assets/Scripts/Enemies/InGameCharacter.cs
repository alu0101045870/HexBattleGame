using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameCharacter
{
    int TickSpeed { get; set; }
    int CounterValue { get; set; }
    int LP { get; set; }
    int LastSkillRank { get; set; }
    string Name { get; set; }
    List<Skill> Skillset { get; set; }
    bool HasMoved { get; set; }

    float GetStatusEffectByName(string name);
    void SetStatusEffectByName(string name, float value);
    int GetStatValueByName(string name);
    void SetStatValueByName(string name, int value);
    Vector2Int GetInGamePosition();
    void SetInGamePosition(GameObject go, int posX, int posY);
    GameObject GetGameObject();
    string GetFamily();

    void DetectUnitsOfInterest();

    void RequestAct();
}

// EVALUATE CHANGING ALL REFERENCES OF TYPE List<GameObject> TO List<IGameCharacter> if possible
// After all, IGameCharacter has the method GetGameObject()


public abstract class InGameCharacter : IGameCharacter
{
    private int posX_;
    private int posY_;

    private int tickSpeed_;
    private int counterVal_;
    private int lp_;
    private int lastSkillRank_;

    private Dictionary<string, int> statValues_ = new Dictionary<string, int>();                /* Range 0 - 255 */
    private Dictionary<string, float> statusEffects_ = new Dictionary<string, float>();         /* 0.5f - 1f - 2f */

    private List<Skill> skill_set_ = new List<Skill>();
    private bool hasMoved_;

    public int TickSpeed { 
        get { return tickSpeed_; } 
        set { tickSpeed_ = value; } 
    }
    public int CounterValue {
        get { return counterVal_; }
        set { counterVal_ = value; }
    }
    public int LP {
        get { return lp_; }
        set { lp_ = value; }
    }
    public int LastSkillRank
    {
        get { return lastSkillRank_; }
        set { lastSkillRank_ = value; }
    }
    public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public List<Skill> Skillset { get => skill_set_; set => skill_set_ = value; }
    public bool HasMoved { get => hasMoved_; set => hasMoved_ = value; }

    public float GetStatusEffectByName(string name)
    {
        float value;

        if(!statusEffects_.TryGetValue(name, out value))
            throw new KeyNotFoundException();
        
        return value;
    }
    public void SetStatusEffectByName(string name, float value)
    {
        if (!statusEffects_.ContainsKey(name))
            throw new KeyNotFoundException();

        statusEffects_[name] = value;
    }
    public void SetInGamePosition(GameObject go, int posX, int posY)
    {
        posX_ = posX;
        posY_ = posY;

        go.transform.position = HexCalculator.CharacterPosition(posY_, posX_);
    }
    public Vector2Int GetInGamePosition()
    {
        return new Vector2Int(posX_, posY_);
    }
    public int GetStatValueByName(string name)
    {
        int value;

        if (!statValues_.TryGetValue(name, out value))
            throw new KeyNotFoundException();

        return value;
    }
    public void SetStatValueByName(string name, int value)
    {
        if (!statValues_.ContainsKey(name))
            throw new KeyNotFoundException();

        statValues_[name] = value;
    }
    public GameObject GetGameObject()
    {
        throw new NotImplementedException();
    }
    public string GetFamily()
    {
        throw new NotImplementedException();
    }
    public void DetectUnitsOfInterest()
    {
        throw new NotImplementedException();
    }
    public void RequestAct()
    {
        throw new NotImplementedException();
    }
    // ------------------------------------------------------------------------------------------------------//
    /*                                     Specific to base class                                            */
    // ------------------------------------------------------------------------------------------------------//
    public void Init()
    {
        InitStatValues();
        InitStatusEffects();
    }

    private void InitStatValues()
    {
        statValues_.Add("STR", 0);                  // Physical strength and damage
        statValues_.Add("MAG", 0);                  // Magic damage
        statValues_.Add("RES", 0);                  // Resistance to physical damage
        statValues_.Add("M.RES", 0);                // Resistance to magical damage
        statValues_.Add("ACT", 0);                  // Number of actions that the unit can take per turn
        statValues_.Add("MOV", 0);                  // How many hexagons can the unit move in one turn
        statValues_.Add("AGL", 0);                  // Agility, speed stat
        statValues_.Add("ACC", 0);                  // Accuracy, hit or miss
    }

    private void InitStatusEffects()
    {
        statusEffects_.Add("BRAVERY", 1f);          // Enhances or diminishes strength impact
        statusEffects_.Add("FAITH", 1f);            // Enhances or diminishes magic impact
        statusEffects_.Add("ARMOR", 1f);            // Enhances or diminishes resistance
        statusEffects_.Add("SHIELD", 1f);           // Enhances or diminishes magic resistance
        statusEffects_.Add("REGEN", 1f);            // Periodically restores LPs
        statusEffects_.Add("HASTE", 1f);            // Affects speed calculation
    }
}