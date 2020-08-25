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

    public int OffsetCol;
    public int OffsetRow;

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    // private const string playerMarker = "0";
    private const string spawnMarker_1 = "x";
    private const string spawnMarker_2 = "y";
    // private const string spawnMarker_3 = "z";
    private List<Vector2Int> spawnableTiles_1 = new List<Vector2Int>();
    private List<Vector2Int> spawnableTiles_2 = new List<Vector2Int>();
    public Dictionary<Vector2Int, HexTile> mapTiles = new Dictionary<Vector2Int, HexTile>();
    
    public List<GameCharacter> battleUnits_ = new List<GameCharacter>();
    public Dictionary<string, int> enemyNames = new Dictionary<string, int>();
    public List<Dictionary<int, bool>> factions = new List<Dictionary<int, bool>>();

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
        //UnityEngine.Random.Range(0, mapFiles.Count)
        string[] mapData = mapFiles[1].text.Split(' ', '\n');

        int maxRow = int.MinValue, maxCol = int.MinValue, minRow = int.MaxValue, minCol = int.MaxValue;
        int row, col;
        bool isSpawn_1 = false, isSpawn_2 = false;


        Vector2Int position;
        GameObject tile;

        int i = 0;
        while (i < mapData.Length)
        {
            if(mapData[i].Equals(spawnMarker_1)) {
                isSpawn_1 = true;
                i++;
            }
            else if (mapData[i].Equals(spawnMarker_2))
            {
                isSpawn_2 = true;
                i++;
            }

            row = int.Parse(mapData[i]) + OffsetRow;
            col = int.Parse(mapData[i + 1]) + OffsetCol;

            position = new Vector2Int(row, col);
            tile = Instantiate(hexTilePrefab, HexCalculator.Position(position.y, position.x), Quaternion.identity, this.transform);

            tile.GetComponent<HexTile>().Position = position;
            mapTiles.Add(position, tile.GetComponent<HexTile>());

            // If this is a spawn tile => Add its position into the spawnable tiles list
            if (isSpawn_1)
            {
                spawnableTiles_1.Add(position);
            }
            else if(isSpawn_2)
            {
                spawnableTiles_2.Add(position);
            }

            // map limits calculation
            if (maxRow < row) maxRow = row;
            if (maxCol < col) maxCol = col;
            if (minRow > row) minRow = row;
            if (minCol > col) minCol = col;

            isSpawn_1 = false;
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

        // Ensure all agents are active
        for (int i = 0; i < battleUnits_.Count; i++)
        {
            battleUnits_[i].GameObject().SetActive(true);
            battleUnits_[i].IsActive = true;
            battleUnits_[i].ResetStats();
        }

        // Randomly shuffle the spawnList in-place to give enemies a spawn position
        Utils.FisherYatesShuffle<Vector2Int>(ref spawnableTiles_1);
        Utils.FisherYatesShuffle<Vector2Int>(ref spawnableTiles_2);
        int j = -1, k = -1, z = -1,  spawnIndex = 0;

        if (spawnableTiles_1.Count < 1 || spawnableTiles_2.Count < 1)
            Debug.LogError("spawners empty");

        for (int i = 0; i < battleUnits_.Count; i++)                                    // Place the agents on the battleMap
        {
            agent = battleUnits_[i];
            go = agent.GameObject();

            if (agent.FactionID == 1)
            {
                spawntiles = spawnableTiles_1;
                spawnIndex = ++j;
            }
            else if (agent.FactionID == 2)
            {
                spawntiles = spawnableTiles_2;
                spawnIndex = ++k;
            }
            else if (agent.FactionID == 3)
            {
                spawntiles = new List<Vector2Int>();            //
                spawnIndex = ++z;
            }
            else
            {
                spawntiles = new List<Vector2Int>();            //      Player faction
                spawnIndex = 0;
            }

            agent.InGamePosition = spawntiles[spawnIndex];
            go.transform.position = HexCalculator.CharacterPosition(spawntiles[spawnIndex]);

            mapTiles[spawntiles[spawnIndex]].Occupier = agent;
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
                Academy.Instance.EnvironmentStep();

                yield return new WaitUntil(() => battleUnits_[index].ActionOver);
                yield return new WaitForSeconds(1f);

                if (CheckDeaths())
                {
                    recalcTurns = true;

                    // Battle Over condition: Faction Supremacy
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
        if (caroussel.actionInfo.WhoDied_.Count > 0)
        {
            caroussel.actionInfo.WhoDied_.Clear();
            return true;
        }

        return false;
    }

    private bool FactionLives(Dictionary<int, bool> faction)
    {
        foreach (int key in faction.Keys)
        {
            if (faction[key]) return true;
        }

        return false;
    }

    bool FactionSupremacy()
    {
        // Players are always the firstly introduced faction 
        /*  Player heck
            
        if (!FactionLives(factions[0])) 
        {
            return true;
        }
        
        */

        int livingfactions = 0;

        for (int i = 0; i < factions.Count; i++)                //
        {
            if (FactionLives(factions[i])) livingfactions++;
        }

        //Debug.Log("Living factions: " + livingfactions);

        return (livingfactions <= 1);
    }

    void CleanUpBattleMap()
    {
        foreach (Vector2Int key in mapTiles.Keys)
        {
            Destroy(mapTiles[key].gameObject);
        }

        mapTiles.Clear();
        spawnableTiles_1.Clear();
        spawnableTiles_2.Clear();

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
