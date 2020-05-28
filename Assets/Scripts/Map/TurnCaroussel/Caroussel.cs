using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Caroussel : MonoBehaviour
{
    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    private const int PRE_CALCULATED_TURNS = 16;

    private Queue<int> turnQueue = new Queue<int>();
    private Queue<ICarousselEntry> entries_ = new Queue<ICarousselEntry>();

    private List<IGameCharacter> battleUnits_ = new List<IGameCharacter>();

    // Key: Name of the enemy | Pair: number of enemies of said species in battle
    private Dictionary<string, int> enemyNames = new Dictionary<string, int>();


    public GameObject entryPrefab;
    public GameObject lupusPrefab;                  
    public GameObject redNosedHarePrefab;

    // ---------------------------------------------------------------------------------------
    /*                                    INIT METHODS                                      */
    // ---------------------------------------------------------------------------------------

    public void Initialize()
    {
        // Instantiate Units based on record (fixed, for now)
        InstantiateEnemies();
        // InstantiatePlayer();

        CalculateICVs();
        CalculateTurns();
    }

    private void InstantiateEnemies()
    {
        // For now, amount and type of said enemies is fixed
        const int NUM_LUPUS = 1;
        const int NUM_RED_NOSED_HARE = 1;

        InstanceAtRandom(NUM_LUPUS, lupusPrefab);
        InstanceAtRandom(NUM_RED_NOSED_HARE, redNosedHarePrefab);

        // Also, make sure to fill unit's preyLists/predLists/playerLists
        // For this example, we make an AD-HOC assignation

        for (int i = 0; i < battleUnits_.Count; i++)
        {
            battleUnits_[i].DetectUnitsOfInterest();
        }

        //Debug.Log("Number of species in the field: " + enemyNames.Count);
        //Debug.Log(battleUnits_.Count + " enemies in the field.");
    }

    private void InstanceAtRandom(int NUM_UNITS, GameObject unitPrefab)
    {
        for (int i = 0; i < NUM_UNITS; i++)
        {
            GameObject go;
            IGameCharacter igcComponent;

            // Enemies are child objects of battlemap (they live inside the battle map)
            go = Instantiate(unitPrefab);
            igcComponent = go.GetComponent<IGameCharacter>();

            if (!enemyNames.ContainsKey(igcComponent.Name))
            {
                enemyNames.Add(igcComponent.Name, 1);
            }
            else
            {
                enemyNames[igcComponent.Name]++;
                igcComponent.Name += " (" + enemyNames[igcComponent.Name] + ")";
            }

            battleUnits_.Add(go.GetComponent<IGameCharacter>());
        }
    }

    private void CalculateICVs()
    {
        // Loop through battleUnits_ and set their counters 
        for (int i = 0; i < battleUnits_.Count; i++)
        {
            battleUnits_[i].CounterValue = StatCalculator.CalculateCounter(
                battleUnits_[i].TickSpeed,
                battleUnits_[i].LastSkillRank,
                battleUnits_[i].GetStatusEffectByName("HASTE")
                );

            //Debug.Log("ICV of: " + battleUnits_[i].CounterValue);
        }

    }

    // ---------------------------------------------------------------------------------------
    /*                                 TURN ASSIGNMENT                                      */
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Called every time a unit takes a turn. 
    /// Needs to check the rank of the last skill used and also look for changes in units haste status
    /// </summary>
    public void CalculateTurns()
    {
        int i = 0;

        while (i < PRE_CALCULATED_TURNS)
        {
            CalculateNextTurn(ref i);
        }
        
    }

    public void CalculateNextTurn(ref int currentlyCalculatedTurns)
    {
        bool foundNextTurn = false;
        int index;

        while (!foundNextTurn)
        {
            for (int j = 0; j < battleUnits_.Count; j++)
            {
                battleUnits_[j].CounterValue -= 1;

                if (battleUnits_[j].CounterValue == 0)
                {
                    foundNextTurn = true;
                    turnQueue.Enqueue(j);
                }
            }
        }

        GameObject go;
        Transform contentPanel = transform.GetChild(0).transform;

        while (turnQueue.Count > 0 && currentlyCalculatedTurns < PRE_CALCULATED_TURNS)
        {
            index = turnQueue.Peek();
            go = Instantiate(entryPrefab, contentPanel);
            go.GetComponent<ICarousselEntry>().SetTurnOwner(index, battleUnits_[index].Name);

            entries_.Enqueue(go.GetComponent<ICarousselEntry>());

            // Re-calculate CV of battleUnits_[index]
            battleUnits_[index].CounterValue = StatCalculator.CalculateCounter(
                battleUnits_[index].TickSpeed,
                3,
                battleUnits_[index].GetStatusEffectByName("HASTE")
                );

            currentlyCalculatedTurns++;
            turnQueue.Dequeue();
        }
    }

    // ---------------------------------------------------------------------------------------
    /*                                  ACCESS METHODS                                      */
    // ---------------------------------------------------------------------------------------

    public List<IGameCharacter> GetBattleUnits()
    {
        return battleUnits_;
    }

    public IGameCharacter NextTurnOwner()
    {
        IGameCharacter unit = battleUnits_[entries_.Peek().GetTurnOwner()];

        return unit;
    }

    public void PassTurn()
    {
        turnQueue.Dequeue();
    }

    // ---------------------------------------------------------------------------------------
    /*                                  ACCESS METHODS                                      */
    // ---------------------------------------------------------------------------------------

    public IEnumerator NextTurn(Dictionary<Vector2Int, GameObject> mapTiles)
    {
        // Get the next turn owner
        IGameCharacter turnOwner = NextTurnOwner();
        int actions = turnOwner.GetStatValueByName("ACT");
        bool hasMoved = false;

        // ACT actions need to be performed during this turn
        for (int i = 0; i < actions; i++)
        {
            turnOwner.ChooseAction(mapTiles, hasMoved);

            // Execute chosen action and wait till its end
            StartCoroutine(turnOwner.Action(mapTiles));
        }

        yield return null;
    }

}
