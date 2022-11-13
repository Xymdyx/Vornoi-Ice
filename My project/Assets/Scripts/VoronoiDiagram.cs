using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace FortuneAlgo
{
    public class VoronoiDiagram
    {
        private List<Vector2> _sites;
        private DCEL _vdDCEL;
        private BBox _vdBox;

        //constructor
        VoronoiDiagram(List<Vector2> sites, BBox bbox)
        {
            _sites = sites!;
            _vdDCEL = null!;
            _vdBox = bbox!;
        }
    }
}
