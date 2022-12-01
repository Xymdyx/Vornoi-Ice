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

    // control key strings
    private const string plantKey = "f";
    private const string constructKey = "e";
    private const string clearKey = "c";

    // jump message
    private const string jumpMsg = "Oh, you're airborne! Have fun ";

    // plant messages
    private const string plantMsg = "Press F to plant seed on ";
    private const string stopPlantMsg = " Stop to plant ";
    private const string maxSeedsMsg = "Planted max seeds. ";
    // surface messages
    private const string surfaceNoticeMsg = "On Voronoi Surface: ";

    // construct message
    private const string constructMsg = "Press E to Construct a Voronoi Diagram ";

    // clear message
    private const string clearMsg = "Press and release C to clear current Voronoi Diagram ";

    // reset messages
    private const string resetSuccessMsg = "Rink reset successfully. Go plant again with F ";
    private const string resetFailMsg = "Rink reset failed. Unable to construct another VD ";
    private const string continueMsg = "Feel free to make another or exit the app ";

    // suggestion message
    private const string lookMsg = "Gander at it for a bit ";

    // future update message
    private const string futureUpdateMsg = "Overhead Camera will happen later on ";



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
        if( vContact != null && vContact.IsEnabled && isGrounded && Input.GetKeyDown(plantKey) )
        {
            //get point
            Vector3 plantPt = this.transform.position;
            Collider vBoundingVol = vContact.GetComponent<Collider>();
            plantPt.y = vBoundingVol.bounds.max.y;
            bool added = vContact.addSeed(plantPt);

            if (added)
                Debug.Log($"Planted seed: {plantPt}");

        }

        return;
    }

    // notify the player that they can construct the VD when they want to.
    void constructPrompt()
    {
        if (this.vContact != null && this.vContact.canConstructVD() &&
            Input.GetKeyDown(constructKey))
        {
            vContact.makeVoronoiDiagram();
            uiMgr.updateText(lookMsg);
            uiMgr.updateUpperText(futureUpdateMsg);
        }
    }

    // notify the player when they can clear the current diagram and make another
    void clearPrompt()
    {
        if(this.vContact != null && !this.vContact.IsEnabled &&
            Input.GetKeyUp(clearKey))
        {
            bool successReset = vContact.freeAllVD();
            if (successReset)
            {
                uiMgr.updateUpperText(resetSuccessMsg);
                uiMgr.updateText(continueMsg);
            }
            else
            {
                uiMgr.updateUpperText(resetFailMsg);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDist, groundMask);
        bool areSeedsFull = false;

        if (vContact)
            areSeedsFull = vContact.seedsFull();

        if(areSeedsFull)
            uiMgr.updateText(maxSeedsMsg + $"{VornoiObj.seedMax} max achieved!");

        //forces us down onto ground
        if (isGrounded && velo.y < 0)
            velo.y = downVal; 

        if(!isGrounded)
            uiMgr.updateText(jumpMsg);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        //move along xz
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * spd * Time.deltaTime);

        // if the player is moving on the ground
        if (move.magnitude > 0 && isGrounded && !areSeedsFull)
        {
            uiMgr.updateText("");
            if (vContact && !vContact.canConstructVD())
                uiMgr.updateText(surfaceNoticeMsg + vContact.name + stopPlantMsg);
        }

        // default jump is "Space"
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velo.y = Mathf.Sqrt(jumpHgt * downVal * grav); // jh= sqrt( height * -2f * g)
        }

        // allow player to plant when still on Voronoi Surface
        if(isGrounded && move.magnitude == 0
            && vContact && vContact.IsEnabled && !areSeedsFull)
        {
            uiMgr.updateText(plantMsg + vContact.name);
            plantSeed();
        }

        if (this.vContact != null && this.vContact.canConstructVD())
        {
            uiMgr.updateUpperText(constructMsg);
            constructPrompt();
        }

        if(this.vContact != null && !this.vContact.IsEnabled)
        {
            uiMgr.updateText(clearMsg);
            clearPrompt();
        }

        //gravity
        velo.y += grav * Time.deltaTime;
        controller.Move(velo * Time.deltaTime);

        return;
    }

    /*
     * Trigger message specific to CharacterController objects.
     CharacterControllers simplify collisions to not need rigid bodies,
    so other stuff isn't needed
    */
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
