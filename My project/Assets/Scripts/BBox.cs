/* filename: BBox.cs
 author: Sam Ford (stf8464)
 desc: class that represents a 2d Bounding Box
        used to contain Voronoi Diagram's edges
 due: Thursday 11/29
*/
using Microsoft.VisualBasic;
using System;
using System.Numerics;
using System.Text;

namespace FortuneAlgo
{
    public class BBox
    {
        public Vector2 lowerLeft; //minBound
        public Vector2 upperRight; //maxBound
        public static float _epsilon = 1e-3f;

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
            return (p.X >= lowerLeft.X - _epsilon) && (p.X <= upperRight.X - _epsilon)
                && (p.Y >= lowerLeft.Y - _epsilon) && (p.Y <= upperRight.Y - _epsilon);
        }

        /// <summary>
        /// gets the first intersection of a point in a given direction
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        Vector2 getFirstIntersection(Vector2 origin, Vector2 direction)
        {
            // origin must be in the box
            // Intersection intersection;
            float t = float.MaxValue;
            Vector2 intersection = default;
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

        public void setExtentsGivenDCEL(DCEL dcel)
        {
            float minX = float.MaxValue; float minY = float.MaxValue;
            float maxX = float.MinValue; float maxY = float.MinValue;
            foreach (Vertex v in dcel.Vertices)
            {
                if (v != dcel.InfiniteVertex)
                {
                    minX = Math.Min(v.Position.X, minX);
                    minY = Math.Min(v.Position.Y, minY);
                    maxX = Math.Max(v.Position.X, maxX);
                    maxY = Math.Max(v.Position.Y, maxY);
                }
            }

            this.lowerLeft = new(minX, minY);
            this.upperRight = new(maxX, maxY);
        }
    }
}
