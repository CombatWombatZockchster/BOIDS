using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BOIDSwarm : MonoBehaviour
{

    #region singleton
    private static BOIDSwarm singleton;
    public static BOIDSwarm getSingleton()
    {
        if(singleton == null)
        {
            singleton = new BOIDSwarm();
        }

        return singleton;
    }


    private void Awake()
    {
        if (singleton != null)
        {
            Debug.LogWarning("BOID Swarm singleton - two instances created");
            Destroy(this);
        }
        else
        {
            singleton = this;
        }
    }

    #endregion singleton

    // maybe use pooling?
    List<BOIDUnit> boids = new List<BOIDUnit>();
    public float maxDistance = 80.0f;

    Dictionary<Vector3Int, List<BOIDUnit>> boidGrids = new Dictionary<Vector3Int, List<BOIDUnit>>();
    
    public void addUnit(BOIDUnit unit)
    {
        boids.Add(unit);
        unit.maxDistance = maxDistance;

        Vector3Int cell = posToGridCell(unit.transform.position);
        List<BOIDUnit> list;
        if (boidGrids.TryGetValue(cell, out list))
        {
            list.Add(unit);
        }
        else
        {
            list = new List<BOIDUnit>();
            list.Add(unit);
            boidGrids.Add(cell, list);
        }
    }
    
    public void removeUnit(BOIDUnit unit)
    {
        /*
        boids.Remove(unit);
        Vector3Int cell = posToGridCell(unit.transform.position);
        List<BOIDUnit> list;
        boidGrids.TryGetValue(cell, out list);
        list.Remove(unit);
        */
        // Does not do anything
    }


    void FixedUpdate()
    {
        List<KeyValuePair<Vector3Int, BOIDUnit>> reassign = new List<KeyValuePair<Vector3Int, BOIDUnit>>();
        foreach(var item in boidGrids)
        {
            Vector3Int lastCell = item.Key;
            List<BOIDUnit> units = item.Value;

            for (int i = units.Count-1; i >= 0; i--)
            {
                BOIDUnit unit = units[i];
                if (unit == null)
                    continue;

                Vector3Int currentCell = posToGridCell(unit.transform.position);

                if(currentCell != lastCell)
                {
                    units.Remove(unit);
                    reassign.Add(new KeyValuePair<Vector3Int, BOIDUnit>(currentCell, unit));
                }
            }          
        }

        foreach(KeyValuePair<Vector3Int, BOIDUnit> pair in reassign)
        {
            List<BOIDUnit> list;
            if(boidGrids.TryGetValue(pair.Key, out list))
            {
                list.Add(pair.Value);
            }
            else
            {
                list = new List<BOIDUnit>();
                list.Add(pair.Value);
                boidGrids.Add(pair.Key, list);
            }  
        }
    }

    public uint maxUnitsConsideredClose = 20;

    public List<BOIDUnit> getCloseUnits(Vector3 center)
    {
        Vector3Int cell = posToGridCell(center);
        Vector3Int[] cells =
        {
            /*
            cell,
            (cell + new Vector3Int(-1, 1)),     (cell + new Vector3Int(0, 1)),      (cell + new Vector3Int(1, 1)),
            (cell + new Vector3Int(-1, 0)),                                         (cell + new Vector3Int(1, 0)),
            (cell + new Vector3Int(-1, -1)),    (cell + new Vector3Int(0, -1)),     (cell + new Vector3Int(1, -1))
            */

            // Center layer
            (cell + new Vector3Int(-1, 0, 1)), (cell + new Vector3Int(0, 0, 1)), (cell + new Vector3Int(1, 0, 1)),
            (cell + new Vector3Int(-1, 0, 0)), (cell + new Vector3Int(0, 0, 0)), (cell + new Vector3Int(1, 0, 0)),
            (cell + new Vector3Int(-1, 0, -1)), (cell + new Vector3Int(0, 0, -1)), (cell + new Vector3Int(1, 0, -1)),
            
            // Top layer
            (cell + new Vector3Int(-1, 1, 1)), (cell + new Vector3Int(0, 1, 1)), (cell + new Vector3Int(1, 1, 1)),
            (cell + new Vector3Int(-1, 1, 0)), (cell + new Vector3Int(0, 1, 0)), (cell + new Vector3Int(1, 1, 0)),
            (cell + new Vector3Int(-1, 1, -1)), (cell + new Vector3Int(0, 1, -1)), (cell + new Vector3Int(1, 1, -1)),
            
            // Bottom layer
            (cell + new Vector3Int(-1, -1, 1)), (cell + new Vector3Int(0, -1, 1)), (cell + new Vector3Int(1, -1, 1)),
            (cell + new Vector3Int(-1, -1, 0)), (cell + new Vector3Int(0, -1, 0)), (cell + new Vector3Int(1, -1, 0)),
            (cell + new Vector3Int(-1, -1, -1)), (cell + new Vector3Int(0, -1, -1)), (cell + new Vector3Int(1, -1, -1))

        };

        List<BOIDUnit> res = new List<BOIDUnit>();

        uint consideredUnits = 0;
        foreach(Vector3Int gridCell in cells)
        {
            List<BOIDUnit> cellList;
            if(boidGrids.TryGetValue(cell, out cellList))
            {
                // foreach (BOIDUnit unit in cellList)
                for(int i = cellList.Count-1; i >= 0; i--)
                {
                    var unit = cellList[i];
                    if(unit == null)
                    {
                        cellList.Remove(unit);
                        continue;
                    }

                    float dis = Vector3.Distance(unit.transform.position, center);
                    if (dis <= maxDistance)
                    {
                        res.Add(unit);
                        consideredUnits++;

                        if(consideredUnits >= maxUnitsConsideredClose)
                        {
                            return res;
                        }
                    }
                }
            }
        }

        return res;
    }

    
    public Vector3Int posToGridCell(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / maxDistance);
        int y = Mathf.RoundToInt(pos.y / maxDistance);
        int z = Mathf.RoundToInt(pos.z / maxDistance);

        return new Vector3Int(x, y, z);
    }


    private void OnDrawGizmosSelected()
    {
        foreach(Vector3Int cell in boidGrids.Keys)
        {
            List<BOIDUnit> units;
            boidGrids.TryGetValue(cell, out units);
            if (units.Count == 0)
                continue;

            Vector3 pos = (Vector3)cell * maxDistance;

            /*
            Vector3[] verts =
            {
                new Vector3(x - maxDistance * 0.5f, 0, y - maxDistance * 0.5f),
                new Vector3(x - maxDistance * 0.5f, 0, y + maxDistance * 0.5f),
                new Vector3(x + maxDistance * 0.5f, 0, y + maxDistance * 0.5f),
                new Vector3(x + maxDistance * 0.5f, 0, y - maxDistance * 0.5f),
            };
            UnityEditor.Handles.DrawSolidRectangleWithOutline(verts, new Color(0.1f, 0.1f, 0.1f, 0.1f), Color.black);
            */

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(pos, Vector3.one * maxDistance);

            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(pos, "" + units.Count);
        }
    }
}