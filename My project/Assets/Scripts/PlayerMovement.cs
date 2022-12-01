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


    //used in Editor to check if a collided obj is in the "GroundCheck" layer
    public LayerMask groundMask;
    public Transform groundCheck;
    public float groundDist = 0.4f;
    private bool isGrounded;
    private float downVal = -2f;
    public float jumpHgt = 3f;

    //needed for point sending
    private bool onVornoi;
    private VornoiObj vContact;
    UIManager uiMgr;

    // needed for camera switching
    private Camera fpCam;
    private Camera overHeadCam;
    private bool isFPActive = true;
    private MeshRenderer playerModel;

    // control key strings
    private const string plantKey = "f";
    private const string constructKey = "e";
    private const string clearKey = "c";
    private const string switchCamKey = "q";

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

    // switch camera message
    private const string switchOHMsg = "Press and release Q to switch to overhead view";
    private const string switchFPMsg = "Press and release Q to return to FP View";

    // future update message
    private const string futureUpdateMsg = "Overhead Camera will happen later on ";


    void Start()
    {
        controller.detectCollisions = true;
        onVornoi = false;
        vContact = null;
        uiMgr = GameObject.FindObjectOfType<UIManager>();
        overHeadCam = GameObject.Find("OverHead-Cam").GetComponent<Camera>();
        fpCam = this.GetComponentInChildren<Camera>();
        this.playerModel = this.GetComponentInChildren<MeshRenderer>();

        if (fpCam == null)
            Debug.Log("FP Cam doesn't exist yet");
        if (overHeadCam == null)
            Debug.Log("OverHead doesn't exist yet");
        if (playerModel == null)
            Debug.Log("Player model doesn't exist yet");
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
            Input.GetKeyUp(clearKey) && this.isFPActive)
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

    // method to switch to secondary overhead view
    void switchToOHCam()
    {
        if (isFPActive)
        {
            this.fpCam.enabled = false;
            this.overHeadCam.enabled = true;
            // make player model invisible
            if (playerModel != null)
            {
                playerModel.enabled = false;
            }

            this.isFPActive = false;
        }
    }

    // method to allow user to return to first-person view
    void switchToFPCam()
    {
        if (!isFPActive)
        {
            this.overHeadCam.enabled = false;
            this.fpCam.enabled = true;

            // make player model visible
            if (playerModel != null)
            {
                playerModel.enabled = true;
            }

            this.isFPActive = true;
        }
    }

    // notify the player that they can switch their POV
    // only once a Voronoi Diagram has been constructed
    void switchViewPrompt()
    {
        if(this.vContact != null && !this.vContact.IsEnabled
            && Input.GetKeyUp(switchCamKey))
        {
            if (isFPActive)
                switchToOHCam();
            else
                switchToFPCam();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // we only want to do this stuff if we're in first person
        if (isFPActive) 
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDist, groundMask);
            bool areSeedsFull = false;

            if (vContact)
                areSeedsFull = vContact.seedsFull();

            if (areSeedsFull)
                uiMgr.updateText(maxSeedsMsg + $"{VornoiObj.seedMax} max achieved!");

            //forces us down onto ground
            if (isGrounded && velo.y < 0)
                velo.y = downVal;

            if (!isGrounded)
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
            if (isGrounded && move.magnitude == 0
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

        }

        // we always want to prompt for these after a VD is made
        if(this.vContact != null && !this.vContact.IsEnabled)
        {
            if (isFPActive)
            {
                uiMgr.updateUpperText(switchOHMsg);
                switchViewPrompt();
                uiMgr.updateText(clearMsg);
                clearPrompt();
            }
            else
            {
                uiMgr.updateUpperText(switchFPMsg);
                uiMgr.updateText("");
                switchViewPrompt();
            }
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
