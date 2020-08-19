﻿using Unity.MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMap : MonoBehaviour
{
    public GameObject hexTilePrefab;
    public GameObject obstaclePrefab;

    [SerializeField] private List<GameObject> enemyPrefabs;
    [SerializeField] private List<TextAsset> mapFiles;
    [SerializeField] private Caroussel caroussel;

    public int OffsetCol;
    public int OffsetRow;

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    private string spawnMarker = "x";
    private List<Vector2Int> spawnableTiles = new List<Vector2Int>();
    public Dictionary<Vector2Int, HexTile> mapTiles = new Dictionary<Vector2Int, HexTile>();
    public List<GameCharacter> battleUnits_ = new List<GameCharacter>();
    public Dictionary<string, int> enemyNames = new Dictionary<string, int>();

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS METHODS                                     */
    // ---------------------------------------------------------------------------------------

    public void StartMap()
    {
        Academy.Instance.OnEnvironmentReset += Initialize();

        InstantiateAgents();

        Initialize().Invoke();

        StartCoroutine(TrainingLoop());
    }

    Action Initialize()
    {
        //Debug.LogError("INITIALIZE WAS CALLED!");

        return delegate ()
        {
            CleanUpBattleMap();

            InitializeMap();
            InitializeAgents();         // =>   Initialize agents + their positions
            InitializeCaroussel();
        };
    }

    /// <summary>
    /// Initializes Map Tiles Based on one Map File chosen at random.
    /// TODO: Add Spawn positions for enemies / player
    /// </summary>
    void InitializeMap()
    {
        string[] mapData = mapFiles[UnityEngine.Random.Range(0, mapFiles.Count)].text.Split(' ', '\n');

        int maxRow = int.MinValue, maxCol = int.MinValue, minRow = int.MaxValue, minCol = int.MaxValue;
        int row, col;
        bool isSpawn = false;

        Vector2Int position;
        GameObject tile;

        int i = 0;
        while (i < mapData.Length)
        {
            if(mapData[i].Equals(spawnMarker)) {
                isSpawn = true;
                i++;
            }

            row = int.Parse(mapData[i]) + OffsetRow;
            col = int.Parse(mapData[i + 1]) + OffsetCol;

            position = new Vector2Int(row, col);
            tile = Instantiate(hexTilePrefab, HexCalculator.Position(position.y, position.x), Quaternion.identity, this.transform);

            tile.GetComponent<HexTile>().Position = position;
            mapTiles.Add(position, tile.GetComponent<HexTile>());

            // If this is a spawn tile => Add its position into the spawnable tiles list
            if (isSpawn)
            {
                spawnableTiles.Add(position);
            }

            // map limits calculation
            if (maxRow < row) maxRow = row;
            if (maxCol < col) maxCol = col;
            if (minRow > row) minRow = row;
            if (minCol > col) minCol = col;

            isSpawn = false;
            i += 2;
        }

        // Now, fill blanks and surroundings with obstacle tiles
        for (int x = minRow - 2; x <= maxRow + 2; x++)
        {
            for (int y = minCol - 2; y <= maxCol + 2; y++)
            {
                position = new Vector2Int(x, y);

                if (!mapTiles.ContainsKey(position))
                {
                    tile = Instantiate(obstaclePrefab, HexCalculator.Position(position.y, position.x), Quaternion.identity, this.transform);
                }
            }
        }

        // For each tile, set its neighbors in the HexTile component
        HexCalculator.SetNeighborsInMap(mapTiles);
    }

    void InstantiateAgents()
    {

        // In a real game scenario, number and variety of enemies would be decided 
        // randomly from the prefab list

        // However, for demo purposes, it is decided beforehand in this stage

        int[] NUM_AGENTS = new int[] { 1, 1 };

        GameObject go;
        GameCharacter agent;

        if (NUM_AGENTS.Length != enemyPrefabs.Count)
        {
            return;     // ¿Repeat the enemy assignation process? 
        }

        for (int i = 0; i < NUM_AGENTS.Length; i++)
        {
            for (int spawn = 0; spawn < NUM_AGENTS[i]; spawn++)
            {
                go = Instantiate(enemyPrefabs[i]);
                agent = go.GetComponent<GameCharacter>();

                if (!enemyNames.ContainsKey(agent.Name))
                {
                    agent.Name = agent.Name;
                    enemyNames.Add(agent.Name, 1);
                }
                else
                {
                    enemyNames[agent.Name]++;
                    agent.Name += " (" + enemyNames[agent.Name] + ")";
                }

                battleUnits_.Add(agent);
                agent.ID = battleUnits_.Count - 1;
                
                agent.BattleMap_ = this;
                agent.Caroussel_ = this.caroussel;
            }
        }

    }

    void InitializeAgents()
    {
        GameObject go;
        GameCharacter agent;

        // Ensure all agents are active
        for (int i = 0; i < battleUnits_.Count; i++)
        {
            battleUnits_[i].GameObject().SetActive(true);
            battleUnits_[i].IsActive = true;
            battleUnits_[i].ResetStats();
        }

        /**
         *  IDEA: Get the enemy spawn pool from mapFile
         *  - Available positions are randomly shuffled into a Vector2Int queue
         *  - Then distributed one by one into the enemies on a loop
         */

        if (spawnableTiles.Count >= enemyPrefabs.Count)
        {
            spawnableTiles.AddRange(new Vector2Int[] {
                new Vector2Int(2 + OffsetRow, 3 + OffsetCol) ,
                new Vector2Int(2 + OffsetRow, 1 + OffsetCol)
            });                                         // Fixed for testing purposes only!)

            // TODO: Make a pool that's big enough (dynamically)
        }

        // Randomly shuffle the spawnList in-place to give enemies an spawn position
        Utils.FisherYatesShuffle<Vector2Int>(spawnableTiles);

        for (int i = 0; i < battleUnits_.Count; i++)                                    // Place the agents on the battleMap
        {
            agent = battleUnits_[i];
            go = agent.GameObject();

            agent.InGamePosition = spawnableTiles[i];
            go.transform.position = HexCalculator.CharacterPosition(spawnableTiles[i]);

            mapTiles[spawnableTiles[i]].Occupier = agent;
        }
    }

    void InitializeCaroussel()
    {
        caroussel.CalculateICVs();
        caroussel.PreCalculateTurns();
    }

    
    IEnumerator TrainingLoop()
    {
        bool stopCondition = false;
        bool recalcTurns = false;
        int index;

        while (!stopCondition)
        {
            index = caroussel.NextTurnOwner();
            caroussel.actionInfo.TurnOwner = battleUnits_[index];

            for (int j = 0; j < battleUnits_[index].GetStatValueByName("ACT"); j++)
            {
                battleUnits_[index].RequestAct();

                yield return new WaitUntil(() => battleUnits_[index].ActionOver);
                yield return new WaitForSeconds(1f);

                battleUnits_[index].ActionOver = false;

                if (CheckDeaths())
                {
                    recalcTurns = true;

                    // TODO: Implement faction supremacy check
                    if (stopCondition = FactionSupremacy()) break;
                }
            }

            if (!stopCondition)
            {
                caroussel.PassTurn();

                if (recalcTurns)
                {
                    // Re-calculate turns
                    caroussel.PreCalculateTurns();
                    recalcTurns = false;
                }
            }
            else
            {
                stopCondition = false;
                EpisodeReset();
            }
        }
    }
    

    bool CheckDeaths()
    {
        // Remove dead unit(s) from caroussel and game on the process
        if (caroussel.actionInfo.WhoDied_.Count > 0)
        {
            for (int i = 0; i < caroussel.actionInfo.WhoDied_.Count; i++)
            {
                battleUnits_[caroussel.actionInfo.WhoDied_[i]].Die();
            }

            caroussel.actionInfo.WhoDied_.Clear();

            return true;
        }

        return false;
    }

    bool FactionSupremacy()
    {
        return true;
    }

    void CleanUpBattleMap()
    {
        foreach (Vector2Int key in mapTiles.Keys)
        {
            Destroy(mapTiles[key].gameObject);
        }

        mapTiles.Clear();
        spawnableTiles.Clear();

        foreach (Transform tr in transform)
        { 
            if (tr.CompareTag("Obstacle")) Destroy(tr.gameObject);
        }
    }

    void EpisodeReset()
    {
        for (int i = 0; i < battleUnits_.Count; i++)
        {
            battleUnits_[i].Reset();
        }

        Academy.Instance.EnvironmentReset();
    }
}