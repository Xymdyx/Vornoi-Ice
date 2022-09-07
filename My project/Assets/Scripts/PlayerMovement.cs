/*
filename: PlayerMovement.cs
desc: first-person movement in "Vornoi Ice" app
author: Sam Ford
date: 9/6/22
due: 11/22
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//https://www.youtube.com/watch?v=_QajrabyTJc 
public class PlayerMovement : MonoBehaviour
{
    // Start is called before the first frame update

    public CharacterController controller;
    public float spd = 12;

    public float grav = -9.81f;
    Vector3 velo;

    public Transform groundCheck;
    public float groundDist = 0.4f;
    public LayerMask groundMask; //used in Editor to check if a collided obj is in the "GroundCheck" layer
    private bool isGrounded;
    private float downVal = -2f;
    public float jumpHgt = 3f;

    void Start()
    {
        // empty for now;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDist, groundMask);

        //forces us down onto ground
        if (isGrounded && velo.y < 0)
            velo.y = downVal; 

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        //move along xz
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move( move * spd * Time.deltaTime );

        if (Input.GetButtonDown("Jump") && isGrounded) // default jump is "Space"
            velo.y = Mathf.Sqrt(jumpHgt * downVal * grav); // sqrt( height * -2f * g)

        //gravity
        velo.y += grav * Time.deltaTime;
        controller.Move(velo * Time.deltaTime);

        return;
    }
}
