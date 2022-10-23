/* filename: RegionNode.cs
 author: Sam Ford (stf8464)
 desc: class that represents an item in the BeachLine.
        Can be a site and its space or a parabola w a focus and directrix (sweepline)
 due: Thursday 11/29
*/
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
        //can be up to length 3. Has cross-link with event nodes.
        private List<Vector2> _regionSites;
        private int _weight;
        private VoronoiEvent _makerEvent;

        //only for triples
        public VoronoiEvent makerEvent { get => this._makerEvent; set => this._makerEvent = value;}

        //constructor for site-only node..Usable in RBT
        RegionNode(Vector2 regionSite, int weight)
        {
            _regionSites = new List<Vector2>{regionSite};
            this._weight = weight;
            this._makerEvent = null!;
        }


        //constructor for site-only node..Multiple sites
        RegionNode(List<Vector2> regionSites, int weight)
        {
            if (regionSites.Count > 3)
                Console.WriteLine("Region with more than 3 sites");

            _regionSites = regionSites;
            this._weight = weight;
            this._makerEvent = null!;

        }

    }
}
