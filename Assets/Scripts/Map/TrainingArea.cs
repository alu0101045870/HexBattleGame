using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class TrainingArea : MonoBehaviour
{
    private const int MAX_ROWS = 4;
    private const int MAX_COLS = 4;

    private BattleMap[] battleMapList_;
    private Camera[] cameraList_;
    private int currentCameraIndex = 0;

    [SerializeField] private GameObject battleMapPrefab;
    [SerializeField] private int numberOfMaps;
    [SerializeField] private int VertOffset;
    [SerializeField] private int HorizOffset;

    private void Awake()
    {

        if (numberOfMaps > MAX_COLS * MAX_ROWS) 
            numberOfMaps = MAX_COLS * MAX_ROWS;

        if (numberOfMaps < 1)
            numberOfMaps = 1;

        battleMapList_ = new BattleMap[numberOfMaps];
        cameraList_ = new Camera[numberOfMaps];

        int j = 0, i = 0;

        for (int k = 0; k < battleMapList_.Length; k++)
        {
            battleMapList_[k] = Instantiate(battleMapPrefab, this.transform).GetComponent<BattleMap>();

            battleMapList_[k].OffsetCol = HorizOffset * i;
            battleMapList_[k].OffsetRow = VertOffset * j;
            battleMapList_[k].transform.position = HexCalculator.Position((HorizOffset * i), (VertOffset * j)) + new Vector3(0, 0, -1);

            cameraList_[k] = battleMapList_[k].GetComponentInChildren<Camera>();
            cameraList_[k].enabled = false;

            battleMapList_[k].StartMap();

            if (j >= MAX_COLS - 1)
            {
                j = 0;

                if (i >= MAX_ROWS) break;
                else i++;
            }
            else
            {
                j++;
            }
        }

        cameraList_[currentCameraIndex].enabled = true;
    }

    // Controls the spectator view (switches between cameras on press)
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            cameraList_[currentCameraIndex].enabled = false;

            if (++currentCameraIndex >= cameraList_.Length)
                currentCameraIndex = 0;

            cameraList_[currentCameraIndex].enabled = true;
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            cameraList_[currentCameraIndex].enabled = false;

            if (--currentCameraIndex < 0)
                currentCameraIndex = cameraList_.Length - 1;

            cameraList_[currentCameraIndex].enabled = true;
        }

    }
}
