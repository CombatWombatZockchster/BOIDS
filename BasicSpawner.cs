using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicSpawner : MonoBehaviour
{
    public GameObject prefab;
    public float range = 50.0f;
    public uint count = 200;


    // Start is called before the first frame update
    void Start()
    {
        for(uint i = 0; i < count; i++)
            spawn();
    }

    void spawn()
    {
        GameObject instance = Instantiate(prefab);
        instance.transform.SetParent(transform);
        instance.transform.position = new Vector3(Random.value*2.0f-1.0f, Random.value*2.0f-1.0f, Random.value*2.0f-1.0f) * range;
        instance.transform.rotation = Random.rotation;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
