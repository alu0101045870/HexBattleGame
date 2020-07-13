using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleMap : MonoBehaviour
{
    public static BattleMap Instance { get; private set; }

    public GameObject hexTilePrefab;
    public List<TextAsset> mapFiles;
    public Caroussel turnCaroussel;

    public Dictionary<Vector2Int, GameObject> mapTiles = new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        // Singleton pattern implementation: no more than a single instance of BattleMap on scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitMap();
        InitCaroussel();            // InGameCharacters are also initialized inside the caroussel

        //InitReward();             // -- Agent Shenanigans

        GameLoop();
    }

    private void InitMap()
    {
        string[] mapData = mapFiles[UnityEngine.Random.Range(0, mapFiles.Count)].text.Split(' ', '\n');

        // TODO: clean data, in case there is rubbish between numbers
        // ----------------------------------------------------
        Vector2Int position;
        GameObject tile;

        int i = 0;
        while(i < mapData.Length)
        {
            position = new Vector2Int(int.Parse(mapData[i]), int.Parse(mapData[i + 1]));
            tile = Instantiate(hexTilePrefab, HexCalculator.Position(position.y, position.x), Quaternion.identity, this.transform);

            tile.GetComponent<HexTile>().Position = position;
            mapTiles.Add(position, tile);

            i += 2;
        }

        // For each tile, set its neighbors in the HexTile component
        HexCalculator.SetNeighborsInMap(mapTiles);
    }

    private void InitCaroussel()
    {
        turnCaroussel.Initialize();
        LocateBattleUnits();
    }

    private void LocateBattleUnits()
    {
        List<IGameCharacter> battleUnits = turnCaroussel.GetBattleUnits();
        Vector2Int position;
        GameObject tileT;
        GameObject go;

        int size = mapTiles.Count;
        List<Vector2Int> keys = mapTiles.Keys.ToList<Vector2Int>();

        foreach (IGameCharacter gc in battleUnits)
        {
            position = keys[UnityEngine.Random.Range(0, size)];         // TODO: Avoid positioning units in the same HexTile
            tileT = mapTiles[position];
            
            go = gc.GetGameObject();
            gc.SetInGamePosition(go, position.x, position.y);
            go.transform.SetParent(tileT.transform);
        }

        // Ad-hoc process for test
        //battleUnits[0].SetInGamePosition(battleUnits[0].GetGameObject(), 3, 2);
        //battleUnits[1].SetInGamePosition(battleUnits[1].GetGameObject(), 2, 3);

    }

    private void InitReward()
    {
        throw new NotImplementedException();
    }

    private void GameLoop()
    {
        bool stopCondition = false;
        //int i = 0;

        while(!stopCondition)          // Take turns eternally
        {
            turnCaroussel.NextTurn();

            turnCaroussel.PassTurn();

            stopCondition = true;
        }

        // Check last turn taken information and decide wether to calculate
        // the whole caroussel again or just the next turn in the list

        // Status ailments and events take place

        // if EnemyDead => Remove from the game
    }
}
