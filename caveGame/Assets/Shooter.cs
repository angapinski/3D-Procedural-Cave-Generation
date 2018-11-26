using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Shooter : MonoBehaviour {

	public GameObject Bullet;
    private Camera myCamera;
    private Component cameraScript;
    public Transform bulletSpawn;

    const float MinTimeBetweenShots = 2.0f;
    float TimeSinceLastShot = MinTimeBetweenShots;
    public float shield = 0f;
    public float heat = 0f;
    public bool inGame = false;


    // Use this for initialization
    void Start()
    {
        myCamera = this.GetComponentInChildren<Camera>();
    }
	
	// Update is called once per frame
	void FixedUpdate () {

        


        TimeSinceLastShot += Time.deltaTime;

        if(Input.GetMouseButtonDown(0))
        {
            TimeSinceLastShot = 0;
            //GameObject bullet = Instantiate<GameObject>(Bullet, transform.position + transform.up / 2 + transform.right / 2 + transform.forward * 2, Quaternion.identity);
            //bullet.GetComponent<Rigidbody>().velocity = myCamera.transform.forward * 50;
            //Fire();
            //Debug.Log(Camera.main.transform.forward);
        }
		
	}

    void Fire()
    {
        // Create the Bullet from the Bullet Prefab
        Vector3 playerPos = myCamera.transform.position;
        Vector3 playerDirection = myCamera.transform.forward;
        Quaternion playerRotation = myCamera.transform.rotation;
        float spawnDistance = 0.6f;

        Vector3 spawnPos = playerPos + playerDirection * spawnDistance;
        //cameraPos = cameraPos + transform.forward * 4;
        GameObject bullet = (GameObject)Instantiate(Bullet, spawnPos, myCamera.transform.rotation);

        // Add velocity to the bullet
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 70;

        // Destroy the bullet after 2 seconds
       // Destroy(bullet, 2.0f);
    }

    internal void Boot()
    {
        inGame = true;
        heat = 0;
        shield = 1;
    }
}
