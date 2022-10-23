/* filename: VoronoiEvent.cs
 author: Sam Ford (stf8464)
 desc: class that represents an item in the priority queue used
        in Fortune's algo. Can represent a site, region, or parabola.
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
        //can be up to length 3. Has cross-link with region nodes.
        private List<Vector2> _eventSites;
        private int _weight;
        private RegionNode _vorVertRegion;

        //user for cross-link between vertexEvents and triples.
        public RegionNode vorVertRegion { get => this._vorVertRegion; set => this._vorVertRegion = value; }

        //single site constructor
        public VoronoiEvent(Vector2 eventSite, int weight)
        {
            this._eventSites = new List<Vector2>{eventSite};
            this._weight = weight;
            this._vorVertRegion = null!;
        }

        //multiple site constructor
        public VoronoiEvent(List<Vector2> eventSites, int weight)
        {
            if (eventSites.Count > 3)
                Console.WriteLine("Event with more than 3 sites");

            this._eventSites = eventSites;
            this._weight = weight;
            this._vorVertRegion = null!;

        }
    }
}
