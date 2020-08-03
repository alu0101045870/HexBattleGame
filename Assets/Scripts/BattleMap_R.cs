using MLAgents;
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
    public List<TextAsset> mapFiles;

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
        InitializeMap();     
        InitializeAgents();         // =>   Initialize agents + their positions
        InitializeCaroussel();

        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// Initializes Map Tiles Based on one Map File chosen at random.
    /// Agent positions are 
    /// </summary>
    void InitializeMap()
    {
        string[] mapData = mapFiles[UnityEngine.Random.Range(0, mapFiles.Count)].text.Split(' ', '\n');

        // TODO: clean data, in case there is rubbish between numbers
        // ----------------------------------------------------
        Vector2Int position;
        GameObject tile;
        int maxRow = int.MinValue, maxCol = int.MinValue, minRow = int.MaxValue, minCol = int.MaxValue;
        int row, col;

        // TODO: Add number of enemies constraint, spawn locations, etc

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

    void InitializeAgents()
    {
        // ----------------------------------------------------------------------  TEMPORARY IMPLEMENTATION
        const int NUM_LUPUS = 1;
        const int NUM_RED_NOSED_HARE = 1;

        GameObject go;
        IGameCharacter agent;
        
        Vector2Int key = new Vector2Int(2, 3);      // Fixed for testing purposes only!
        Vector2Int key2 = new Vector2Int(2, 1);

        for (int i = 0; i < NUM_LUPUS; i++)
        {
            // Enemies are child objects of the tile they are in
            go = Instantiate(lupusPrefab);
            go.transform.position = HexCalculator.CharacterPosition(key);

            agent = go.GetComponent<Lupus_R>();
            mapTiles[key].Occupier = agent;
            agent.InGamePosition = key;

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

            battleUnits_.Add(go.GetComponent<Lupus_R>());
        }

        // ----------------------------------------------------------------- GENERALIZE THIS BIT IN THE FUTURE

        for (int i = 0; i < NUM_RED_NOSED_HARE; i++)
        {
            go = Instantiate(rnhPrefab);
            go.transform.position = HexCalculator.CharacterPosition(key2);

            agent = go.GetComponent<RedNosedHare_R>();
            mapTiles[key2].Occupier = agent;
            agent.InGamePosition = key2;

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

            battleUnits_.Add(go.GetComponent<RedNosedHare_R>());
        }
    }

    void InitializeCaroussel()
    {
        Caroussel_R.Instance.CalculateICVs();
        Caroussel_R.Instance.PreCalculateTurns();
    }

    IEnumerator GameLoop()
    {
        bool stopCondition = false;
        int index;
        
        while (!stopCondition)
        {
            Debug.LogWarning("Turn!");

            index = Caroussel_R.Instance.NextTurnOwner();

            for (int j = 0; j < battleUnits_[index].GetStatValueByName("ACT"); j++)
            {
                battleUnits_[index].RequestAct();

                //yield return new WaitForSeconds(2f);
                yield return new WaitUntil(() => battleUnits_[index].TurnOver);
                yield return new WaitForSeconds(1f);

                battleUnits_[index].TurnOver = false;
            }
            Caroussel_R.Instance.PassTurn();

            // Check end of loop conditions
            // - [IGameCharacters health > 0]

        }
    }
}
