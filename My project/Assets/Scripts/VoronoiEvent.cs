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


namespace CSHarpSandBox
{
    public class VoronoiEvent
    {
        // the location of the event, given as xy coords
        private Vector2 _eventSite;
        private float _weight;
        private List<RegionNode> _vorVertexTriple;
        private bool _vorVertexIsActive;

        /* PROPERTIES */
        public Vector2 eventSite { get => this._eventSite; }
        public float weight { get => this._weight; }

        // user for cross-link between vertexEvents and triples.
        // middle parabola gets squeezed out always
        public List<RegionNode> vorVertTriple { get => this._vorVertexTriple; }
        public bool vorVertexIsActive { get => this._vorVertexIsActive; }

        //single site constructor
        public VoronoiEvent(Vector2 eventSite, float weight)
        {
            this._eventSite = eventSite;
            this._weight = weight;
            this._vorVertexTriple = null!;
            this._vorVertexIsActive = false;
        }

        //Voronoi Vertex constructor
        public VoronoiEvent(Vector2 eventSite, float weight, List<RegionNode> vorVertexTriple)
        {
            this._eventSite = eventSite;
            this._weight = weight;

            if (vorVertexTriple.Count != 3)
            {
                Console.WriteLine("Constructing circle event without 3 parabolas, ending program");
                Environment.Exit(-1);
            }

            this._vorVertexTriple = vorVertexTriple;
            this._vorVertexIsActive = false;

        }

        /*
        * called in main Fortune Algo loop 
        */
        public bool isSiteEvent()
        {
            return this._vorVertexTriple == null;
        }

        /*
         * disable _vorVertexTriple, called during insertAndSplit
         * if the leaf we split has a circleEvent ptr
         */
        public void disableVertexEvent() 
        {
            if(!isSiteEvent())
                this._vorVertexIsActive = false;
        }
        
    }
}
