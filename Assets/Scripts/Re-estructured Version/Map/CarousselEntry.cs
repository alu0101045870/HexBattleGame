using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ICarousselEntry
{
    int GetTurnOwner();
    void SetTurnOwner(int index, string name);
}

public class CarousselEntry : MonoBehaviour, ICarousselEntry
{
    // ---------------------------------------------------------------------------------------
    /*                                    CLASS MEMBERS                                     */
    // ---------------------------------------------------------------------------------------

    private int turnOwnerIndex_;        // Reference to the owner of this turn
    private string turnOwnerName_;

    // ---------------------------------------------------------------------------------------
    /*                              INTERFACE IMPLEMENTATION                                */
    // ---------------------------------------------------------------------------------------

    public int GetTurnOwner()
    {
        return turnOwnerIndex_;
    }

    public void SetTurnOwner(int index, string name)
    {
        turnOwnerIndex_ = index;
        turnOwnerName_ = name;

        // Reflect the changes onscreen too
        Text nameTag = transform.GetChild(0).transform.GetComponent<Text>();
        nameTag.text = turnOwnerName_;
    }
}

