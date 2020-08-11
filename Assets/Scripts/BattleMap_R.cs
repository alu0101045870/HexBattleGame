using MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMap_R : MonoBehaviour
{
    public static BattleMap_R Instance { get; private set; }

    public GameObject hexTilePrefab;
    public GameObject obstaclePrefab;
    
    public GameObject lupusPrefab;
    public GameObject rnhPrefab;

    [SerializeField] private List<GameObject> enemyPrefabs;
    [SerializeField] private List<TextAsset> mapFiles;

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    public Dictionary<Vector2Int, HexTile> mapTiles = new Dictionary<Vector2Int, HexTile>();
    public List<IGameCharacter> battleUnits_ = new List<IGameCharacter>();
    public Dictionary<string, int> enemyNames = new Dictionary<string, int>();

    // ---------------------------------------------------------------------------------------
    /*                                    CLASS METHODS                                     */
    // ---------------------------------------------------------------------------------------

    void Awake()
    {
        // Singleton pattern implementation: no more than a single instance of BattleMap on scene
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InstantiateAgents();

        Academy.Instance.OnEnvironmentReset += Log();
        Initialize().Invoke();

        StartCoroutine(GameLoop());
    }

    Action Log()
    {
        return delegate ()
        {
            Debug.Log(Academy.Instance.EpisodeCount);
        };
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

        Vector2Int position;
        GameObject tile;

        int i = 0;
        while (i < mapData.Length)
        {
            row = int.Parse(mapData[i]);
            col = int.Parse(mapData[i + 1]);
            
            position = new Vector2Int(row, col);
            tile = Instantiate(hexTilePrefab, HexCalculator.Position(position.y, position.x), Quaternion.identity, this.transform);

            tile.GetComponent<HexTile>().Position = position;
            mapTiles.Add(position, tile.GetComponent<HexTile>());

            // map limits calculation
            if (maxRow < row) maxRow = row;
            if (maxCol < col) maxCol = col;
            if (minRow > row) minRow = row;
            if (minCol > col) minCol = col;
 
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

    void InstantiateAgents() {

        // In a real game scenario, number and variety of enemies would be decided 
        // randomly from the prefab list

        // However, for demo purposes, it is decided beforehand in this stage

        int[] NUM_AGENTS = new int[] { 1, 1 };

        GameObject go;
        IGameCharacter agent;

        if (NUM_AGENTS.Length != enemyPrefabs.Count)
        {
            return;     // ¿Repeat the enemy assignation process? 
        }

        for (int i = 0; i < NUM_AGENTS.Length; i++)
        {
            for (int spawn = 0; spawn < NUM_AGENTS[i]; spawn++)
            {
                go = Instantiate(enemyPrefabs[i]);
                agent = go.GetComponent<IGameCharacter>();

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

                battleUnits_.Add(go.GetComponent<IGameCharacter>());
                go.GetComponent<IGameCharacter>().ID = battleUnits_.Count - 1;
            }
        }

    }

    void InitializeAgents()
    {
        GameObject go;
        IGameCharacter agent;

        // Ensure all agents are active
        for (int i = 0; i < battleUnits_.Count; i++)
        {
            battleUnits_[i].GameObject.SetActive(true);
            battleUnits_[i].IsActive = true;
            battleUnits_[i].ResetStats();
        }

        /**
         *  IDEA: Get the enemy spawn pool from mapFile
         *  - Available positions are randomly shuffled into a Vector2Int queue
         *  - Then distributed one by one into the enemies on a loop
         */

        Vector2Int[] keys = new Vector2Int[] {
            new Vector2Int(2, 3) , 
            new Vector2Int(2, 1) 
        };                                        // Fixed for testing purposes only!

        if (keys.Length != enemyPrefabs.Count)
        {
            return;     // ¿Repeat the enemy assignation process? 
        }

        // Place the agents on the battleMap (physically and logically)
        for (int i = 0; i < keys.Length; i++)
        {
            agent = battleUnits_[i];
            go = agent.GameObject;

            agent.InGamePosition = keys[i];
            go.transform.position = HexCalculator.CharacterPosition(keys[i]);

            mapTiles[keys[i]].Occupier = agent;
        }
    }

    void InitializeCaroussel()
    {
        Caroussel_R.Instance.CalculateICVs();
        Caroussel_R.Instance.PreCalculateTurns();
    }

    IEnumerator GameLoop()
    {
        //Debug.LogError("LOOP STARTED");

        bool stopCondition = false;
        bool recalcTurns = false;
        int index;

        while (!stopCondition)
        {
            //Debug.LogWarning("Turn!");

            index = Caroussel_R.Instance.NextTurnOwner();
            Caroussel_R.Instance.actionInfo.TurnOwner = battleUnits_[index];

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
                Caroussel_R.Instance.PassTurn();

                if (recalcTurns)
                {
                    // Re-calculate turns
                    Caroussel_R.Instance.PreCalculateTurns();
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
        if (Caroussel_R.Instance.actionInfo.WhoDied_.Count > 0)
        {
            for (int i = 0; i < Caroussel_R.Instance.actionInfo.WhoDied_.Count; i++)
            {
                battleUnits_[Caroussel_R.Instance.actionInfo.WhoDied_[i]].Die();
            }

            Caroussel_R.Instance.actionInfo.WhoDied_.Clear();

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

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Obstacle"))
        {
            Destroy(go);
        }
    }

    void EpisodeReset()
    {
        //Debug.LogError("RESET IS CALLED");

        for (int i = 0; i < battleUnits_.Count; i++)
        {
            battleUnits_[i].Reset();
        }

        Initialize().Invoke();
    }
}

