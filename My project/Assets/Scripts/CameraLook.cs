/*
filename: CameraLook.cs
desc: script for first-person movement in "Vornoi Ice" app
author: Sam Ford
date: 9/6/22
due: 11/22
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraLook : MonoBehaviour

{
    private float mouseSens = 100f;
    public float xRot = 0;
    public Transform playerBody; //this is initialized in the editor as the "PlayerController" object

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; //locks cursor to center of screen!
    }

    //the main input loop for processing movement 
    void inputLoop()
    {
        ;
    }

    //update the camera view based on input received
    void updateView()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSens * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSens * Time.deltaTime;

        xRot -= mouseY; //ensure proper rotation
        xRot = Mathf.Clamp(xRot, -90f, 90f); //ensure we can't break our neck to look around

        transform.localRotation = Quaternion.Euler( xRot, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
