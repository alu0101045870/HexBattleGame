using Unity.MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Caroussel : MonoBehaviour
{
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private BattleMap battleMap;

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    private const int PRE_CALCULATED_TURNS = 16;

    private Queue<int> turnQueue = new Queue<int>();
    private Queue<ICarousselEntry> entries_ = new Queue<ICarousselEntry>();

    // Key: Name of the enemy | Pair: number of enemies of said species in battle   (?)
    private Dictionary<string, int> enemyNames = new Dictionary<string, int>();
    
    private List<bool> defaultTurnsToBeAssigned = new List<bool>();
    private List<Pair<float, int>> hasteCounters = new List<Pair<float, int>>();
    private List<int> counterValues = new List<int>();

    public ActionInfo actionInfo = new ActionInfo();

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS METHODS                                     */
    // ---------------------------------------------------------------------------------------

    public void Init()
    {
        InitDefaultTurns();
        InitHasteCounters();
        InitForwardCounterValues();
        
        CalculateICVs();
        PreCalculateTurns();
    }

    private void InitDefaultTurns()
    {
        defaultTurnsToBeAssigned.Clear();
        
        for (int i = 0; i < battleMap.battleUnits_.Count; i++)
        {
            defaultTurnsToBeAssigned.Add(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>true: if there are non-default turns left to be assigned - false: otherwise </returns>
    private bool NonDefaultTurnsToBeAssigned()
    {
        for (int i = 0; i < defaultTurnsToBeAssigned.Count; i++)
        {
            if (!defaultTurnsToBeAssigned[i]) return true;
        }

        return false;
    }

    private void InitHasteCounters()
    {
        float haste;
        int counter; 

        for (int i = 0; i < battleMap.battleUnits_.Count; i++)
        {
            haste = battleMap.battleUnits_[i].GetStatusEffectByName("HASTE");
            counter = battleMap.battleUnits_[i].GetStatusCounterByName("HASTE");
            hasteCounters.Add(new Pair<float, int>(haste, counter));
        }
    }

    private void DecreaseForwardHasteCounter(int unitIndex)
    {
        if (!hasteCounters[unitIndex].Equals(1f)) {
            
            hasteCounters[unitIndex].Second--;

            if (hasteCounters[unitIndex].Second <= 0)
            {
                hasteCounters[unitIndex].Second = 0;
                hasteCounters[unitIndex].First = 1f;
            } 
        }
    }

    private void InitForwardCounterValues()
    {
        for (int i = 0; i < battleMap.battleUnits_.Count; i++)
        {
            counterValues.Add(0);    
        }
    }

    /// <summary>
    /// Calculate ICV of each battleUnit. Method meant to be called from the BattleMap at the start of each episode
    /// </summary>
    /// <param name="battleUnits_"></param>
    public void CalculateICVs()
    {
        List<GameCharacter> battleUnits = battleMap.battleUnits_;

        int tickspeed, lastskillR;
        float hasteStatus;

        for (int i = 0; i < battleUnits.Count; i++)
        {
            tickspeed = battleUnits[i].TickSpeed;
            lastskillR = battleUnits[i].LastSkillRank;
            hasteStatus = battleUnits[i].GetStatusEffectByName("HASTE");

            counterValues[i] = battleUnits[i].CounterValue = StatCalculator.CalculateCounter(tickspeed, lastskillR, hasteStatus);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void PreCalculateTurns()
    {
        int currentlyCaltulatedTurns = 0;

        // Clear previous turn queue
        ClearPreviousQueue();
        ResetDefaultTurns();
        ReSetForwardCounterValues();

        // 
        // Calculate a "first round" of turns 
        // For the current LastSkillRanks
        while (currentlyCaltulatedTurns < PRE_CALCULATED_TURNS)
        {
            SetNextTurn(CalculateNextTurn());
            currentlyCaltulatedTurns++;
        }

        // check flag is active => order is maintained in 
        // forward SetTurn routine
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

    private void ResetDefaultTurns()
    {
        for (int i = 0; i < defaultTurnsToBeAssigned.Count; i++)
        {
            defaultTurnsToBeAssigned[i] = false;
        }
    }

    /// <summary>
    /// Sets Forward Counter Values in caroussel to match with actual units' Counter Values
    /// </summary>
    private void ReSetForwardCounterValues()
    {
        for (int i = 0; i < battleMap.battleUnits_.Count; i++)
        {
            counterValues[i] = battleMap.battleUnits_[i].CounterValue;
        }
    }

    private Pair<int, int> CalculateNextTurn()
    {
        List<GameCharacter> battleUnits = battleMap.battleUnits_;
        int index = 0;
        bool turnFound = false;

        // remember to check isActive
        while (!turnFound)
        {
            for (int i = 0; i < battleUnits.Count; i++)
            {
                if (battleUnits[i].IsActive && counterValues[i] <= 0 && !turnFound)
                {
                    turnFound = true;
                    index = i;
                }

                counterValues[i]--;
            }
        }

        if (NonDefaultTurnsToBeAssigned())
        {
            defaultTurnsToBeAssigned[index] = true;
            return new Pair<int, int>(index, battleUnits[index].LastSkillRank);
        }
        else
        {
            // Queue a default skillRank turn
            return new Pair<int, int>(index, 3);
        }
    }

    private void SetNextTurn(Pair<int, int> index_rank)
    {
        Transform contentPanel = transform.GetChild(0).transform;
        GameObject go = Instantiate(entryPrefab, contentPanel);

        go.GetComponent<ICarousselEntry>().SetTurnOwner(index_rank.First, battleMap.battleUnits_[index_rank.First].Name);

        entries_.Enqueue(go.GetComponent<ICarousselEntry>());

        counterValues[index_rank.First] = StatCalculator.CalculateCounter(
                battleMap.battleUnits_[index_rank.First].TickSpeed,
                index_rank.Second,
                hasteCounters[index_rank.First].First
                );

        // Decrease Forward Haste Counter after assignment
        DecreaseForwardHasteCounter(index_rank.First);
    }

    public int NextTurnOwner()
    {
        return entries_.Peek().GetTurnOwner();
    }

    public void PassTurn()
    {
        // Decrease status effects of current turn owner
        actionInfo.TurnOwner.DecreaseStatusCounters();
        UpdateCounterValues();

        // Dequeue the first turn entry
        entries_.Dequeue();

        // second, actually dequeue from the unity gameObject
        Destroy(gameObject.transform.GetChild(0).GetChild(0).gameObject);

        // if => haste has been applied     
        //       skill rank has changed (not 3)
        //       a character has [died, fell asleep, been incapacitated]
        // then: re-calculate the full queue
        if (CheckCarousselTriggerEvents())
        {
            Debug.Log("In");
            PreCalculateTurns();
        }
        // else: calculate and assign next turn
        else SetNextTurn(CalculateNextTurn());

        // UI refreshes on Update
    }
    public bool CheckCarousselTriggerEvents()
    {
        // At least one unit died                      or there was a re-calc trigger issued towards the caroussel
        return (actionInfo.WhoDied_.Count > 0) || (actionInfo.StatusTriggerApplied_);
    }

    /// <summary>
    /// Effectively passes the turn from the perspective of the battle units.
    /// Their counter values will now reflect the last performed action in-game
    /// </summary>
    private void UpdateCounterValues()
    {
        List<GameCharacter> battleUnits = battleMap.battleUnits_;
        int index = 0;
        bool turnFound = false;

        // remember to check isActive
        while (!turnFound)
        {
            for (int i = 0; i < battleUnits.Count; i++)
            {
                if (battleUnits[i].IsActive && battleUnits[i].CounterValue <= 0 && !turnFound)
                {
                    turnFound = true;
                    index = i;
                }

                battleUnits[i].CounterValue--;
            }
        }

        battleUnits[index].CounterValue = StatCalculator.CalculateCounter(
                battleUnits[index].TickSpeed,
                battleUnits[index].LastSkillRank,
                battleUnits[index].GetStatusEffectByName("HASTE")
                );


    }
}

public class ActionInfo
{
    // turn owner
    // skillrank
    GameCharacter turnOwner = null;

    bool statusTriggerApplied_ = false;

    List<int> whoDied_ = new List<int>();

    public GameCharacter TurnOwner { get => turnOwner; set => turnOwner = value; }
    public bool StatusTriggerApplied_ { get => statusTriggerApplied_; set => statusTriggerApplied_ = value; }
    public List<int> WhoDied_ { get => whoDied_; set => whoDied_ = value; }

    public void Reset()
    {
        turnOwner = null;
        statusTriggerApplied_ = false;
        whoDied_.Clear();
    }

    //   ~~ IDEAS ~~          => Unsure as to where this should be implemented
    // Damage applied:
    // Damage receiver:
    // Damage taken:
    // Status/Stats change
}