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
        public Vector2 lowerLeft; //minBound
        public Vector2 upperRight; //maxBound

        public BBox(Vector2 lowerLeft, Vector2 upperRight)
        {
            this.lowerLeft = lowerLeft;
            this.upperRight = upperRight;
        }

        // utility for computing upperLeft corner
        public Vector2 getUpperLeft()
        {
            return new Vector2(this.lowerLeft.X, this.upperRight.Y);
        }

        // utility for getting upperRight corner
        public Vector2 getLowerRight()
        {
            return new Vector2(this.upperRight.X, this.lowerLeft.Y);
        }
    }
}
