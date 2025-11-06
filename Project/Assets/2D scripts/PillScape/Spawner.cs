using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform RigthSpawn;
    public Transform LeftSpawn;
    public List<GameObject> PillPrefabs;
    public float timer=5;
    public float restTime;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        Spawn();
    }
    
    void Spawn()
    {
        if (restTime>= timer)
        {
            restTime = 0;
            float y = transform.position.y;
            float x = Random.Range(LeftSpawn.position.x, RigthSpawn.position.x);
            Vector2 position = new Vector2(x, y);
            Instantiate(PillPrefabs[Random.Range(0, PillPrefabs.Count)], position, Quaternion.Euler(0, 0, 0));


        }else
        {
            restTime += Time.deltaTime;
        }



    }

}
