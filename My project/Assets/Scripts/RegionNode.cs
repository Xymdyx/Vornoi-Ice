/* filename: RegionNode.cs
 author: Sam Ford (stf8464)
 desc: class that represents an item in the BeachLine.
        Can be a site and its space or a parabola w a focus and directrix (sweepline)
 due: Thursday 11/29
*/
using Ethereality.DoublyConnectedEdgeList;
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
		private int _dcelEdge;


        /* PROPERTIES */
        public List<Vector2> regionSite { get => this._regionSites; }
        public float weight { get => this._weight; }

        //only for triples
        public VoronoiEvent leafVertexEvent { get => this._makerEvent; set => this._makerEvent = value; }

        //only for internal nodes
        public int internalDcelEdge { get => this._dcelEdge; }

        //constructor for leaf node..Usable in RBT
        RegionNode(Vector2 regionSite, float weight)
        {
            _regionSites = new List<Vector2>{regionSite};
            this._weight = weight;
            this._makerEvent = null!;
			this._dcelEdge = -1;
        }


        //constructor for internalNodes used in insertAndSplit
        RegionNode(List<Vector2> regionSites, float weight)
        {
            if (regionSites.Count != 2)
            {
                Console.WriteLine("Breakpoint without 2 sites, ending program");
                Environment.Exit(-1);
            }

            _regionSites = regionSites;
            this._weight = weight;
            this._makerEvent = null!;
            this._dcelEdge = 0;
        }
		
		//convenience method for telling if a node is a parent.
		public bool isInternalNode()
		{
			return this._regionSites.Count == 2;
		}
		
		/*
		 * change leaf to parent during split event
		 * @param: regionDuo -- the new parabola pair that this edge is being traced by
		 * @param: vdDcel -- the master DCEL that we are drawing a dangling edge of
		 */
		public void leafToInternal(List<Vector2> regionDuo, Vector2 breakPt, List<LineSegment> vdSegments)
		{
			// replace sites
			this._regionSites = regionDuo;
            // make dangling dcelEdge
            LineSegment dangling = new LineSegment(breakPt);
            //Vertex<LineSegment, Vector2> vertex = new Vertex<LineSegment, Vector2>(breakPt);
            //HalfEdge<LineSegment, Vector2> breakPtHE = new HalfEdge<LineSegment, Vector2>(dangling, vertex);
            vdSegments.Add(dangling);
		}
		
		//change parent to leaf
		public void internalToLeaf(Vector2 deadArc)
		{
            // remove deadArc
            this._regionSites.Remove(deadArc);
            this._makerEvent = null!;
			// clip dcelEdge and remove reference
		}
    }
}
