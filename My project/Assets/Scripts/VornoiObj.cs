/*
filename: VornoiObj.cs
desc: area that the player can make a Vornoi diagram on. Has a "Vornoi" tag
author: Sam Ford
date: 9/9/22
due: 11/22
https://github.com/Zalgo2462/VoronoiLib credit to Zalgo4626 while I repair my
Fortune algo implementation 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FortuneAlgo; //for MinHeap, RBT, and algo itself.
using System;
using System.Text;
using VoronoiLib.Structures;
using VoronoiLib;
using Unity.VisualScripting;
using UnityEditor;

// https://docs.unity3d.com/ScriptReference/LineRenderer.html
public class VornoiObj : MonoBehaviour
{
    private List<Vector3> seeds; //list of Vornoi seeds

    // imposing this limit as a safeguard
    // since Unity cannot handle infinite game objects
    public static  int seedMax = 300;
    private bool isEnabled;
    private bool isRealTime;
    private Material crackShader;
    private Material seedShader;

    private Vector2 upperRight;
    private Vector2 lowerLeft;
    private float surfaceY;

    [Range(.01f, .1f)]
    public float seedYOffset;

    private Color voronoiColor;
    private List<LineRenderer> vorLines;
    private List<LineRenderer> vorSites;

    // optimal crack width is .01
    // optimal intermediate is .15f
    [Range(.01f, .15f)]
    public float EdgeWidth;

    [Range(.01f, 1f)]
    public float SeedWidth;

    [Range(6, 60)] 
    public int lineCount;

    public LinkedList<VEdge> VoronoiEdges { get; private set; }
    public bool IsEnabled { get => isEnabled; private set => isEnabled = value; }

    // Start is called before the first frame update
    void Start()
    {
        seeds = new List<Vector3>(seedMax);
        isEnabled = true;
        isRealTime = false;

        Collider selfVolume = this.GetComponent<Collider>();
        float maxZ = selfVolume.bounds.max.z;
        float maxX = selfVolume.bounds.max.x;
        float minZ = selfVolume.bounds.min.z;
        float minX = selfVolume.bounds.min.x;
        this.surfaceY = selfVolume.bounds.max.y;
        upperRight = new(maxX, maxZ);
        lowerLeft = new(minX, minZ);
        voronoiColor = this.GetComponent<Renderer>().material.color;
        vorLines = new List<LineRenderer>();
        vorSites = new List<LineRenderer>();
        crackShader = Resources.Load("textures/Crack-Material", typeof(Material)) as Material;
        seedShader = Resources.Load("textures/Seed-Material", typeof(Material)) as Material;

        VoronoiEdges = null!;
    }

    // draw a new seed by making a ring with LineRenderer
    // https://stackoverflow.com/questions/13708395/how-can-i-draw-a-circle-in-unity3d
    // try bottom answer
    private void addSeedCircle(Vector3 seed, float r = .5f)
    {
        //float groundY = this.GetComponent<Renderer>().bounds.max.y;
        //Vector3 groundVec = new(seed.x, seed.y, seed.z);
        int seedNum = this.vorSites.Count;
        string seedName = $"VorSeed-{seedNum}";
        GameObject child = new GameObject(seedName);
        SeedDraw seedScript = child.AddComponent<SeedDraw>();
        seedScript.enabled = true;
        seedScript.StartSeed(this.lineCount, this.SeedWidth);
        seedScript.transform.position = seed;
        child.transform.SetParent(this.transform);
        LineRenderer lineRenderer = child.GetComponent<LineRenderer>();

        this.vorSites.Add(lineRenderer);
        seedScript.setSeedShader(this.seedShader);
        seedScript.DrawSiteCircle(this.seedYOffset);
    }

    // helper method to plant seeds
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

    // bool for checking if total seed capacity met
    public bool seedsFull()
    {
        return this.seeds.Count >= seedMax;
    }

    /*Method used to add Vornoi seeds if they aren't already in the list*/
    public bool addSeed(Vector3 seedPos, bool debug = false)
    {
        bool added = false;
        if (this.isEnabled && !seedsFull())
        {
            if (seeds == null)
            {
                seeds = new List<Vector3>();
            }

            if (!seeds.Contains(seedPos))
            {
                seeds.Add(seedPos);
                this.addSeedCircle(seedPos);
                added = true;
            }
            if (debug)
                printSeeds();
        }
        return added;
    }

    // determine if this can have seeds planted on it or not
    private void setisEnabled(bool setting)
    {
        this.isEnabled = setting;
    }
    

    // quickly see if we can construct a Voronoi Diagram
    // when the player wishes to do so
    public bool canConstructVD()
    {
        return this.seeds.Count >= 2 && this.isEnabled;
    }

    // make a Voronoi Diagram from the recorded seeds, then disabled input.
    public bool makeVoronoiDiagram()
    {
        if(this.seeds.Count < 2)
        {
            Console.WriteLine("Must have more than one seed planted to construct VD");
            return false;
        }

        Debug.Log("Constructing VD!!!");
        var points = new List<FortuneSite>();
        foreach( Vector3 seed in seeds)
        {
            FortuneSite seedSite = new(seed.x, seed.z);
            points.Add(seedSite);
        }
        VoronoiEdges = FortunesAlgorithm.Run(points, lowerLeft.x, lowerLeft.y, upperRight.x, upperRight.y);
        if (VoronoiEdges == null || VoronoiEdges.Count == 0)
        {
            Debug.Log("Voronoi Diagram creation failed");
            return false;
        }

        setisEnabled(false);
        Debug.Log("Drawing VD!");
        //https://github.com/Zalgo2462/VoronoiLib credit to Zalgo4626 while I repair my implementation
        drawVoronoiDiagram(); 
        return true;
    }

    // draws the Voronoi Diagram using lineRenderers
    public void drawVoronoiDiagram()
    {
        Debug.Log("Attempt to draw Voronoi Diagram from sites: ");
        printSeeds();
        Renderer renderer = GetComponent<Renderer>();

        if (VoronoiEdges == null || VoronoiEdges.Count == 0)
            return;

        vorLines = new(VoronoiEdges.Count - 1);
        int lineNum = 1;
        float lineY = this.surfaceY + this.seedYOffset;
        foreach ( VEdge edge in VoronoiEdges)
        {
            float startX = (float) edge.Start.X;
            float startZ = (float) edge.Start.Y;
            float endX = (float)edge.End.X;
            float endZ = (float)edge.End.Y;
            Vector3 start = new(startX, lineY, startZ);
            Vector3 end = new(endX, lineY, endZ);
            string edgeStr = $"VorEdge-{lineNum}";
            GameObject child = new GameObject(edgeStr);
            child.transform.SetParent(this.transform);
            LineRenderer newLine = child.AddComponent<LineRenderer>();
            newLine.SetPosition(0, start);
            newLine.SetPosition(1, end);

            if (crackShader != null)
            {
                Debug.Log("Assigning shader to voronoi line");
                newLine.material = crackShader;
            }
            newLine.startWidth = this.EdgeWidth ; newLine.endWidth = this.EdgeWidth;
            newLine.startColor = Color.black; newLine.endColor = Color.black;
            vorLines.Add(newLine);
            lineNum++;
        }

        return;
    }

    // set visibility of the Voronoi Edges
    private void showVoronoiEdges(bool isVisible = true)
    {
        foreach (LineRenderer line in vorLines)
            line.enabled = isVisible;

        return;
    }

    // set visibility of the Voronoi seeds/sites
    private void showVoronoiSeeds(bool isVisible = true)
    {
        foreach (LineRenderer site in vorSites)
            site.enabled = isVisible;

        return;
    }

    // set visibility of whole diagram
    public void toggleVoronoi()
    {
        showVoronoiSeeds();
        showVoronoiEdges();
    }

    // destroy the lines we drew and release them into memory
    // clear out the Voronoi Edges as well.
    public void freeVoronoiSites()
    {
        foreach (LineRenderer site in vorSites)
        {
            site.enabled = false;
            site.transform.parent = null;
            GameObject.Destroy(site, .01f); // this is not recommended in general
        }

        this.vorSites.Clear();
        this.seeds.Clear();
        return;
    }

    // destroy the lines we drew and release them into memory
    // clear out the Voronoi Edges as well.
    public void freeVoronoiEdges()
    {
        foreach (LineRenderer line in vorLines)
        {
            line.enabled = false;
            line.transform.parent = null;
            GameObject.Destroy(line, .01f);
        }

        this.vorLines.Clear();
        this.VoronoiEdges.Clear();
        return;
    }

    // free all the linerenderers for edges and sites and
    // make the Voronoi Object ineractive again.
    public bool freeAllVD()
    {
        freeVoronoiEdges();
        freeVoronoiSites();
        bool successVal = this.VoronoiEdges.Count == 0 && this.seeds.Count == 0;
        setisEnabled(successVal);

        return successVal;
    }

}
//Camera cam = Camera.main;
//Vector3 min = GetComponent<Renderer>().bounds.min;
//Vector3 max = GetComponent<Renderer>().bounds.max;
//Vector3 screenMin = cam.WorldToScreenPoint(min);
//Vector3 screenMax = cam.WorldToScreenPoint(max);
//int screenWidth = (int)(screenMax.x - screenMin.x);
//int screenHeight = (int)(screenMax.y - screenMin.y);
//int screenDepth = (int)(screenMax.z - screenMin.z);

//Debug.Log($"VorObj pix dims are w = {screenWidth}, h = {screenHeight}, d = {screenDepth}");

//Vector3 min = renderer.bounds.min;
//Vector3 max = renderer.bounds.max;
//float seedHeight = max.y;