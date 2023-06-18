/*
*  Written by Jonas H.
*
*  Spawns multiple instances of a prefab within a cube.
*
*  Very simple way of quickly getting a bunch of swarm units into the scene.
*  Feel free to discard and make your own.
*/

using UnityEngine;

public class BasicSpawner : MonoBehaviour
{
    [Tooltip("Prefab to be instantiated")]
    public GameObject prefab;

    [Tooltip("Number of instances to be created")]
    public uint count = 200;


    [Tooltip("Side length of cube defining the spawn volume")]
    public float range = 50.0f;
 
    
    [Tooltip("Do not spawn within colliders")]
    public bool avoidSceneIntersection = false;
    [Tooltip("Number of times scene intersection will be checked")]
    public uint avoidanceAttempts = 4;
    [Tooltip("Size of checked space")]
    public float avoidanceRadius = 1.0f;

    void Awake()
    {
        // Spawn multiple instances
        for(uint i = 0; i < count; i++)
            spawn();
    }

    /// <summary>
    /// Create a single instance of the selected prefab,
    /// At a random position within the spawn volume,
    /// Witha a random rotation,
    /// As a child of my gameObject,
    /// </summary>
    void spawn()
    {
        // Get random pos in spawn volume
        Vector3 spawnPos = transform.position + randPosInCube();

        // Keep trying until pos is unoccupied or I give up
        if (avoidSceneIntersection)
            for (uint i = 0; i < avoidanceAttempts && Physics.CheckSphere(spawnPos, avoidanceRadius); i++)
                spawnPos = transform.position + randPosInCube();

        // Spawn the prefab
        GameObject instance = Instantiate(prefab);
        instance.transform.SetParent(transform);
        instance.transform.position = spawnPos;
        instance.transform.rotation = Random.rotation;
    }

    /// <summary>
    /// Random Position in a Cube
    /// </summary>
    /// <returns></returns>
    private Vector3 randPosInCube()
    {
        return new Vector3
            (
                Random.value * 2.0f - 1.0f,
                Random.value * 2.0f - 1.0f, 
                Random.value * 2.0f - 1.0f
            )
            * range / 2.0f;
    }


    // In the Editor Show volume where instances can appear
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * range);
    }
}
