using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingArea : MonoBehaviour
{
    private BattleMap[] battleMapList_;
    private Camera cameraList_;

    [SerializeField] private int VertOffset;
    [SerializeField] private int HorizOffset;

    private void Awake()
    {
    
        // Apply vertical and horizontal spacing between BattleMaps
    }

    // Controls the spectator view (switches between cameras on press)
    private void FixedUpdate()
    {
        
    }
}
