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

public class VornoiObj : MonoBehaviour
{
    private List<Vector3> seeds; //list of Vornoi seeds
    private bool isEnabled;
    private bool isRealTime; 
    private List<Color> seedColors; //if I want to color the patches

    // Start is called before the first frame update
    void Start()
    {
        seeds = new List<Vector3>();
        isEnabled = true;
        isRealTime = false;
        seedColors = new List<Color>();
    }


    void addSeed(Vector3 seedPos)
    {
        if (!seeds.Contains(seedPos))
            seeds.Add(seedPos);

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

}
