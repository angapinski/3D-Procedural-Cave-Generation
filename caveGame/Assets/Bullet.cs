using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

    float TimeAlive = 0;
    public GameObject BulletDeath;

    // Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {

        TimeAlive += Time.deltaTime;

        if(TimeAlive > 5)
        {
            //Instantiate(BulletDeath, transform.position, Quaternion.identity);
            GameObject particles = Instantiate<GameObject>(BulletDeath, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
            Destroy(particles, 1);
        }
	}

    private void OnCollisionEnter(Collision collision)
    {
        Enemy Enemy = collision.gameObject.GetComponent<Enemy>();
        if (Enemy != null)
        {
            Debug.Log("Hit enemy");
            Enemy.health -= 1;
        }

        GameObject particles = Instantiate<GameObject>(BulletDeath, transform.position, Quaternion.identity);
        Destroy(particles, 1);
        Destroy(this.gameObject);
    }
}
