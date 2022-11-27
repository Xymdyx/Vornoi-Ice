/* filename: BBox.cs
 author: Sam Ford (stf8464)
 desc: class that represents a 2d Bounding Box
        used to contain Voronoi Diagram's edges
 due: Thursday 11/29

https://github.com/pvigier/FortuneAlgorithm/blob/master/src/Box.cpp... credit
*/
using System.Collections.Generic; 
using System;
using System.Numerics;
using System.Text;

namespace FortuneAlgo
{
    /// <summary>
    /// defines box borders in a CLOCKWISE MANNER
    /// </summary>
    public enum boxBorders
    {
        NONE = -1,
        TOP = 0,
        RIGHT = 1,
        BOTTOM = 2,
        LEFT = 3
    }

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
        /// Given a DCEL, set this box's bounds
        /// to contain all vertices in the DCEL
        /// </summary>
        /// <param name="dcel"></param>
        /// <param name="sites"></param>
        public void setExtentsGivenDCEL(DCEL dcel, List<Vector2> sites, float minYCoord = float.MaxValue)
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

            foreach (Vector2 site in sites)
            {
                minX = Math.Min(site.X, minX);
                minY = Math.Min(site.Y, minY);
                maxX = Math.Max(site.X, maxX);
                maxY = Math.Max(site.Y, maxY);
            }

            if (minYCoord != float.MaxValue)
                minY = Math.Min(minYCoord, minY);

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

        /// <summary>
        /// given the origin and target of a ray
        /// find point(s) where this ray intersects the box
        /// there are 2 max.
        /// </summary>
        /// <param name="origin"> the start pt of the ray</param>
        /// <param name="target"> the target pt of the ray</param>
        /// <returns> a Vector with at most 2 intersection points </returns>
        public Vector2[] getIntersections(Vector2 origin, Vector2 target)
        {
            Vector2[] ints = new Vector2[2];
            float[] dists = new float[2];
            int idx = 0;

            Vector2 direction = Vector2.Normalize(target - origin);
            float left = this.lowerLeft.X;
            float bottom = this.lowerLeft.Y;
            float right = this.upperRight.X;
            float top = this.upperRight.Y;

            // left
            if (origin.X < (left - _tolerance) || target.X < (left - _tolerance))
            {
                dists[idx] = (left - origin.X) / direction.X;
                if (dists[idx] > _tolerance && dists[idx] < (1.0 - _tolerance))
                {
                    ints[idx] = origin + Vector2.Multiply(direction, dists[idx]);
                    if (ints[idx].Y >= (bottom - _tolerance) && ints[idx].Y <= (top + _tolerance))
                        ++idx;
                }
            }
            // right
            if (origin.X > (right + _tolerance) || target.X > (right + _tolerance))
            {
                dists[idx] = (right - origin.X) / direction.X;
                if (dists[idx] > _tolerance && dists[idx] < (1.0 - _tolerance))
                {
                    ints[idx] = origin + Vector2.Multiply(direction, dists[idx]);
                    if (ints[idx].Y >= (bottom - _tolerance) && ints[idx].Y <= (top + _tolerance))
                        ++idx;
                }
            }
            // bottom
            if (origin.Y < (bottom - _tolerance) || target.Y < (bottom - _tolerance))
            {
                dists[idx] = (bottom - origin.Y) / direction.Y;
                if ( idx < 2 && dists[idx] > _tolerance && dists[idx] < (1.0 - _tolerance))
                {
                    ints[idx] = origin + Vector2.Multiply(direction, dists[idx]);
                    if (ints[idx].X >= (left - _tolerance) && ints[idx].X <= (right + _tolerance))
                        ++idx;
                }
            }
            // top
            if (origin.Y > (top + _tolerance) || target.Y > (top + _tolerance))
            {
                dists[idx] = (top - origin.Y) / direction.Y;
                if ( idx < 2 && dists[idx] > _tolerance && dists[idx] < (1.0 - _tolerance))
                {
                    ints[idx] = origin + Vector2.Multiply(direction, dists[idx]);
                    if (ints[idx].X >= (left - _tolerance) && ints[idx].X <= (right + _tolerance))
                        ++idx;
                }
            }
            //sort nearest to furthest
            if(idx == 2 && dists[0] > dists[1])
                (ints[0], ints[1]) = (ints[1], ints[0]);

            return ints;
        }

        /// quickly tell if a query point is a corner of this box
        public bool isBoxCorner(Vector2 query)
        {
            bool isUsualExtent = (query == this.lowerLeft) || (query == this.upperRight);
            bool isOtherExtent = (query == getLowerRight()) || (query == getUpperLeft());
            return isUsualExtent || isOtherExtent;
        }

        /// tell if query point is on the box's boundaries
        public bool isOnBoxBorders(Vector2 query)
        {
            bool inYRange = (this.lowerLeft.Y <= query.Y && query.Y <= this.upperRight.Y);
            bool inXRange = (this.lowerLeft.X <= query.X && query.X <= this.upperRight.X);
            if (query.X == this.lowerLeft.X && inYRange)
                return true;
            else if (query.X == this.upperRight.X && inYRange)
                return true;
            else if (query.Y == lowerLeft.Y && inXRange)
                return true;
            else if (query.Y == upperRight.Y && inXRange)
                return true;

            return false;
        }
        /// get enum value for a query pt
        private boxBorders grabBorderValueOfPoint(Vector2 query)
        {
            if (!isOnBoxBorders(query))
                return boxBorders.NONE;
            else if (query.X == this.lowerLeft.X)
                return boxBorders.LEFT;
            else if (query.X == this.upperRight.X)
                return boxBorders.RIGHT;
            else if (query.Y == lowerLeft.Y)
                return boxBorders.BOTTOM;
            else if (query.Y == upperRight.Y)
                return boxBorders.TOP;

            return boxBorders.NONE;
        }

        /// <summary>
        /// given 2 points on the bounding box,
        /// find 2 corners between them along a ccw walk... make this clockwise... 11/26
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<Vector2> getCornersBetween2BorderPts(Vector2 start, Vector2 end)
        {
            boxBorders startVal = grabBorderValueOfPoint(start);
            boxBorders endVal = grabBorderValueOfPoint(end);
            List<Vector2> corners = new();
            switch (startVal)
            {
                case boxBorders.TOP:
                    if (endVal == boxBorders.RIGHT)
                        corners.Add(this.upperRight);
                    else if(endVal == boxBorders.BOTTOM)
                    {
                        corners.Add(this.upperRight);
                        corners.Add(this.getLowerRight());
                    }
                    else if(endVal == boxBorders.LEFT)
                    {
                        corners.Add(this.upperRight);
                        corners.Add(this.getLowerRight());
                        corners.Add(this.lowerLeft);
                    }
                    break;

                case boxBorders.RIGHT:
                    if (endVal == boxBorders.BOTTOM)
                        corners.Add(this.getLowerRight());
                    else if (endVal == boxBorders.LEFT)
                    {
                        corners.Add(this.getLowerRight());
                        corners.Add(this.lowerLeft);
                    }
                    else if (endVal == boxBorders.TOP)
                    {
                        corners.Add(this.getLowerRight());
                        corners.Add(this.lowerLeft);
                        corners.Add(this.getUpperLeft());
                    }
                    break;

                case boxBorders.BOTTOM:
                    if (endVal == boxBorders.LEFT)
                        corners.Add(this.lowerLeft);
                    else if (endVal == boxBorders.TOP)
                    {
                        corners.Add(this.lowerLeft);
                        corners.Add(this.getUpperLeft());
                    }
                    else if (endVal == boxBorders.RIGHT)
                    {
                        corners.Add(this.lowerLeft);
                        corners.Add(this.getUpperLeft());
                        corners.Add(this.upperRight);
                    }
                    break;

                case boxBorders.LEFT:
                    if (endVal == boxBorders.TOP)
                        corners.Add(this.getUpperLeft());
                    else if (endVal == boxBorders.RIGHT)
                    {
                        corners.Add(this.getUpperLeft());
                        corners.Add(this.upperRight);
                    }
                    else if (endVal == boxBorders.BOTTOM)
                    {
                        corners.Add(this.getUpperLeft());
                        corners.Add(this.upperRight);
                        corners.Add(this.getLowerRight());
                    }
                    break;
            }

            // if they are corners, remove them
            if (corners.Count > 0)
            {
                corners.Remove(start);
                corners.Remove(end);
            }

            return corners;
        }
    }
}
