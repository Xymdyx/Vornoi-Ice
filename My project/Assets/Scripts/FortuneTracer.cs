/* filename: FortuneTracer.cs
 author: Sam Ford (stf8464)
 desc: class that represents Fortune's Algorithm for
        tracing Vornoi diagrams using a sweepline method.
 due: Thursday 11/29
https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
https://blog.ivank.net/fortunes-algorithm-and-implementation.html
https://jacquesheunis.com/post/fortunes-algorithm/
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
            this._vd = null!;
        }

        /*
         * for parabola intersection
         * https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
         */
        public bool doParabsIntersect()
        {
            return true;
        }

        /*
         * get parabola from site(focus) and sweepline(directrix)
         * if F = (f1,f2)
         * d = ax + by + c
         * P = (x,y)... we get |PF|^2 = |PD|^2
         * parab eqn = (ax + by + c)^2 / (a^2 + b^2) = (x - f1)^2 + (y - f2)^2
         * LHS = Hesse Normal Form of line to get distance |Pl|.. We manipulate this below
         * https://en.wikipedia.org/wiki/Parabola#Definition_as_a_locus_of_points
         */
        public int yOnParabFromSiteAndX(Vector2 site, int sweepCoord, int x)
        {
            // given our defn of f and the dir, we can do this given an x coord
            // for d as y = yd and f = (f1,f2), we can find yp (parabola yCoord) st
            // yp = ( (x - f1)^2 / 2(f2 - yd) ) + (f2 + yd)/2
            //https://jacquesheunis.com/post/fortunes-algorithm/
            int ypTerm1 = (int) Math.Pow(x - site.X, 2) / (int) (2 * (site.Y - sweepCoord));
            int ypTerm2 = (int) site.Y + sweepCoord / 2;
            return ypTerm1 + ypTerm2;
        }

        // algorithm that executes Fortune's algo for Voronoi diagrams
        // via using sweepline and beachline of parabolas 
        // @returns a DCEL
        public void fortuneAlgo()
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
            return;
        }
    }
}
