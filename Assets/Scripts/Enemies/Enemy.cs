using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemy         ////
{
    void SetGeneralTag(GameObject go);
}

public enum EnemyBaseType { Herbivore, Carnivore }
public enum EnemySocialType { Loner, Groupal }

public abstract class Enemy : InGameCharacter, IEnemy
{
    private EnemyBaseType btype_;
    private EnemySocialType stype_;

    public Enemy(EnemyBaseType btype, EnemySocialType stype)
    {
        SetBaseType(btype);
        SetSocialType(stype);
    }

    public void SetGeneralTag(GameObject go)
    {
        go.tag = "Enemy";
    }

    protected void SetBaseType(EnemyBaseType btype)
    {
        btype_ = btype;
    }

    protected void SetSocialType(EnemySocialType stype)
    {
        stype_ = stype;
    }
}