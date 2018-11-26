using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    public float health = 1;
    public GameObject DeathParticleEffect;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(health <= 0)
        {
            Instantiate(DeathParticleEffect, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }

        
    }
}
