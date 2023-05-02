using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BOIDUnit : MonoBehaviour
{
    public float maxDistance = 5.0f;
    public float turnSpeed = 1.0f;
    public float moveSpeed = 5.0f;
    public static Transform target;

    public float[] weights = 
        {
            1.4f,   // alignmernt
            1.0f,   // cohesion
            0.75f,   // seperation
            1.125f,    // steering
            10.0f    // avoidance
        };

    BOIDSwarm swarm;
    List<BOIDUnit> closeUnits = new List<BOIDUnit>();

    Rigidbody rigidB;
    private void Start()
    {
        rigidB = GetComponent<Rigidbody>();

        swarm = BOIDSwarm.getSingleton();
        swarm.addUnit(this);

        if (target == null)
        {
            /*
            var move = GameObject.FindObjectOfType<PlayerMovement>();
            if (move != null)
            {
                target = move.gameObject.transform;
            }
            */

            target = GameObject.FindObjectOfType<Camera>().transform;
        }
    }   

    public void addMe(Vector3 pos)
    {
        transform.position = pos;
        swarm.addUnit(this);
        swarm.maxDistance = maxDistance;
    }
    public void removeMe()
    {
        swarm.removeUnit(this);
    }
    private void OnDestroy()
    {
        removeMe();
    }
  
    private void FixedUpdate()
    {
        if (swarm == null)
            swarm = BOIDSwarm.getSingleton();

        closeUnits = swarm.getCloseUnits(transform.position);
        closeUnits.Remove(this);

        Vector3 dir = computeBoidDirection();
        Vector3 forward = transform.forward;
        Vector3 smoothed = Vector3.Slerp(forward, dir, turnSpeed * Time.deltaTime * Time.timeScale);
        
        //Vector3 vel = Vector3.ProjectOnPlane(smoothed, Vector3.up).normalized * moveSpeed;
        Vector3 vel = smoothed.normalized * moveSpeed;
        rigidB.velocity = vel;

        transform.LookAt(transform.position + vel, Vector3.up);
    }

    float influence(Vector3 otherPos)
    {
        float distance = Vector3.Distance(transform.position, otherPos);
        float linear = Mathf.Clamp01(distance / maxDistance);
        return linear * linear; // quadratic
    }

    Vector3 computeBoidDirection()
    {
        Vector3 direction = Vector3.zero;

        if(closeUnits.Count != 0)
        {
            direction += computeAlignment() * weights[0];
            direction += computeCohesion()  * weights[1];
            direction += computeSeperation()* weights[2];
        }

        direction += computeSteering() * weights[3];
        direction += computeAvoidance() * weights[4];

        //Debug.DrawLine(transform.position, transform.position + computeAvoidance()* maxDistance/2.0f, Color.red, Time.fixedDeltaTime);
        
        direction.Normalize();
        return direction;
    }

    Vector3 computeSeperation()
    {
        Vector3 direction = Vector3.zero;

        foreach (BOIDUnit unit in closeUnits)
        {
            float multiplier = influence(unit.transform.position);
            direction += (transform.position - unit.transform.position) * multiplier;
        }

        if (direction != Vector3.zero)
            direction.Normalize();
        return direction;
    }

    Vector3 computeAlignment()
    {
        Vector3 direction = Vector3.zero;

        foreach (BOIDUnit unit in closeUnits)
        {
            float multiplier = influence(unit.transform.position);
            direction += unit.transform.forward.normalized * multiplier;
        }


        if (direction != Vector3.zero)
            direction.Normalize();
        return direction;
    }

    Vector3 computeCohesion()
    {
        Vector3 center = Vector3.zero;

        foreach (BOIDUnit unit in closeUnits)
        {
            center += unit.transform.position;
        }


        if(closeUnits.Count > 0)
            return (center - transform.position).normalized;
        else
            return Vector3.zero;
    }

    Vector3 computeSteering()
    {
        Vector3 to;
        if (target == null)
            to = new Vector3();
        else
            to = target.transform.position;

        Vector3 diff = (to - transform.position);
        Vector3 dir = diff.normalized;
        
        return dir;
    }

    Vector3 computeAvoidance()
    {
        Vector3 direction = transform.forward;

        float angle = 0;
        bool clear = false;
        while(!clear)
        {
            clear = !Physics.Raycast(transform.position, direction, maxDistance / 2.0f);
            //Debug.DrawLine(transform.position, transform.position + direction * maxDistance/2.0f, Color.green, Time.fixedDeltaTime);

            if(clear)
            {
                if(angle == 0)
                    return new Vector3();
                else return direction;
            }
            else
            {
                Quaternion randRot = Quaternion.Euler(new Vector3(Random.value*2.0f-1.0f, Random.value*2.0f-1.0f, Random.value*2.0f-1.0f) * angle);
                direction = randRot * transform.forward;
                angle += 10.0f;

                if(angle >= 180)
                    return -transform.forward;
            }
        }

        return direction;
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
        rigidB.velocity = collision.GetContact(0).normal * rigidB.velocity.magnitude;
        transform.LookAt(transform.position + rigidB.velocity);
        Debug.DrawLine(transform.position, transform.position + rigidB.velocity);
    }
    */

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        //UnityEditor.Handles.color = Color.yellow;
        //UnityEditor.Handles.DrawWireDisc(pos, Vector3.up, maxDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, maxDistance);

        foreach (BOIDUnit unit in closeUnits)
        {
            Vector3 otherPos = unit.transform.position;
            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(pos, otherPos);
        }

        if (rigidB != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + rigidB.velocity);
        }

        if(swarm != null)
        {
            Vector3 cell = swarm.posToGridCell(pos);
            Vector3 cellPos = cell * maxDistance;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, cellPos);
        }
    }
}