/* filename: BBox.cs
 author: Sam Ford (stf8464)
 desc: class that represents a 2d Bounding Box
        used to contain Voronoi Diagram's edges
 due: Thursday 11/29
*/
using System;
using System.Numerics;
using System.Text;

namespace FortuneAlgo
{
    public class BBox
    {
        public Vector2 minBound; //lower-left corner
        public Vector2 maxBound; //upper-right corner

        BBox(Vector2 minBound, Vector2 maxBound)
        {
            this.minBound = minBound;
            this.maxBound = maxBound;
        }
    }
}
