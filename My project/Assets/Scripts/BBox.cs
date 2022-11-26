/* filename: BBox.cs
 author: Sam Ford (stf8464)
 desc: class that represents a 2d Bounding Box
        used to contain Voronoi Diagram's edges
 due: Thursday 11/29
*/
using System.Collections.Generic; 
using System;
using System.Numerics;
using System.Text;

namespace FortuneAlgo
{
    public class BBox
    {
        public Vector2 lowerLeft; //minBound
        public Vector2 upperRight; //maxBound
        public static float _tolerance = 1e-3f;
        public static Vector2 DNE = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
        private const float _boundBoost = 10f;

        public BBox(Vector2 lowerLeft, Vector2 upperRight)
        {
            this.lowerLeft = lowerLeft;
            this.upperRight = upperRight;
        }

        public BBox()
        {
            this.lowerLeft = new(float.MaxValue, float.MaxValue);
            this.upperRight = new(float.MinValue, float.MinValue);
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

        /// <summary>
        /// check if a point p is inside this box
        /// </summary>
        /// <param name="p"></param>
        /// <returns>if p is inside this box</returns>
        public bool containsPoint(Vector2 p)
        {
            return (p.X >= lowerLeft.X - _tolerance) && (p.X <= upperRight.X - _tolerance)
                && (p.Y >= lowerLeft.Y - _tolerance) && (p.Y <= upperRight.Y - _tolerance);
        }

        /// <summary>
        /// gets the first intersection of a point in a given direction
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Vector2 getFirstIntersection(Vector2 origin, Vector2 direction)
        {
            // origin must be in the box
            // Intersection intersection;
            float t = float.MaxValue;
            Vector2 intersection = BBox.DNE;
            if (direction.X > 0.0)
            {
                t = (upperRight.X - origin.X) / direction.X;
                // intersection.side = Side::RIGHT;
                intersection = origin + Vector2.Multiply(direction, t);
            }
            else if (direction.X < 0.0)
            {
                t = (lowerLeft.X - origin.X) / direction.X;
                //intersection.side = Side::LEFT;
                intersection = origin + Vector2.Multiply(direction, t);
            }

            if (direction.Y > 0.0)
            {
                float newT = (upperRight.Y - origin.Y) / direction.Y;
                if (newT < t)
                {
                        // intersection.side = Side::TOP;
                        intersection = origin + Vector2.Multiply(direction, newT);
                }
            }

            else if (direction.Y < 0.0)
            {
                float newT = (lowerLeft.Y - origin.Y) / direction.Y;
                if (newT < t)
                {
                    // intersection.side = Side::BOTTOM;
                    intersection = origin + Vector2.Multiply(direction, newT);
                }
            }

            return intersection;
        }

        public void setExtentsGivenDCEL(DCEL dcel, List<Vector2> sites)
        {
            float minX = float.MaxValue; float minY = float.MaxValue;
            float maxX = float.MinValue; float maxY = float.MinValue;
            foreach (Vertex v in dcel.Vertices)
            {
                if (v != DCEL.INFINITY)
                {
                    minX = Math.Min(v.Position.X, minX);
                    minY = Math.Min(v.Position.Y, minY);
                    maxX = Math.Max(v.Position.X, maxX);
                    maxY = Math.Max(v.Position.Y, maxY);
                }
            }

            foreach(Vector2 site in sites)
            {
                minX = Math.Min(site.X, minX);
                minY = Math.Min(site.Y, minY);
                maxX = Math.Max(site.X, maxX);
                maxY = Math.Max(site.Y, maxY);
            }
            minX -= _boundBoost; minY -= _boundBoost;
            maxX += _boundBoost; maxY += _boundBoost;
            this.lowerLeft = new(minX, minY);
            this.upperRight = new(maxX, maxY);
        }

        /// <summary>
        /// expand the bounding box to contain a new point
        /// if necessary
        /// </summary>
        /// <param name="p"></param>
        public bool doesPointExpandBox(Vector2 p)
        {
            float minX = Math.Min(this.lowerLeft.X, p.X);
            float minY = Math.Min(this.lowerLeft.Y, p.Y);
            float maxX = Math.Max(this.upperRight.X, p.X);
            float maxY = Math.Max(this.upperRight.Y, p.Y);

            Vector2 minXY = new(minX, minY);
            Vector2 maxXY = new(maxX, maxY);

            if (minXY == this.lowerLeft && maxXY == this.upperRight)
                return false;

            this.lowerLeft = minXY;
            this.upperRight = maxXY;

            return true;
        }

        /// <summary>
        /// expand this bounding box given another bounding box we wish to map to.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool doesBoxExpandBox(BBox b)
        {
            bool doesExpand = doesPointExpandBox(b.lowerLeft);
            doesExpand = doesPointExpandBox(b.getLowerRight());
            doesExpand = doesPointExpandBox(b.getUpperLeft());
            doesExpand = doesPointExpandBox(b.upperRight);

            return doesExpand;
        }
    }
}
