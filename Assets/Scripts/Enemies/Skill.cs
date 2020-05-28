using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface ISkill
{
    IEnumerator Exec(IGameCharacter caller, List<GameObject> targets);
}

public abstract class Skill : ISkill
{
    protected int range_;
    protected int skillRank_;
    protected string skillName_;

    public int SkillRank
    {
        get { return skillRank_; }
    }

    public string SkillName
    {
        get { return skillName_; }
    }

    public abstract IEnumerator Exec(IGameCharacter caller, List<GameObject> targets);

    public virtual bool AreTargetsInRange(Vector2Int callerPos, Dictionary<Vector2Int, GameObject> mapTiles, 
        List<GameObject> groupOfInterest, List<GameObject> targetsInRange)
    {
        int range = range_;
        if (range == 0)
            return true;

        // ----------------------------------------------------------------------

        HexTile currentTile = mapTiles[callerPos].GetComponent<HexTile>();

        List<HexTile> neighbors = currentTile.Neighbors;
        HashSet<HexTile> visited = new HashSet<HexTile>(neighbors);

        //Debug.Log("Group Of interest: " + groupOfInterest.Count);
        //Debug.Log("Neighbors init: " + neighbors.Count);

        visited.Add(currentTile);       // Don't forget to add initial tile to avoid own check

        FindTargets(groupOfInterest, range - 1, neighbors, visited, targetsInRange);

        //Debug.Log("Targets: " + targetsInRange.Count);

        return (targetsInRange.Count > 0);
    }

    private void FindTargets(List<GameObject> groupOfInterest, int range, 
        List<HexTile> neighbors, HashSet<HexTile> visited, List<GameObject> targetsInRange)
    {
        foreach (HexTile tile in neighbors)
        {
            // if target in tile position
            foreach (GameObject target in groupOfInterest)
            {

               // Debug.Log("Tile Position: " + tile.Position.ToString() +
                 //   "\nComponent Position: " + target.GetComponent<IGameCharacter>().GetInGamePosition().ToString());

                if (target.GetComponent<IGameCharacter>().GetInGamePosition() == tile.Position)
                {
                    targetsInRange.Add(target);
                }
            }
        }

        //Debug.Log("Range (inside):" + range);
        //Debug.Log("Targets in Range:" + targetsInRange.Count);

        if (range > 0 && (targetsInRange.Count < groupOfInterest.Count))
        {
            foreach(HexTile neighbor in neighbors)
            {
                // We want neighbor's neighbors that are NOT already in the set
                // Then we want to add them all to said set and follow recursive algorithm one layer down

                IEnumerable<HexTile> difference = new HashSet<HexTile>(neighbor.Neighbors).Except(visited);

                foreach (HexTile nonVisited in difference)
                {
                    visited.Add(nonVisited);
                }

                FindTargets(groupOfInterest, range - 1, difference.ToList(), visited, targetsInRange);
            }
        }
    }
}

public class Bite : Skill
{
    public Bite()
    {
        skillRank_ = 3;
        skillName_ = "Bite";

        range_ = 1;
    }

    override public IEnumerator Exec(IGameCharacter caller, List<GameObject> targets)
    {
        Debug.Log(caller.Name + " used BITE on " + targets[0].GetComponent<IGameCharacter>().Name);
        yield return new WaitForSeconds(0.1f);
    }
}

public class Move : Skill
{
    private int hex_;            // Amount of cells the unit can move
    private List<HexTile> path_ = new List<HexTile>();

    public Move(int hex)
    {
        skillRank_ = 3;
        skillName_ = "Move";

        hex_ = hex;
        range_ = 4;
    }

    public override bool AreTargetsInRange(Vector2Int callerPos, Dictionary<Vector2Int, GameObject> mapTiles,
        List<GameObject> groupOfInterest, List<GameObject> targetsInRange)
    {
        // TODO: Range check

        // AD-HOC PATHFINDING ALGORITHM (RANDOM PATHING)

        HexTile currentTile = mapTiles[callerPos].GetComponent<HexTile>();
        List<HexTile> neighbors = currentTile.Neighbors;
        path_.Clear();

        AdHocPathFinding(neighbors, hex_);

        return true;
    }

    private void AdHocPathFinding(List<HexTile> neighbors, int hex)
    {
        if (hex <= 0)
            return;
        HexTile nextTile = neighbors[Random.Range(0, neighbors.Count)];
        path_.Add(nextTile);

        AdHocPathFinding(nextTile.Neighbors, hex - 1);
    }

    public override IEnumerator Exec(IGameCharacter caller, List<GameObject> targets)
    {
        if (path_.Count != hex_)
            throw new System.Exception();

        HexTile tile;

        for (int i = 0; i < path_.Count; i++)
        {
            tile = path_[i].GetComponent<HexTile>();
            Debug.Log(tile.Position.x + ", " + tile.Position.y);

            ShowMovement(caller, tile);
            yield return null;
        }

        Debug.Log(caller.Name + "moved (randomly)!");

    }

    private void ShowMovement(IGameCharacter caller, HexTile tile)
    {
        caller.GetGameObject().transform.position = HexCalculator.CharacterPosition(tile.Position.y, tile.Position.x);
    }
}

public class Defend : Skill
{
    public Defend()
    {
        skillRank_ = 3;
        skillName_ = "Defend";

        range_ = 0;
    }

    public override IEnumerator Exec(IGameCharacter caller, List<GameObject> targets)
    {
        yield return new WaitForSeconds(0.2f);
    }
}

// ---------------------------------------------------------------------------------------
/*                            TURN-INFO AND HELPER DELEGATE                             */
// ---------------------------------------------------------------------------------------

public delegate void TurnEnded(TurnInfo turnInfo);

public struct TurnInfo
{
    private float damageDone_;
    private string moveUsed_;             
    private float m_HealthLeft;             
    private bool m_TurnOver;
    public float DamageDone
    {
        get { return damageDone_; }
        set { damageDone_ = value; }
    }
    public string MoveUsed
    {
        get { return moveUsed_; }
        set { moveUsed_ = value; }
    }
    public float HealthLeft
    {
        get { return m_HealthLeft; }
        set { m_HealthLeft = value; }
    }   
    public bool TurnOver
    {
        get { return m_TurnOver; }
    }
    
    public TurnInfo(float damage, string moveUsed, float healthLeft, bool turnOver)
    {
        damageDone_ = damage;
        moveUsed_ = moveUsed;
        m_HealthLeft = healthLeft;
        m_TurnOver = turnOver;
    }
}