using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SeedDraw : MonoBehaviour
{
    // Start is called before the first frame update
    public LineRenderer lineRenderer;
    public int lineCount;
    public const float r = .5f;
    private float width;
    private Material seedShader;

    public void StartSeed(int lineCount = 30, float width = .15f)
    {
        lineRenderer = this.AddComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.Log("Site LineRenderer is null");
        }

        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        // set x rotation to 90
        this.transform.Rotate(Vector3.right, 90f);
        this.lineCount = lineCount;
        this.width = width;

    }

    public void setSeedShader(Material shader)
    {
        this.seedShader = shader;
    }

    // Update is called once per frame
    public void DrawSiteCircle()
    {
        if (lineRenderer == null)
            return;

        if (seedShader != null)
            lineRenderer.material = seedShader;

        Debug.Log($"Seed loc: {this.transform.position}");
        lineRenderer.startColor = Color.blue;
        lineRenderer.endColor = Color.blue;
        lineRenderer.startWidth = this.width;
        lineRenderer.endWidth = this.width;
        lineRenderer.positionCount = this.lineCount;

        //float thetaScale = 0.01f;  // Circle resolution
        float theta = (2f * Mathf.PI) / this.lineCount;
        float angle = 0;
        for (int i = 0; i < this.lineCount; i++)
        {
            // there's a Mathf library...11/28
            float x = (r * Mathf.Cos(angle));
            float z = (r * Mathf.Sin(angle));

            Vector3 pos = new Vector3(x, 0.01f, z) + this.transform.position;
            lineRenderer.SetPosition(i, pos);
            angle += theta;
        }
    }
}
