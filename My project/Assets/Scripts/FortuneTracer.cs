/* filename: FortuneTracer.cs
 author: Sam Ford (stf8464)
 desc: class that represents Fortune's Algorithm for
        tracing Vornoi diagrams using a sweepline method.
 due: Thursday 11/29
https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
https://blog.ivank.net/fortunes-algorithm-and-implementation.html
https://jacquesheunis.com/post/fortunes-algorithm/
https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
*/
using CSHarpSandBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace FortuneAlgo
{
    public class FortuneTracer
    {
        private int _siteCount;
        private List<Vector2> _sites;
        private MinHeap<VoronoiEvent> _pq;
        // we use only site events in the beachline per Dave Mount's Lecture Notes
        private RedBlackTree<int> _beachLine;
        // private _bBox;
        public VoronoiDiagram _vd;

        //constructor
        public FortuneTracer(List<Vector2> sites)
        {
            this._sites = sites;
            this._siteCount = _sites.Count;
            //given as lowerLeft and upperRight corners
            //of 2d face of VoronoiObj in Unity. Bounds Voronoi Diagram
            //this._bBox =  bBox;
            this._pq = null!;
            this._beachLine = null!;
            this._vd = null!;
        }

        /*----------------------------DISTANCE METRICS----------------------------*/
        /*
        * gets Euclidean Distance of 2 sites if it's the desired distance metric 
        */
        public float computeEuclidDist(Vector2 s1, Vector2 s2)
        {
            return Vector2.Distance(s1, s2);
        }

        /*----------------------------FORTUNE'S ALGO----------------------------*/

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
        public int yOnParabFromSiteAndX(Vector2 site, int sweepCoord, int x)
        {
            // given our defn of f and the dir, we can do this given an x coord
            // for d as y = yd and f = (f1,f2), we can find yp (parabola yCoord) st
            // yp = ( (x - f1)^2 / 2(f2 - yd) ) + (f2 + yd)/2
            int ypTerm1 = (int)Math.Pow(x - site.X, 2) / (int)(2 * (site.Y - sweepCoord));
            int ypTerm2 = (int)site.Y + sweepCoord / 2;
            return ypTerm1 + ypTerm2;
        }

        /*
        * intersection of 2 parabs...aka getting the breakpoint between 2 sites
        * via computing parabola intersection given coord of sweepLine
        * Computed every time we need to determine if an inserted arc + or - of one in the RBT.
        * https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
        * https://github.com/jacobdweightman/fortunes-algorithm/blob/master/js/breakpoint.js -- Credited
        */
        public float getBreakPtX(Vector2 s1, Vector2 s2, int sweepCoord)
        {
            // given y =  a1x^2 + b1x + c1, y =  a2x^2 + b2^x + c2
            // solve (a1 - a2)x^2 + (b1 - b2)y + (c1 - c2)
            // solve quad formula

            float a = s2.Y - s1.Y;
            float b = 2 * (s2.X * (s1.Y - sweepCoord) - s1.X * (s2.Y - sweepCoord));
            float c = (s1.X * s1.X * (s2.Y - sweepCoord) ) - (s2.X * s2.X)
                * (s1.Y - sweepCoord) + (s1.Y - s2.Y) * (s1.Y - sweepCoord) * (s2.Y - sweepCoord);

            // if a=0, quadratic formula does not apply
            if (Math.Abs(a) < 0.001)
                return -c / b;

            float x1 = (-b + (float) Math.Sqrt(b * b - (4 * a * c)) ) / (2 * a);
            float x2 = (-b - (float)Math.Sqrt(b * b - (4 * a * c))) / (2 * a);

            if (s1.X < x1 && x1 < s2.X)
                return x1;

            return x2;
        }

        /*
         * given 3 points, computes circumcenter of their triangle
         * so we can join the 2 Voronoi edges for bisectors (pi,pj) and (pj, pk) to it.
         * schedules Voronoi Vertex events
         * https://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations
         */
        public Vector2 getCircumCenter(Vector2 s1, Vector2 s2, Vector3 s3, int sweepCoord)
        {
            float s1Scale = s1.X * s1.X + s1.Y * s1.Y;
            float s2Scale = s2.X * s2.X + s2.Y * s2.Y;
            float s3Scale = s3.X * s3.X + s3.Y * s3.Y;

            // D = 2 * |Ax * (By - Cy) + Bx * (Cy - Ay) + Cx * (Ay - By)|
            float d = 2 * Math.Abs(s1.X * (s2.Y - s3.Y) + s2.X * (s3.Y - s1.Y) + s3.X * (s1.Y - s2.Y));
            //Cart Coords of CC Ux = [(Ax^2 + Ay^2) * (By - Cy) + (Bx^2 + By^2) * (Cy - Ay) + (Cx^2 + Cy^2) * (Ay - By)] / D
            float Ux = (s1Scale * (s2.Y - s3.Y) + s2Scale * (s3.Y - s1.Y) + s3Scale * (s1.Y - s2.Y)) / d;
            //Cart Coords of CC Uy = [(Ax^2 + Ay^2) * (Cx - Bx) + (Bx^2 + By^2) * (Ax - Cx) + (Cx^2 + Cy^2) * (Bx - Ax)] / D
            float Uy = (s1Scale * (s3.X - s2.X) + s2Scale * (s1.X - s3.X) + s3Scale * (s2.X - s1.X)) / d;

            return new Vector2(Ux, Uy);
        }

        // algorithm that executes Fortune's algo for Voronoi diagrams
        // via using sweepline and beachline of parabolas 
        // @returns a built DCEL
        public void fortuneAlgo(MinHeap<VoronoiEvent> sweep, RedBlackTree<RegionNode> beach)
        {
        //1.Fill the event queue with site events for each input site.
        //2.While the event queue still has items in it:
        //    If the next event on the queue is a site event:
        //        Add the new site to the beachline
        //    Otherwise it must be an edge-intersection event:
        //        Remove the squeezed cell from the beachline
        //3.Cleanup any remaining intermediate state
            return;
        }

        //initialize SweepLine
        public MinHeap<VoronoiEvent> initSweep()
        {
            List<Vector2> orderedSites = new List<Vector2>(this._sites);
            List<VoronoiEvent> events = new List<VoronoiEvent>();
            // sort by minimal y then x
            orderedSites.Sort((s1, s2) =>
            {
                int ret = s1.X.CompareTo(s2.X);
                return ret != 0 ? ret : s1.Y.CompareTo(s2.Y);
            });

            VoronoiEvent ev = null!;
            MinHeap<VoronoiEvent> pq = new MinHeap<VoronoiEvent>();
            foreach (Vector2 site in orderedSites)
            {
                ev = new VoronoiEvent(site, ((int)site.Y));
                pq.InsertElementInHeap(site.Y, ev);
            }

            return pq;
        }

        // initialize beachline with sites only per class notes
        // what is stored in the beachline is the main variation
        // with Fortune's Algorithm
        public RedBlackTree<RegionNode> initBeachSO()
        {
            return new RedBlackTree<RegionNode>();
        }

        //main algorithm for this class
        public void fortuneMain()
        {
            MinHeap<VoronoiEvent> sweep = initSweep();
            RedBlackTree<RegionNode> beach = initBeachSO();
            fortuneAlgo(sweep, beach);
            return;
        }
    }
}

/* used while trying to figure out how to get break pt using circle with sites pi and pj and touching sweep
* https://www.emathzone.com/tutorials/geometry/equation-of-a-circle-given-two-points-and-tangent-line.html - 2 methods
* https://www.youtube.com/watch?v=nRAT0cyp74o -- via distance. These are great if the line has 1 perpendicular line...
* https://www.youtube.com/watch?v=DsaYcD_Ab9I&t=194s -- circle touches a line. Doesn't work for sweepline
 */