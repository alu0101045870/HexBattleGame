using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionInfo
{
    // turn owner
    // skillrank
    IGameCharacter turnOwner = null;
    int skillRank_ = 3;

    bool hasteApplied_ = false;

    List<int> whoDied_ = new List<int>();

    public IGameCharacter TurnOwner { get => turnOwner; set => turnOwner = value; }
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

public class Caroussel_R : MonoBehaviour
{
    public static Caroussel_R Instance { get; private set; }

    public GameObject entryPrefab;

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    private const int PRE_CALCULATED_TURNS = 16;

    private Queue<int> turnQueue = new Queue<int>();
    private Queue<ICarousselEntry> entries_ = new Queue<ICarousselEntry>();

    // Key: Name of the enemy | Pair: number of enemies of said species in battle   (?)
    private Dictionary<string, int> enemyNames = new Dictionary<string, int>();

    public ActionInfo actionInfo = new ActionInfo();

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS METHODS                                     */
    // ---------------------------------------------------------------------------------------

    void Awake()
    {
        // Singleton pattern implementation: no more than a single instance of Caroussel on scene
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Calculate ICV of each battleUnit. Method meant to be called from the BattleMap
    /// </summary>
    /// <param name="battleUnits_"></param>
    public void CalculateICVs()
    {
        List<IGameCharacter> battleUnits = BattleMap_R.Instance.battleUnits_;

        for (int i = 0; i < battleUnits.Count; i++)
        {
            battleUnits[i].CounterValue = StatCalculator.CalculateCounter(
                battleUnits[i].TickSpeed,
                battleUnits[i].LastSkillRank,
                battleUnits[i].GetStatusEffectByName("HASTE")
                );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void PreCalculateTurns()
    {
        int currentlyCaltulatedTurns = 0;

        // Clear previous list
        ClearPreviousQueue();

        // TODO: Store current countervalues and lastskillranks

        while (currentlyCaltulatedTurns < PRE_CALCULATED_TURNS)
        {
            SetNextTurn(GetNextTurnIndex());
            currentlyCaltulatedTurns++;
        }

    }

    private int GetNextTurnIndex()
    {
        List<IGameCharacter> battleUnits = BattleMap_R.Instance.battleUnits_;
        int index = 0;
        bool foundzero = false;

        // we check for 0 values too so we potentially save some comp. time
        for (int i = 1; i < battleUnits.Count; i++)
        {
            if (battleUnits[i].IsActive)
            {       // => not dead, [asleep or incapacitated]
                if (battleUnits[i].CounterValue < battleUnits[index].CounterValue)
                {
                    index = i;

                    if (battleUnits[index].CounterValue == 0)
                    {
                        foundzero = true;
                        break;
                    }
                }
            }
        }

        if (!foundzero)
        {
            for (int i = 0; i < battleUnits.Count; i++)
            {
                if (battleUnits[i].IsActive)       // => not dead, [asleep or incapacitated]
                    battleUnits[i].CounterValue -= battleUnits[index].CounterValue;
            }
        }

        return index;
    }

    private void SetNextTurn(int index)
    {
        Transform contentPanel = transform.GetChild(0).transform;
        GameObject go = Instantiate(entryPrefab, contentPanel);

        go.GetComponent<ICarousselEntry>().SetTurnOwner(index, BattleMap_R.Instance.battleUnits_[index].Name);

        entries_.Enqueue(go.GetComponent<ICarousselEntry>());

        BattleMap_R.Instance.battleUnits_[index].CounterValue = StatCalculator.CalculateCounter(
                BattleMap_R.Instance.battleUnits_[index].TickSpeed,
                BattleMap_R.Instance.battleUnits_[index].LastSkillRank,
                BattleMap_R.Instance.battleUnits_[index].GetStatusEffectByName("HASTE")
                );

        BattleMap_R.Instance.battleUnits_[index].LastSkillRank = 3;
    }

    public int NextTurnOwner()
    {
        return entries_.Peek().GetTurnOwner();
    }

    public void PassTurn()
    {
        // first, dequeue the first turn entry
        entries_.Dequeue();

        // second, actually dequeue from the unity gameObject
        Destroy(gameObject.transform.GetChild(0).GetChild(0).gameObject);

        // TODO: then, check for the TURN INFORMATION (store in caroussel_r)

        // if => haste has been applied     
        //       skill rank has changed (not 3)
        //       a character has [died, fell asleep, been incapacitated]
        // then: re-calculate the full queue

        // else: calculate and assign next turn
        SetNextTurn(GetNextTurnIndex());

        // refresh UI to show changes
    }

    private void ClearPreviousQueue()
    {
        entries_.Clear();
        turnQueue.Clear();

        Transform panel = gameObject.transform.GetChild(0);

        for (int i = 0; i < panel.childCount; i++)
        {
            Destroy(panel.GetChild(i).gameObject);
        }
    }
}
