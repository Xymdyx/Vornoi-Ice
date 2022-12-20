/* filename: FortuneMath.cs
 author: Sam Ford (stf8464)
 desc: utility class for doing complicated math in Fortune's Algo
*/
using FortuneAlgo;
using System;
using System.Collections;
using System.Collections.Generic; // sorted dictionary for beachline
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
namespace FortuneAlgo
{
    public static class FortuneMath
    {

        public static float _toleranceThreshold = 1e-3f;
        public const float _beachlineBoost = 10e6f;
        public const float _convergeDivisor = 10f;
        /*
        * gets Euclidean Distance of 2 sites if it's the desired distance metric 
        */
        public static float computeEuclidDist(Vector2 s1, Vector2 s2)
        {
            return Vector2.Distance(s1, s2);
        }

        /* checks if a difference is nearly zero*/
        public static bool aroundZero(float diff)
        {
            return (Math.Abs(diff) <= _toleranceThreshold);
        }

        /*------------------------------- FORTUNE ALGO COMMON METHODS -------------------------------*/
        /*
         * get parabola from site(focus) and sweepline(directrix). Site event
         * if F = (f1,f2)
         * d = ax + by + c
         * P = (x,y)... we get |PF|^2 = |PD|^2
         * parab eqn = (ax + by + c)^2 / (a^2 + b^2) = (x - f1)^2 + (y - f2)^2
         * LHS = Hesse Normal Form of line to get distance |Pl|.. We manipulate this below
         * https://en.wikipedia.org/wiki/Parabola#Definition_as_a_locus_of_points
         * https://jacquesheunis.com/post/fortunes-algorithm/
         */
        public static float yOnParabFromSiteAndX(Vector2 site, float sweepCoord, float x)
        {
            // given our defn of f and the dir, we can do this given an x coord
            // for d as y = yd and f = (f1,f2), we can find yp (parabola yCoord) st
            // yp = ( (x - f1)^2 / 2(f2 - yd) ) + (f2 + yd)/2
            float ypTerm1 = (float)Math.Pow(x - site.X, 2) / (float)(2 * (site.Y - sweepCoord));
            float ypTerm2 = (site.Y + sweepCoord) / 2;
            return ypTerm1 + ypTerm2;
        }

        /*
        * intersection of 2 parabs...aka getting the breakpoint between 2 sites
        * via computing parabola intersection given coord of sweepLine
        * Computed every time we need to determine if an inserted arc + or - of one in the RBT.
        * https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
        * https://github.com/jacobdweightman/fortunes-algorithm/blob/master/js/breakpoint.js -- Credited
        */
        public static float getBreakPtX(Vector2 s1, Vector2 s2, float sweepCoord)
        {
            // given y =  a1x^2 + b1x + c1, y =  a2x^2 + b2^x + c2
            // solve (a1 - a2)x^2 + (b1 - b2)y + (c1 - c2)
            // solve quad formula
            float a = s2.Y - s1.Y;
            float b = 2 * (s2.X * (s1.Y - sweepCoord) - (s1.X * (s2.Y - sweepCoord)));
            float c = (s1.X * s1.X * (s2.Y - sweepCoord)) - (s2.X * s2.X)
                * (s1.Y - sweepCoord) + ((s1.Y - s2.Y) * (s1.Y - sweepCoord) * (s2.Y - sweepCoord));

            // if a=0, quadratic formula does not apply
            if (Math.Abs(a) < _toleranceThreshold)
                return -c / b;

            float det = (float)Math.Sqrt(b * b - (4 * a * c));
            float x1 = (-b + det) / (2 * a);
            float x2 = (-b - det) / (2 * a);

            //if (s1.X < x1 && x1 < s2.X)
            //    return x1;

            //return x2;
            if (s1.Y < s2.Y)
                return Math.Max(x1, x2);

            return Math.Min(x1, x2);
        }

        /*
         * given 3 points, computes circumcenter of their triangle
         * so we can join the 2 Voronoi edges for bisectors (pi,pj) and (pj, pk) to it.
         * schedules Voronoi Vertex events
         * https://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations
         */
        public static Vector2 getCircumCenter(Vector2 s1, Vector2 s2, Vector2 s3)
        {
            if (s1 == default || s2 == default || s3 == default)
                return default!;

            float s1Scale = s1.X * s1.X + s1.Y * s1.Y;
            float s2Scale = s2.X * s2.X + s2.Y * s2.Y;
            float s3Scale = s3.X * s3.X + s3.Y * s3.Y;

            // D = 2 * [Ax * (By - Cy) + Bx * (Cy - Ay) + Cx * (Ay - By)]
            float d = 2 * (s1.X * (s2.Y - s3.Y) + s2.X * (s3.Y - s1.Y) + s3.X * (s1.Y - s2.Y));
            //Cart Coords of CC Ux = [(Ax^2 + Ay^2) * (By - Cy) + (Bx^2 + By^2) * (Cy - Ay) + (Cx^2 + Cy^2) * (Ay - By)] / D
            float Ux = (s1Scale * (s2.Y - s3.Y) + s2Scale * (s3.Y - s1.Y) + s3Scale * (s1.Y - s2.Y)) / d;
            //Cart Coords of CC Uy = [(Ax^2 + Ay^2) * (Cx - Bx) + (Bx^2 + By^2) * (Ax - Cx) + (Cx^2 + Cy^2) * (Bx - Ax)] / D
            float Uy = (s1Scale * (s3.X - s2.X) + s2Scale * (s1.X - s3.X) + s3Scale * (s2.X - s1.X)) / d;

            return new Vector2(Ux, Uy);
        }

        /* 
         * given RegionNodes bp1 and bp2 are part of a circle event, 
         * detect if at a future arbitrary point
         * that they become closer to the computed circumcircleCenter
         * assuming we sweep top-down on the y-axis
         * https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
         */
        public static bool checkIfCloserInFuture(Vector2 circCenter, RegionNode bp1, RegionNode bp2, float sweepCoord, float initDist)
        {
            // this increment is arbitrary,
            // what's important is the distance closes for both breakpts
            // as we approach the circumcircle's center
            float sweepYIncrement = (circCenter.Y - sweepCoord) / _convergeDivisor;

            //get future points
            float futureSweepY = sweepCoord - sweepYIncrement;
            float futureBp1X = getBreakPtX(bp1.regionSites[0], bp1.regionSites[1], futureSweepY);
            float futureBp2X = getBreakPtX(bp2.regionSites[0], bp2.regionSites[1], futureSweepY);
            Vector2 futureBp1 = new Vector2(futureBp1X, futureSweepY);
            Vector2 futureBp2 = new Vector2(futureBp2X, futureSweepY);

            bool futureBp1Closer = computeEuclidDist(futureBp1, circCenter) < initDist;
            bool futureBp2Closer = computeEuclidDist(futureBp2, circCenter) < initDist;

            return futureBp1Closer && futureBp2Closer;
        }

        /*
         * given a start and end pts of a line, determine if a point is right of the line defined by them
         * right determined from start's perspective....Returns true if it's on the line as well.
         *  https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
         */
        public static bool isRightOfLine(Vector2 start, Vector2 end, Vector2 point)
        {
            return ((end.X - start.X) * (point.Y - start.Y) - (end.Y - start.Y) * (point.X - start.X)) <= 0;
        }

        /*
		* helper for detectCircleEvent
		* check if two breakpoints of a consecutive triple converge....
		* this means the bisectors that define them move in opposite directions and never intersect at the circumcenter
        * breakpts converge if the center of the circle
        * defined by the 3 sites is "in front" of the middle site
        * check if circleCenter is right of the lines formed from the left and middle sites
        * and the middle and right sites
		* https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
		*/
        public static bool doesTripleConverge(Vector2 circCenter, Vector2 left, Vector2 mid, Vector2 right)
        {
            return isRightOfLine(left, mid, circCenter) && isRightOfLine(mid, right, circCenter);
        }
    }
}