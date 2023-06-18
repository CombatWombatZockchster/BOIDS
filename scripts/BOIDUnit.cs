using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BOIDUnit : MonoBehaviour
{
    // Settings

    // Settings for individual units
    public float turnSpeed = 1.0f;
    public float moveSpeed = 5.0f;

    // Will be overriden by the mySwarm
    [HideInInspector] public float[] weights;
    [HideInInspector] public float maxVisionDistance = 5.0f;
    [HideInInspector] public Transform target;


    // Variables needed for Functionality

    // Management of close units
    [HideInInspector] public BOIDSwarm mySwarm;
    List<BOIDUnit> closeUnits = new List<BOIDUnit>();

    // Movement via physics
    Rigidbody rigidB;

#region Swarm Management
    private void Start()
    {
        // Gather all needed references

        rigidB = GetComponent<Rigidbody>();

        // Register to appropiate swarm, so I can get info on close units
        if (mySwarm == null)
            mySwarm = transform.parent.GetComponent<BOIDSwarm>();
        mySwarm.addUnit(this);
    }   

    // Should I ever need to "respawn" at any given point
    public void addMe(Vector3 pos)
    {
        transform.position = pos;
        mySwarm.addUnit(this);
    }

    // Clean up after myself
    public void removeMe()
    {
        mySwarm.removeUnit(this);
    }
    private void OnDestroy()
    {
        removeMe();
    }
#endregion

    // Move using physics
    // TOODO: maybe move this into one single call on the swarm
    private void FixedUpdate()
    {
        // Find all neighbours
        closeUnits = mySwarm.getCloseUnits(transform.position);
        closeUnits.Remove(this);

        // Decide where to go
        Vector3 dir = computeBoidDirection();
        Vector3 forward = transform.forward;
        Vector3 smoothed = Vector3.Slerp(forward, dir, turnSpeed * Time.deltaTime * Time.timeScale);
        
        // Actually go there
        Vector3 vel = smoothed.normalized * moveSpeed;
        rigidB.velocity = vel;

        // Look where youre going
        transform.LookAt(transform.position + vel, Vector3.up);
    }

    /// <summary>
    /// How much should I care about something else
    /// </summary>
    /// <returns>Importance calculated trough distance</returns>
    float influence(Vector3 otherPos)
    {
        // Influence is highest when very close, and lowest when far away
        float distance = Vector3.Distance(transform.position, otherPos);
        float linear = Mathf.Clamp01(distance / maxVisionDistance);
        return linear * linear; // quadratic
    }


#region Steering Behaviours
    /// <summary>
    /// Calculate all the individual steeting behaviours and combine them.
    /// </summary>
    Vector3 computeBoidDirection()
    {
        Vector3 direction = Vector3.zero;

        // Some Behaviours make no sense, if alone
        if(closeUnits.Count != 0)
        {
            direction += computeAlignment() * weights[0];
            direction += computeCohesion()  * weights[1];
            direction += computeSeperation()* weights[2];
        }

        direction += computeSteering()  * weights[3];
        direction += computeAvoidance() * weights[4];

        //Debug.DrawLine(transform.position, transform.position + computeAvoidance()* maxVisionDistance/2.0f, Color.red, Time.fixedDeltaTime);
        
        // Take the average
        direction.Normalize();
        return direction;
    }

    /// <summary>
    /// Keep a minimum distance from my neighbours
    /// </summary>
    Vector3 computeSeperation()
    {
        Vector3 direction = Vector3.zero;

        // For each neighbour, find direction facing away
        foreach (BOIDUnit unit in closeUnits)
        {
            float multiplier = influence(unit.transform.position);
            direction += (transform.position - unit.transform.position) * multiplier;
        }

        // Take weighted average
        if (direction != Vector3.zero)
            direction.Normalize();
        return direction;
    }

    /// <summary>
    /// Look the same way as my neighbours
    /// </summary>
    Vector3 computeAlignment()
    {
        Vector3 direction = Vector3.zero;

        // For each neighbour, look up forward direction
        foreach (BOIDUnit unit in closeUnits)
        {
            float multiplier = influence(unit.transform.position);
            direction += unit.transform.forward.normalized * multiplier;
        }

        // Take weighted average
        if (direction != Vector3.zero)
            direction.Normalize();
        return direction;
    }

    /// <summary>
    /// Keep close to my local cluster of neighbours
    /// </summary>
    Vector3 computeCohesion()
    {
        Vector3 center = Vector3.zero;

        // Calculate average position of my neighbours
        foreach (BOIDUnit unit in closeUnits)
        {
            center += unit.transform.position;
        }
        center /= closeUnits.Count;


        // Return direction to neighbours
        if(closeUnits.Count > 0)
            return (center - transform.position).normalized;
        else
            return Vector3.zero;
    }

    /// <summary>
    /// Steer towards my goal
    /// </summary>
    Vector3 computeSteering()
    {
        // Cover edge cases
        Vector3 to;
        if (target == null)
            to = new Vector3();
        else
            to = target.transform.position;

        // Calc direction
        Vector3 diff = (to - transform.position);
        Vector3 dir = diff.normalized;
        
        return dir;
    }

    /// <summary>
    /// Avoid colliding with scenery
    /// </summary>
    Vector3 computeAvoidance()
    {
        Vector3 direction = transform.forward;

        // As long as my course will lead to a collision, look for a new course
        float angle = 0;
        bool clear = false;
        while(!clear)
        {
            // Is there an obstacle along my current course?
            clear = !Physics.Raycast(transform.position, direction, maxVisionDistance / 2.0f);
            //Debug.DrawLine(transform.position, transform.position + direction * maxVisionDistance/2.0f, Color.green, Time.fixedDeltaTime);

            if(clear)
            {
                // I found a clear course
                if(angle == 0)
                    return new Vector3();
                else return direction;
            }
            else
            {
                // Look for a new course
                // Randomly, but increasily further from the original course
                Quaternion randRot = Quaternion.Euler(new Vector3(Random.value*2.0f-1.0f, Random.value*2.0f-1.0f, Random.value*2.0f-1.0f) * angle);
                direction = randRot * transform.forward;
                angle += 10.0f;

                // I have tried everything, let's just faceplant
                if(angle >= 180)
                    return -transform.forward;
            }
        }

        return direction;
    }
#endregion

    // In Editor Visualize Steering Behaviours
    private void OnDrawGizmosSelected()
    {
        // Percieved Volume
        Vector3 pos = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, maxVisionDistance);

        // Known Neighbours
        foreach (BOIDUnit unit in closeUnits)
        {
            Vector3 otherPos = unit.transform.position;
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(pos, otherPos);
        }

        // Current direction
        if (rigidB != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + rigidB.velocity);
        }

        // Spatial Hash Grid
        if(mySwarm != null)
        {
            Vector3 cell = mySwarm.posToGridCell(pos);
            Vector3 cellPos = cell * maxVisionDistance;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, cellPos);
        }
    }
}