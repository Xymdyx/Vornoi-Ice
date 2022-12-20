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

        /// <summary>
        /// fill infinite point with endPt
        /// </summary>
        /// <param name="endPt"></param>
        public void fillInfiniteEndPt(Vector2 endPt)
        {
            HalfEdge myTwin = this._dcelHalfEdge.Twin;

            // find out which halfedge goes off into infinity.. Should only be one.. Revise later possibly 11/12
            bool isTwinInfiniteHE = (myTwin.Origin == DCEL.INFINITY);
            bool isThisInfiniteHE = (this._dcelHalfEdge.Origin == DCEL.INFINITY);
            if (isTwinInfiniteHE && isThisInfiniteHE)
            {
                Console.WriteLine("This half edge and its twin are  both infinite? Region Node death");
                Environment.Exit(-1);
            }

            else if (isThisInfiniteHE)
                this._dcelHalfEdge.Origin = new Vertex(this._dcelHalfEdge, endPt);

            else if (isTwinInfiniteHE)
                myTwin.Origin = new Vertex(myTwin, endPt);
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
            this.fillInfiniteEndPt(circleCenter);

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

        /// <summary>
        /// tells if an internal node's edge is dangling or not, i.e. if one 
        /// vertex is infinite and the other isn't.
        /// </summary>
        /// <returns></returns>
        public bool isEdgeDangling()
        {
            if (this.isInternalNode() && 
                (this.dcelEdge.Origin == DCEL.INFINITY ^ this.dcelEdge.Twin.Origin == DCEL.INFINITY))
                return true;
            
            return false;
        }


        public override string ToString()
        {
            if (isInternalNode())
                return ($"IN: {this.regionSites[0]}->{this.regionSites[1]} he: {this.dcelEdge}");
            return ($"AN: {this.regionSites[0]} CE:{this.leafCircleEvent}");
        }
    }
}
