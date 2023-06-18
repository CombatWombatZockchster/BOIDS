/*
*  Written by Jonas H.
*
*  This is a single swarm of BOIDS.
*  It keeps track of multiple swarm units.
*  Suppplies list of of neighbours within vision range to individual units
*  Uses a spatial hashing for increased performance.
*/

using System.Collections.Generic;
using UnityEngine;

public class BOIDSwarm : MonoBehaviour
{
#region Parametes
    [Tooltip("How far will the units be able to percieve others")]
    public float maxVisionDistance = 60.0f;
    [Tooltip("Maximum number of units taken into account for steering calculations")]
    public uint maxUnitsConsideredClose = 20;
    [Tooltip("Point to steer towards")]
    public Transform target;

    [Header("Steering Behaviour Strengths")]
    [Tooltip("Face the same direction as neighbours")]
    [SerializeField] float alignment = 1.4f;
    [Tooltip("Stay close to neighbours")]
    [SerializeField] float cohesion = 0.5f;
    [Tooltip("Keep distance from neighbours")]
    [SerializeField] float seperation = 0.75f;
    [Tooltip("Move towars goal position")]
    [SerializeField] float steering = 0.2f;
    [Tooltip("Avoid physical obstacles")]
    [SerializeField] float avoidance = 10.0f;
    #endregion

    // Store all units
    // Hash grid for improved performance
    // So units do not need to loop over every other unit
    Dictionary<Vector3Int, List<BOIDUnit>> boidGrids = new Dictionary<Vector3Int, List<BOIDUnit>>();

    /// <summary>
    /// Registers unit & sets unit settings to fit swarm
    /// </summary>
    public void addUnit(BOIDUnit unit)
    {
        // Apply settings to unit
        unit.maxVisionDistance = maxVisionDistance;
        unit.weights = new float[] {alignment, cohesion, seperation, steering, avoidance};
        if (target == null) target = transform;
        unit.target = target;

        // Remember unit
        // Store unit in spatial grid
        Vector3Int cell = posToGridCell(unit.transform.position);
        List<BOIDUnit> list;
        if (boidGrids.TryGetValue(cell, out list))
        {
            // Add to list, if grid cell already exits
            list.Add(unit);
        }
        else
        {
            // Else create grid cell & then register there
            list = new List<BOIDUnit>();
            list.Add(unit);
            boidGrids.Add(cell, list);
        }
    }
    
    /// <summary>
    /// Remove unit from data structure
    /// </summary>
    public void removeUnit(BOIDUnit unit)
    {
        //FIXME: does nothing. unit is no longer where I  thought it was
        /*
        Vector3Int cell = posToGridCell(unit.transform.position);
        List<BOIDUnit> list;
        boidGrids.TryGetValue(cell, out list);
        list.Remove(unit);
        */
        // Does not do anything
    }

    // Update spatial hash grid cell positions
    // TODO: units should probably update their own positions when moving 
    void FixedUpdate()
    {
        // Units to be reasigned, because they have moved senificantly
        List<KeyValuePair<Vector3Int, BOIDUnit>> reassign = new List<KeyValuePair<Vector3Int, BOIDUnit>>();
        foreach(var item in boidGrids)
        {
            Vector3Int lastCell = item.Key;
            List<BOIDUnit> units = item.Value;

            // Check every unit
            for (int i = units.Count-1; i >= 0; i--)
            {
                BOIDUnit unit = units[i];

                if (unit == null) // In case I messed up cleanup of a destroyed unit
                    continue;

                Vector3Int currentCell = posToGridCell(unit.transform.position);

                // Unit has moved to a new grid cell
                if(currentCell != lastCell)
                {
                    units.Remove(unit);
                    reassign.Add(new KeyValuePair<Vector3Int, BOIDUnit>(currentCell, unit));
                }
            }          
        }

        // Assign moved units to their new grid cells
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


    /// <summary>
    /// Return all units within vision range
    /// </summary>
    public List<BOIDUnit> getCloseUnits(Vector3 center)
    {
        // Cell of the observer
        Vector3Int cell = posToGridCell(center);
        // Adjacent cells (also needed, because visionRange = grid size)
        Vector3Int[] cells =
        {
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

        // Store results here
        List<BOIDUnit> res = new List<BOIDUnit>();

        // Number of units we will return so far. 
        // We might not return all possible units,
        // but just call it quits if we reach a given maximum
        // to limit performance cost
        // leads to slight artifacts
        uint consideredUnits = 0;

        // Look at all relevent (adjacent) grid cells
        foreach(Vector3Int gridCell in cells)
        {
            // All units within this cell
            List<BOIDUnit> cellList;
            if(boidGrids.TryGetValue(cell, out cellList))
            {
                // Look at all units within this cell
                for(int i = cellList.Count-1; i >= 0; i--)
                {
                    var unit = cellList[i];
                    
                    // In case a unit was not properly removed
                    if(unit == null)
                    {
                        cellList.Remove(unit);
                        continue;
                    }

                    // If the unit is within the vision distance
                    float dis = Vector3.Distance(unit.transform.position, center);
                    if (dis <= maxVisionDistance)
                    {
                        // Add it to the return list
                        res.Add(unit);
                        consideredUnits++;

                        // Just call it quits, if there is to much going on                       
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

    
    /// <summary>
    /// Convert world coordinates to grid cell indices
    /// </summary>
    public Vector3Int posToGridCell(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x / maxVisionDistance);
        int y = Mathf.RoundToInt(pos.y / maxVisionDistance);
        int z = Mathf.RoundToInt(pos.z / maxVisionDistance);

        return new Vector3Int(x, y, z);
    }


    // In the Editor, visualize the spatial hash grid
    private void OnDrawGizmosSelected()
    {
        // For all the cells
        foreach(Vector3Int cell in boidGrids.Keys)
        {
            // Ignore empty cells
            List<BOIDUnit> units;
            boidGrids.TryGetValue(cell, out units);
            if (units.Count == 0)
                continue;

            // Convert cell index to worl space
            Vector3 pos = (Vector3)cell * maxVisionDistance;

            // Draw grid cell outline
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(pos, Vector3.one * maxVisionDistance);

            // Indicate number of units within cell
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.Label(pos, "" + units.Count);
        }
    }
}