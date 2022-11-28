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
using VoronoiLib.Structures;
using VoronoiLib;
using Unity.VisualScripting;
using UnityEditor;

public class VornoiObj : MonoBehaviour
{
    private List<Vector3> seeds; //list of Vornoi seeds
    private bool isEnabled;
    private bool isRealTime;
    private Material crackShader;

    private List<Color> seedColors; //if I want to color the patches
    public LinkedList<VEdge> VoronoiEdges { get; private set; }
    private Vector2 upperRight;
    private Vector2 lowerLeft;
    private Color voronoiColor;
    private List<LineRenderer> vorLines;
    private const float LINEWIDTH = .25F;

    // Start is called before the first frame update
    void Start()
    {
        seeds = new List<Vector3>();
        isEnabled = true;
        isRealTime = false;
        seedColors = new List<Color>();

        Collider selfVolume = this.GetComponent<Collider>();
        float maxZ = selfVolume.bounds.max.z;
        float maxX = selfVolume.bounds.max.x;
        float minZ = selfVolume.bounds.min.z;
        float minX = selfVolume.bounds.min.x;
        upperRight = new(maxX, maxZ);
        lowerLeft = new(minX, minZ);
        voronoiColor = this.GetComponent<Renderer>().material.color;
        vorLines = new List<LineRenderer>();
        crackShader = Resources.Load("textures/Crack-Material", typeof(Material)) as Material;
        VoronoiEdges = null!;
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

        Camera cam = Camera.main;
        Vector3 min = GetComponent<Renderer>().bounds.min;
        Vector3 max = GetComponent<Renderer>().bounds.max;
        Vector3 screenMin = cam.WorldToScreenPoint(min);
        Vector3 screenMax = cam.WorldToScreenPoint(max);
        int screenWidth = (int)(screenMax.x - screenMin.x);
        int screenHeight = (int)(screenMax.y - screenMin.y);
        int screenDepth = (int)(screenMax.z - screenMin.z);

        Debug.Log($"VorObj pix dims are w = {screenWidth}, h = {screenHeight}, d = {screenDepth}");

        return;
    }

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
            Console.WriteLine("Voronoi Diagram creation failed");
            return false;
        }

        setisEnabled(false);
        Debug.Log("Drawing VD!");
        drawVoronoiDiagram();
        return true;
    }

    // draws the Voronoi Diagram using lineRenderers
    public void drawVoronoiDiagram()
    {
        Console.WriteLine("Attempt to draw Voronoi Diagram from sites: ");
        printSeeds();
        Renderer renderer = GetComponent<Renderer>();

        //int newDepth = this.upperRight.y;

        if (VoronoiEdges == null || VoronoiEdges.Count == 0)
            return;

        Camera cam = Camera.main;
        Vector3 min = renderer.bounds.min;
        Vector3 max = renderer.bounds.max;
        Vector3 screenMin = cam.WorldToScreenPoint(min);
        Vector3 screenMax = cam.WorldToScreenPoint(max);
        int screenWidth = (int) (screenMax.x - screenMin.x);
        int screenHeight = (int) (screenMax.y - screenMin.y);
        int screenDepth = (int) (screenMax.z - screenMin.z);
        float seedHeight = max.y;

        vorLines = new(VoronoiEdges.Count - 1);
        //Texture3D vorTexture = new(screenWidth, screenHeight, screenDepth, TextureFormat.ARGB32, false);
        foreach( VEdge edge in VoronoiEdges)
        {
            float startX = (float) edge.Start.X;
            float startZ = (float) edge.Start.Y;
            float endX = (float)edge.End.X;
            float endZ = (float)edge.End.Y;
            Vector3 start = new(startX, seedHeight, startZ);
            Vector3 end = new(endX, seedHeight, endZ);
            GameObject child = new GameObject();
            child.transform.SetParent(this.transform);
            LineRenderer newLine = child.AddComponent<LineRenderer>();
            newLine.SetPosition(0, start);
            newLine.SetPosition(1, end);

            if (crackShader != null)
            {
                Debug.Log("Assigning shader to voronoi line");
                newLine.material = crackShader;
            }
            newLine.startWidth = LINEWIDTH ; newLine.endWidth = LINEWIDTH;
            newLine.startColor = Color.black; newLine.endColor = Color.black;
            vorLines.Add(newLine);
        }

        return;
    }

    // destroy the lines we drew and release them into memory
    // clear out the Voronoi Edges as well.
    public bool freeVoronoiDiagram()
    {
        foreach(LineRenderer line in vorLines)
        {
            line.enabled = false;
            GameObject.Destroy(line);
        }

        this.vorLines.Clear();
        this.VoronoiEdges.Clear();
        setisEnabled(true);
        return true;
    }

    public bool showVoronoiDiagram(bool isVisible = false)
    {
        foreach (LineRenderer line in vorLines)
            line.enabled = isVisible;

        return true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
