/*
filename: VornoiObj.cs
desc: area that the player can make a Vornoi diagram on. Has a "Vornoi" tag
author: Sam Ford
date: 9/9/22
due: 11/22
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FortuneAlgo; //for MinHeap, RBT, and algo itself.
using System;
using System.Text;

public class VornoiObj : MonoBehaviour
{
    private List<Vector3> seeds; //list of Vornoi seeds
    private bool isEnabled;
    private bool isRealTime; 
    private List<Color> seedColors; //if I want to color the patches

    // Start is called before the first frame update
    void Start()
    {
        seeds = null;
        isEnabled = true;
        isRealTime = false;
        seedColors = new List<Color>();
    }

    public void printSeeds() 
    {
        if(seeds == null || seeds.Count == 0)
        {
            Debug.Log("No seeds.");
            return;
        }

        StringBuilder seedsStr = new StringBuilder();
        seedsStr.Append("Seeds: ");
        foreach (Vector3 seed in this.seeds)
            seedsStr.Append($"{seed} ");

        Debug.Log(seedsStr.ToString());

    }

    /*Method used to add Vornoi seeds if they aren't already in the list*/
    public void addSeed(Vector3 seedPos, bool debug = true)
    {
        if (seeds == null)
        {
            seeds = new List<Vector3>();
            seeds.Add(seedPos);
        }
        else if (!seeds.Contains(seedPos))
            seeds.Add(seedPos);

        if(debug)
            printSeeds();

        return;
    }

    void setisEnabled(bool setting)
    {
        this.isEnabled = setting;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //triggers make objects not solid, which explained why the player was falling through earlier
    //void OnCollisionEnter(Collision collision)
    //{
    //    //GameObject go = collision.gameObject;
    //    //Debug.Log("In On coll w/ " + go.name + " of tag " + go.tag);
    //    //if (collision.gameObject.CompareTag("Player"))
    //    //{
    //    //    Debug.Log("Collided with" + go.name);
    //    //    UIManager uiMgr = GetComponentInParent<UIManager>();

    //    //    if (uiMgr != null)
    //    //        uiMgr.updateText("Collided with " + this.gameObject.name);

    //    //}
    //}
    //void OnCollisionStay(Collision collision)
    //{
    //    //Debug.Log("Coll Stay");
    //}

    //void OnCollisionExit(Collision collision)
    //{
    //    //Debug.Log("Coll Exit");
    //}
}
