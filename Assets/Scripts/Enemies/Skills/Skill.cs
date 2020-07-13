using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Skill 
{
    protected int range_;
    protected int rank_;
    public int Range 
    { 
        get { return range_; }
        set { range_ = value; }
    }

    public int Rank
    {
        get { return rank_; }
        set { rank_ = value; }
    }

    public abstract IEnumerator Exec(IGameCharacter caster, int dir);
}

public class Bite : Skill
{
    public Bite () {
        range_ = 1;
        rank_ = 3;
    }

    public override IEnumerator Exec(IGameCharacter caster, int dir)
    {
        Debug.Log("Did something!");
        throw new System.NotImplementedException();
    }
}

public class Move : Skill
{
    public Move ()
    {
        range_ = 2;
        rank_ = 3;
    }

    public override IEnumerator Exec(IGameCharacter caster, int dir)
    {
        Debug.Log("Did something!");
        yield return new WaitForSeconds(1f);
    }
}

public class Defend : Skill
{
    public Defend()
    {
        range_ = 0;
        rank_ = 2;
    }

    public override IEnumerator Exec(IGameCharacter caster, int dir)
    {
        Debug.Log("Did something!");
        throw new System.NotImplementedException();
    }
}