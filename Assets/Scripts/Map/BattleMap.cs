using Unity.MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMap : MonoBehaviour
{
    public GameObject hexTilePrefab;
    public GameObject obstaclePrefab;

    [SerializeField] private List<GameObject> playableCharPrefabs; 
    [SerializeField] private List<GameObject> enemyFaction_1_Prefabs;
    [SerializeField] private List<GameObject> enemyFaction_2_Prefabs;
    [SerializeField] private List<GameObject> enemyFaction_3_Prefabs;
    [SerializeField] private List<TextAsset> mapFiles;
    [SerializeField] private Caroussel caroussel;
    [SerializeField] private List<string> spawnMarkers;

    public int OffsetCol;
    public int OffsetRow;

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------
    
    private List<List<Vector2Int>> spawnableTiles_ = new List<List<Vector2Int>>();
    public Dictionary<Vector2Int, HexTile> mapTiles = new Dictionary<Vector2Int, HexTile>();
    
    public List<GameCharacter> battleUnits_ = new List<GameCharacter>();
    public Dictionary<string, int> enemyNames = new Dictionary<string, int>();
    public List<Dictionary<int, bool>> factions = new List<Dictionary<int, bool>>();
    private List<int> winnerFactions = new List<int>();

    public bool envDone = true;

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS METHODS                                     */
    // ---------------------------------------------------------------------------------------

    public void StartMap()
    {
        Academy.Instance.AutomaticSteppingEnabled = false;
        Academy.Instance.OnEnvironmentReset += Initialize();

        InstantiateAgents();
        InstantiatePlayableChars();

        Initialize().Invoke();

        StartCoroutine(TrainingLoop());
    }

    Action Initialize()
    {
        return delegate ()
        {
            if (envDone)
            {
                // Ensure all agents are active
                for (int i = 0; i < battleUnits_.Count; i++)
                {
                    battleUnits_[i].GameObject().SetActive(true);
                    battleUnits_[i].IsActive = true;
                    battleUnits_[i].ResetStats();
                }

                CleanUpBattleMap();

                InitializeMap();
                InitializeAgents();         // =>   Initialize agents + their positions
                InitializeCaroussel();
            }

            envDone = false;
        };
    }

    /// <summary>
    /// Initializes Map Tiles Based on one Map File chosen at random.
    /// TODO: Add Spawn positions for enemies / player
    /// </summary>
    void InitializeMap()
    {
        //UnityEngine.Random.Range(0, mapFiles.Count)
        string[] mapData = mapFiles[1].text.Split(' ', '\n');

        int maxRow = int.MinValue, maxCol = int.MinValue, minRow = int.MaxValue, minCol = int.MaxValue;
        int row, col;
        bool isSpawn = false;
        int faction = 0;

        Vector2Int position;
        GameObject tile;

        for (int j = 0; j < 4; j++) 
            spawnableTiles_.Add(new List<Vector2Int>());

        int i = 0;
        while (i < mapData.Length)
        {
            for (int j = 0; j < spawnMarkers.Count; j++) { 
                
                if (mapData[i].Equals(spawnMarkers[j])) {
                    isSpawn = true;
                    faction = j;
                    i++;
                    break;
                }
            }

            row = int.Parse(mapData[i]) + OffsetRow;
            col = int.Parse(mapData[i + 1]) + OffsetCol;

            position = new Vector2Int(row, col);
            tile = Instantiate(hexTilePrefab, HexCalculator.Position(position.y, position.x), Quaternion.identity, this.transform);

            tile.GetComponent<HexTile>().Position = position;
            mapTiles.Add(position, tile.GetComponent<HexTile>());

            // If this is a spawn tile => Add its position into the spawnable tiles list
            if (isSpawn) spawnableTiles_[faction].Add(position);
            
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

    void InstantiatePlayableChars()
    {
        // Retrieve serialized team data

        // Instantiate from list of prefabs
    }

    void InstantiateAgents()
    {
        InstantiateFaction(playableCharPrefabs, new int[] { 1, 1, 1, 1 });

        InstantiateFaction(enemyFaction_1_Prefabs, new int[] { 1 });
        InstantiateFaction(enemyFaction_2_Prefabs, new int[] { 1 });
        InstantiateFaction(enemyFaction_3_Prefabs, new int[] { 1 });

        //Deb();
    }


    void InstantiateFaction(List<GameObject> factionPrefabs, int[] NUM_AGENTS)
    {
        GameObject go;
        GameCharacter agent;

        factions.Add(new Dictionary<int, bool>());

        if (factionPrefabs == null || NUM_AGENTS.Length != factionPrefabs.Count)
        {
            return;
        }

        for (int i = 0; i < NUM_AGENTS.Length; i++)
        {
            for (int spawn = 0; spawn < NUM_AGENTS[i]; spawn++)
            {
                go = Instantiate(factionPrefabs[i]);
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
                agent.FactionID = factions.Count - 1;

                agent.BattleMap_ = this;
                agent.Caroussel_ = this.caroussel;

                factions[factions.Count - 1].Add(agent.ID, true);
            }
        }
    }

    void InitializeAgents()
    {
        GameObject go;
        GameCharacter agent;
        List<Vector2Int> spawntiles;
        int[] spawnIndex = new int[] { -1, -1, -1, -1 };
        int factionID;

        // Randomly shuffle the spawnList in-place to give enemies a spawn position
        for (int i = 0; i < spawnableTiles_.Count; i++)
            Utils.FisherYatesShuffle<Vector2Int>(spawnableTiles_[i]);
      

        for (int i = 0; i < battleUnits_.Count; i++)                                    // Place the agents on the battleMap
        {
            agent = battleUnits_[i];
            go = agent.GameObject();

            factionID = agent.FactionID;
            factions[factionID][agent.ID] = true;
            spawntiles = spawnableTiles_[factionID];

            agent.InGamePosition = spawntiles[++spawnIndex[factionID]];
            go.transform.position = HexCalculator.CharacterPosition(spawntiles[spawnIndex[factionID]]);

            mapTiles[spawntiles[spawnIndex[factionID]]].Occupier = agent;
        }
    }

    void InitializeCaroussel()
    {
        caroussel.Init();
    }

    
    IEnumerator TrainingLoop()
    {
        bool stopCondition = false;
        int index;

        while (!stopCondition)
        {
            index = caroussel.NextTurnOwner();
            caroussel.actionInfo.TurnOwner = battleUnits_[index];

            for (int j = 0; j < battleUnits_[index].GetStatValueByName("ACT"); j++)
            {
                battleUnits_[index].RequestAct();
                Academy.Instance.EnvironmentStep();

                yield return new WaitUntil(() => battleUnits_[index].ActionOver);
                yield return new WaitForSeconds(1f);

                // Training Over condition: MaxStep Reached
                if (stopCondition = (battleUnits_[index].MaxStep < battleUnits_[index].StepCount)) break;

                if (caroussel.CheckCarousselTriggerEvents())
                {
                    // Battle Over condition: Faction Supremacy 
                    if (stopCondition = FactionSupremacy(out winnerFactions)) break;
                }
            }

            // After-Turn events (poison, etc) should be triggered here <======

            if (!stopCondition)
            {
                battleUnits_[index].SynthethiseSkillRanks();
                caroussel.PassTurn();
            }
            else
            {
                stopCondition = false;
                RewardWinners(winnerFactions);
                EpisodeReset();
            }

            caroussel.actionInfo.Reset();
        }
    }

    private bool FactionLives(Dictionary<int, bool> faction)
    {
        foreach (int key in faction.Keys)
        {
            if (faction[key]) return true;
        }

        return false;
    }

    bool FactionSupremacy(out List<int> winnerFactions)
    {
        // Players are always the firstly introduced faction 
        /*  If the player is dead -> other factions win
            
        if (!PlayersLive())) 
        {
            winnerFactions.Add(1);
            winnerFactions.Add(2);
            winnerFactions.Add(3);

            return true;
        }
        
        */

        List<int> livingfactions = new List<int>();
        winnerFactions = new List<int>();

        for (int i = 0; i < factions.Count; i++)                //
        {
            if (FactionLives(factions[i])) 
                livingfactions.Add(i);
        }

        if (livingfactions.Count == 1)
        {
            winnerFactions.Add(livingfactions[0]);
            
            //Debug.Log("Winner Faction: " + winnerFactions[0]);
            return true;
        }

        return false;
    }

    void CleanUpBattleMap()
    {
        foreach (Vector2Int key in mapTiles.Keys)
        {
            Destroy(mapTiles[key].gameObject);
        }

        mapTiles.Clear();
        winnerFactions.Clear();

        for (int i = 0; i < spawnableTiles_.Count; i++)
            spawnableTiles_[i].Clear();

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

        envDone = true;
        Academy.Instance.EnvironmentReset();
    }

    void RewardWinners(List<int> winnerFactionID)
    {
        // No winners!
        if (winnerFactionID.Count <= 0)
        {
            for (int i = 0; i < battleUnits_.Count; i++)
            {
                battleUnits_[i].Lose();
            }

            return;
        }

        // At least one faction won!
        for (int i = 0; i < battleUnits_.Count; i++)
        {
            for (int j = 0; j < winnerFactionID.Count; j++)
                if (battleUnits_[i].FactionID.Equals(winnerFactions[j]))
                    battleUnits_[i].Win();
                else
                    battleUnits_[i].Lose();
        }
    }
}
