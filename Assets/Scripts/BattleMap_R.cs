using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMap_R : MonoBehaviour
{
    public static BattleMap_R Instance { get; private set; }

    public GameObject hexTilePrefab;
    public GameObject lupusPrefab;
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

        // TODO: Add number of enemies constraint, spawn locations, etc

        int i = 0;
        while (i < mapData.Length)
        {
            position = new Vector2Int(int.Parse(mapData[i]), int.Parse(mapData[i + 1]));
            tile = Instantiate(hexTilePrefab, HexCalculator.Position(position.y, position.x), Quaternion.identity, this.transform);

            tile.GetComponent<HexTile>().Position = position;
            mapTiles.Add(position, tile.GetComponent<HexTile>());

            i += 2;
        }

        // For each tile, set its neighbors in the HexTile component
        HexCalculator.SetNeighborsInMap(mapTiles);
    }

    void InitializeAgents()
    {
        // TEMPORARY IMPLEMENTATION
        const int NUM_LUPUS = 1;
        //const int NUM_RED_NOSED_HARE = 1;

        GameObject go;
        IGameCharacter agent;
        Vector2Int key = new Vector2Int(2, 3);      // Fixed for testing purposes only!

        for (int i = 0; i < NUM_LUPUS; i++)
        {
            // Enemies are child objects of the tile they are in
            go = Instantiate(lupusPrefab, mapTiles[key].transform);
            go.transform.position = HexCalculator.CharacterPosition(key);

            agent = go.GetComponent<Lupus_R>();
            mapTiles[key].OccupiedBy = agent;
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
    }

    void InitializeCaroussel()
    {
        Caroussel_R.Instance.CalculateICVs();
        Caroussel_R.Instance.PreCalculateTurns();
    }

    IEnumerator GameLoop()
    {
        int index;

        for (int i = 0; i < 5; i++)
        {
            Debug.LogWarning("Turn!");

            index = Caroussel_R.Instance.NextTurnOwner();

            battleUnits_[index].RequestAct();

            yield return new WaitForSeconds(1f);
            
            Caroussel_R.Instance.PassTurn();
        }
    }
}
