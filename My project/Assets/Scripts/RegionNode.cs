/* filename: RegionNode.cs
 author: Sam Ford (stf8464)
 desc: class that represents an item in the BeachLine.
        Can be a site and its space or a parabola w a focus and directrix (sweepline)
 due: Thursday 11/29
*/
using FortuneAlgo;
using System.Numerics;
using System.Linq;
using System;
using System.Text;
using System.Collections.Generic;

namespace FortuneAlgo
{
    // open to expansion later on. 
    public class RegionNode
    {
        // can be up to length 2. 
        private List<Vector2> _regionSites;
        private float _weight;

        // Has cross-link with VoronoiVertex event.
        private VoronoiEvent _makerEvent;
		private HalfEdge _dcelHalfEdge;


        /* PROPERTIES */
        public List<Vector2> regionSites { get => this._regionSites; }
        public float weight { get => this._weight; }
        public HalfEdge dcelEdge { get => this._dcelHalfEdge; internal set => this._dcelHalfEdge = value; }

        //only for triples
        public VoronoiEvent leafCircleEvent { get => this._makerEvent; set => this._makerEvent = value; }

        //only for internal nodes

        //constructor for leaf node..Usable in RBT
        public RegionNode(Vector2 regionSite, float weight)
        {
            _regionSites = new List<Vector2>{regionSite};
            this._weight = weight;
            this._makerEvent = null!;
			this._dcelHalfEdge = null!;
        }


        //constructor for internalNodes used in insertAndSplit
        public RegionNode(Vector2 leftSite, Vector2 rightSite, HalfEdge dcelHalfEdge, float weight)
        {

            _regionSites = new List<Vector2> {leftSite, rightSite};
            this._weight = weight;
            this._makerEvent = null!;
            this._dcelHalfEdge = dcelHalfEdge;
        }
		
		//convenience method for telling if a node is a parent.
		public bool isInternalNode()
		{
			return this._regionSites.Count == 2;
		}
		
		/*
		 * change leaf to parent during split event
		 * @param: regionDuo -- the new parabola pair that this edge (two halfedges) is being traced by
		 * @param: dcelHalfEdge -- one of two pre-made halfedge being traced out by two colliding parabolas,
		 * specified by the regionDuo whose twin is the reverse of this regionDuo.
		 * @param: vdDcel -- the master DCEL that we are drawing a dangling edge of
		 */
		public void leafToInternal(List<Vector2> regionDuo, HalfEdge dcelHalfEdge)
		{
            this.leafDisableCircleEvent();
            // replace sites
            this._regionSites = regionDuo;
            this._dcelHalfEdge = dcelHalfEdge;
		}
		
        
		// change parent to leaf
		public void internalToLeaf(Vector2 deadArc)
		{
            // remove deadArc
            this._regionSites.Remove(deadArc);
            this._makerEvent = null!;
		}

        // update internal and keep it internal
        public int updateInternal(Vector2 deadArc, Vector2 newArc, Vector2 circleCenter, DCEL voronoiDCEL) 
        {
            if (!isInternalNode())
            {
                Console.WriteLine("Trying to update a non-internal node");
                return -1;
            }

            int deadIdx = this._regionSites.IndexOf(deadArc);
            if (deadIdx == -1)
            {
                Console.WriteLine("Dead arc not found in internal node");
                return -1;
            }

            _regionSites[deadIdx] = newArc;
            HalfEdge myTwin = this._dcelHalfEdge.Twin;

            // find out which halfedge goes off into infinity.. Should only be one.. Revise later possibly 11/12
            bool isTwinInfiniteHE = (myTwin.Origin == voronoiDCEL.InfiniteVertex);
            bool isThisInfiniteHE = (this._dcelHalfEdge.Origin == voronoiDCEL.InfiniteVertex);
            if (isTwinInfiniteHE && isThisInfiniteHE)
            {
                Console.WriteLine("This half edge and its twin are  both infinite? Region Node death");
                Environment.Exit(-1);
            }

            else if (isThisInfiniteHE)
                this._dcelHalfEdge.Origin = new Vertex(this._dcelHalfEdge, circleCenter);

            else if (isTwinInfiniteHE)
                myTwin.Origin = new Vertex(myTwin, circleCenter);

            return 0;
        }

        /* 
         * given another internalNode, return this internalNode's unqiue site(s)
         * return null if they aren't both internal
         */
        public List<Vector2> getInternalUniqueSites(RegionNode other) 
        {
            if (!other.isInternalNode() || !this.isInternalNode())
                return null!;
            
            List<Vector2> otherSites = other.regionSites;
            List<Vector2> uniqueSites = new List<Vector2>(2);

            if (!otherSites.Contains(regionSites[0]))
                uniqueSites.Add(regionSites[0]);

            if (!otherSites.Contains(regionSites[1]))
                uniqueSites.Add(regionSites[1]);

            return uniqueSites;
        }

        /*
        * intersection of 2 parabs...aka getting the breakpoint between 2 sites
        * via computing parabola intersection given coord of sweepLine
        * Computed every time we need to determine if an inserted arc + or - of one in the RBT.
        * https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
        * https://github.com/jacobdweightman/fortunes-algorithm/blob/master/js/breakpoint.js -- Credited
        */
        public float getInternalBreakPtX(float sweepCoord)
        {
            // given y =  a1x^2 + b1x + c1, y =  a2x^2 + b2^x + c2
            // solve (a1 - a2)x^2 + (b1 - b2)y + (c1 - c2)
            // solve quad formula
		
		    if(!this.isInternalNode()) 
			    return regionSites[0].X;
		
            Vector2 s1 = this.regionSites[0];
            Vector2 s2 = this.regionSites[1];
            float a = s2.Y - s1.Y;
            float b = 2 * (s2.X * (s1.Y - sweepCoord) - s1.X * (s2.Y - sweepCoord));
            float c = (s1.X * s1.X * (s2.Y - sweepCoord)) - (s2.X * s2.X)
                * (s1.Y - sweepCoord) + (s1.Y - s2.Y) * (s1.Y - sweepCoord) * (s2.Y - sweepCoord);

            // if a=0, quadratic formula does not apply
            if (Math.Abs(a) < 0.001)
                return -c / b;

            float x1 = (-b + (float)Math.Sqrt(b * b - (4 * a * c))) / (2 * a);
            float x2 = (-b - (float)Math.Sqrt(b * b - (4 * a * c))) / (2 * a);

            if (s1.X < x1 && x1 < s2.X)
                return x1;

            return x2;
        }

        /// <summary>
        /// util method for disabling active circle event.
        /// </summary>
        public void leafDisableCircleEvent()
        {
            if (isInternalNode())
                Console.Write("Trying to disable circle event in internal node");
            else if (this.leafCircleEvent != null)
                this.leafCircleEvent.disableCircleEvent();

            return;
        }
    }

    public class RegionNodeComp : IComparer<RegionNode>
    {
        // we need to know where the sweepLine is before we traverse
        // we can use this when we traverse a list of values
        // during INSERTION into the Beachline.
        // we store nodes as values and their keys as the arbitrary value
        // derived for structural purposes.
        public float currSweepCoord { get; set; }

        public RegionNodeComp(float currSweepCoord)
        {
            this.currSweepCoord = currSweepCoord;
        }

        public int Compare(RegionNode? r1, RegionNode? r2) 
        {
            if (r1 == null || r2 == null)
                return 0;

            float r1Val = 0;
            float r2Val = 0;
            r1Val = r1.isInternalNode() ? r1.regionSites[0].X : r1.getInternalBreakPtX(currSweepCoord);
            r2Val = r2.isInternalNode() ? r1.regionSites[0].X : r2.getInternalBreakPtX(currSweepCoord);

            return r1Val.CompareTo(r2Val);
        }
    }
}
