using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -------------------------------------------------------------------------------------------------------------

public interface ICanid
{

}

public class Canid : Enemy, ICanid
{
    public Canid() : base(EnemyBaseType.Carnivore, EnemySocialType.Loner)
    { 


    }
}

// -------------------------------------------------------------------------------------------------------------

public interface ILeporidae
{

}

public class Leporidae : Enemy, ILeporidae
{
    public Leporidae() : base(EnemyBaseType.Herbivore, EnemySocialType.Groupal)
    {

    }
}