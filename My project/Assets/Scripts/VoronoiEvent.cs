/* filename: VoronoiEvent.cs
 author: Sam Ford (stf8464)
 desc: class that represents an item in the priority queue used
        in Fortune's algo. Represents a site event or Voronoi vertex event.
        Sorted by minimal y-value, then by x...
 due: Thursday 11/29
*/
using System;
using System.Collections.Generic;
using System.Numerics;
using FortuneAlgo;

namespace FortuneAlgo
{
    public class VoronoiEvent
    {
        // the location of the event, given as xy coords
        private Vector2 _eventSite;
        // sanity check for what's in the priority queue
        private float _weight;

        // for circleEvents only
        private RBNode<RegionNode> _squeezedArcLeaf;
        private bool _circleEventIsActive;
        private Vector2 _circleEventCenter;

        /* PROPERTIES */
        public Vector2 eventSite { get => this._eventSite; }
        public float weight { get => this._weight; }

        // user for cross-link between vertexEvents and squuezedArc
        // middle parabola gets squeezed out always
        public RBNode<RegionNode> squeezedArcLeaf { get => this._squeezedArcLeaf; }
        public bool circleEventIsActive { get => this._circleEventIsActive; }
        public Vector2 circleEventCenter { get => this._circleEventCenter; }

        //single site constructor
        public VoronoiEvent(Vector2 eventSite, float weight)
        {
            this._eventSite = eventSite;
            this._weight = weight;
            this._squeezedArcLeaf = null!;
            this._circleEventIsActive = false;
            this._circleEventCenter = default;
        }

        //circle event/Voronoi Vertex constructor
        public VoronoiEvent(Vector2 eventSite, float weight, RBNode<RegionNode> squeezedArcLeaf, Vector2 circleCenter)
        {
            this._eventSite = eventSite;
            this._weight = weight;
            if(squeezedArcLeaf == null || circleCenter == default)
            {
                Console.WriteLine("Constructing circle event with null node or default Vector2, ending program");
                Environment.Exit(-1);
            }

            if (squeezedArcLeaf.obj.isInternalNode())
            {
                Console.WriteLine("Constructing circle event with internal node, ending program");
                Environment.Exit(-1);
            }

            this._squeezedArcLeaf = squeezedArcLeaf;
            this._circleEventIsActive = true;
            this._circleEventCenter = circleCenter;
        }

        /*
        * called in main Fortune Algo loop 
        */
        public bool isSiteEvent()
        {
            return this._squeezedArcLeaf == null;
        }

        /*
         * disable _squeezedArcLeaf, called during insertAndSplit
         * if the leaf we split has a circleEvent ptr
         */
        public void disableCircleEvent() 
        {
            if(!isSiteEvent())
                this._circleEventIsActive = false;
        }
        
    }
}
