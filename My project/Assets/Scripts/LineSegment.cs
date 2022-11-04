/* filename: LineSegment.cs
 author: Sam Ford (stf8464)
 desc: utility for representing an edge using EtherReality's DCEL implementation
 due: Thursday 10/6
*/


using Ethereality.DoublyConnectedEdgeList;
using System.Numerics;

namespace FortuneAlgo
{
    public class LineSegment : IEdge<Vector2>
    {
        static Vector2 INFINITY = new Vector2(float.MaxValue, float.MaxValue); 

        //create a line segment with two endpoints
        public LineSegment(Vector2 start, Vector2 end)
        {
            PointA = start;
            PointB = end;
        }

        //create an edge with a startpoint that goes off into infinity
        public LineSegment(Vector2 start)
        {
            PointA = start;
            PointB = INFINITY;
        }

        //create the twin of a given segment.
        public LineSegment makeOwnTwin() 
        {
            return new LineSegment(this.PointB, this.PointA);
        }

        public Vector2 PointA{ get => this.PointA; set => PointA = value; }

        public Vector2 PointB{ get => this.PointB; set => PointB = value; }

        public override string ToString() => $"({PointA})->({PointB})";
    }
}
