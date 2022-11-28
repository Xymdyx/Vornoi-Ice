/*
filename: PlayerMovement.cs
desc: first-person movement in "Vornoi Ice" app
author: Sam Ford
date: 9/6/22
due: 11/22
 */

//https://docs.unity3d.com/Manual/CollidersOverview.html
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

    //needed for point sending
    private bool onVornoi;
    private VornoiObj vContact;
    UIManager uiMgr;


    void Start()
    {
        controller.detectCollisions = true;
        onVornoi = false;
        vContact = null;
        uiMgr = GameObject.FindObjectOfType<UIManager>();
    }

    /*
     * Sends player planted seed to Voronoi Surface for processing
     */
    void plantSeed()
    {
        if( vContact != null && isGrounded && Input.GetKeyDown("f") )
        {
            //get point
            Vector3 plantPt = this.transform.position;
            Collider vBoundingVol = vContact.GetComponent<Collider>();
           
            plantPt.y = vBoundingVol.bounds.max.y;
            vContact.addSeed(plantPt);
            Debug.Log($"Planted seed: {plantPt}");
        }

        return;
    }

    void constructPrompt()
    {
        if (this.vContact != null && this.vContact.canConstructVD() &&
            Input.GetKeyDown("e"))
        {
            vContact.makeVoronoiDiagram();
        }
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
        controller.Move(move * spd * Time.deltaTime);

        // if the player is moving
        if (move.magnitude > 0)
        {
            uiMgr.updateText("");
            if (vContact && !vContact.canConstructVD())
                uiMgr.updateText($"On Voronoi Surface: {vContact.name}. Stop to plant");
        }

        if (Input.GetButtonDown("Jump") && isGrounded) // default jump is "Space".. send position to current ice square to leave crack
            velo.y = Mathf.Sqrt(jumpHgt * downVal * grav); // jh= sqrt( height * -2f * g)

        // allow player to plant when still on Voronoi Surface
        if(isGrounded && move.magnitude == 0 && vContact)
        {
            uiMgr.updateText($"Press F to plant seed on {vContact.name}");
            plantSeed();
        }

        if (this.vContact != null && this.vContact.canConstructVD())
        {
            uiMgr.updateText("Press E to Construct a Voronoi Diagram");
            constructPrompt();
        }
        //gravity
        velo.y += grav * Time.deltaTime;
        controller.Move(velo * Time.deltaTime);


        return;
    }

    /*Trigger message specific to CharacterController objects.
     CharacterControllers simplify collisions to not need rigid bodies,
    so other stuff isn't needed*/
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        GameObject go = hit.gameObject;

        if (go.CompareTag("Vornoi") && !vContact) //first enter Vornoi layer
        {
            onVornoi = true;
            Debug.Log("GC hit Vornoi object");
            Debug.Log("Collided with" + go.name);
            //UIManager uiMgr = GameObject.FindObjectOfType<UIManager>();
            if (uiMgr != null && !vContact)
                uiMgr.updateText("Collided with " + go.name);

            vContact = go.GetComponent<VornoiObj>();

            return;
        }

        else if (!(go.CompareTag("Vornoi")) && vContact)
        {
            onVornoi = false;
            vContact = null;
        }

        //to simulate onCollisonEnter and onCollisionExit, use groundCheck
        return;
    }
}
