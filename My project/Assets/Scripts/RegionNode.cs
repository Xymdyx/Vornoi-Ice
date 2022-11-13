/* filename: RegionNode.cs
 author: Sam Ford (stf8464)
 desc: class that represents an item in the BeachLine.
        Can be a site and its space or a parabola w a focus and directrix (sweepline)
 due: Thursday 11/29
*/
using FortuneAlgo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CSHarpSandBox
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
        public HalfEdge dcelEdge { get => this._dcelHalfEdge; }

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
			// replace sites
			this._regionSites = regionDuo;
            // make dangling dcelEdge TODO
            //  Dcel<LineSegment, Vector2> vd = null! caller's responsibility to update DCEL before.
            //LineSegment dangling = new LineSegment(breakPt);
            //LineSegment danglingTwin = dangling.makeOwnTwin();
            //Vertex<LineSegment, Vector2> vertex = new (breakPt);
            //Vertex<LineSegment, Vector2> infVertex = new (LineSegment.INFINITY);
            //HalfEdge<LineSegment, Vector2> breakPtUpHE = new (dangling, vertex);
            //HalfEdge<LineSegment, Vector2> breakPtDownHE = new (danglingTwin, infVertex);
            //breakPtUpHE.Twin = breakPtDownHE;
            //breakPtDownHE.Twin = breakPtUpHE;
            this._dcelHalfEdge = dcelHalfEdge;

           
            //vdSegments.Add(dangling);
		}
		
        
		// change parent to leaf
		public void internalToLeaf(Vector2 deadArc)
		{
            // remove deadArc
            this._regionSites.Remove(deadArc);
            this._makerEvent = null!;
		}

        // update internal and keep it internal
        public int updateInternal(Vector2 deadArc, Vector2 newArc, Vector2 circleCenter) 
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
            myTwin.Twin.Origin = new Vertex(myTwin, circleCenter);

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
    }
}
