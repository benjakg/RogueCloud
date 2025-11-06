using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstKill : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
    
            Player p = collision.gameObject.GetComponent<Player>();
            p.insKill();
            Destroy(gameObject);

    
    }
}
