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


        public VoronoiDiagram(List<Vector2> sites, DCEL vdDCEL, BBox bbox = null!)
        {
            _sites = sites!;
            _vdDCEL = vdDCEL;
            _vdBox = bbox!;
        }

        //constructor
        public VoronoiDiagram(List<Vector2> sites, BBox bbox)
        {
            _sites = sites!;
            _vdDCEL = null!;
            _vdBox = bbox!;
        }

        // given a box, a side, and a DCEL, determine which corner to make
        public Vertex createCorner(BBox box, boxBorders side, DCEL queryDCEL = null!)
        {
            Vertex corner = null!;
            Vertex dcelQuery = null!;
            switch (side)
            {
                case boxBorders.LEFT:
                    corner = new(box.getUpperLeft());
                    break;
                case boxBorders.TOP:
                    corner = new(box.upperRight);
                    break;
                case boxBorders.RIGHT:
                    corner = new(box.getLowerRight());
                    break;
                case boxBorders.BOTTOM:
                    corner = new(box.lowerLeft);
                    break;

            }

            if (queryDCEL != null && queryDCEL.Vertices != null && queryDCEL.Vertices.Count > 1)
                dcelQuery = queryDCEL.Vertices.FirstOrDefault(v => v == corner)!;

            corner = dcelQuery ?? corner;

            return corner;
        }

        public DCEL clipVDToBBox(BBox container)
        {
            if (container == null || container == this._vdBox)
                return this._vdDCEL;

            DCEL clippedDCEL = this._vdDCEL;
            bool error = false;
            List<HalfEdge> processedHEs = new();
            List<Vertex> verticesToRemove = new();

            foreach(HalfEdge he in clippedDCEL.HalfEdges)
            {
                HalfEdge travel = he;
                bool inside = container.containsPoint(he.Origin.Position);
                bool outerComponentDirty = !inside;
                HalfEdge incomingHE = null!;
                HalfEdge outgoingHE = null!;
                boxBorders incomingSide = boxBorders.NONE;
                boxBorders outgoingSide = boxBorders.NONE;
                do
                {
                    Vertex originVert = travel.Origin;
                    Vertex destVert = travel.getTarget();
                    List <Vector2> ints = container.getIntersections(originVert.Position, destVert.Position).ToList();
                    Vertex closeInterVert = new(ints[0]);
                    Vertex farInterVert = new(ints[1]);

                    boxBorders closeInterSide = container.grabBorderValueOfPoint(ints[0]);
                    boxBorders farInterSide = container.grabBorderValueOfPoint(ints[1]);

                    int intsCount = ints.Count;
                    bool nextInside = container.containsPoint(destVert.Position);
                    HalfEdge nextHE = travel.Next;

                    //case 2 & case 5
                    if (!inside && !nextInside)
                    {
                        if (intsCount == 0)
                        {
                            // case 2... completely outside the dang box so discard them
                            verticesToRemove.Add(originVert);
                            clippedDCEL.removeHE(he);
                        }

                        else if (intsCount == 2)
                        {
                            // case 5...the edge crosses the borders twice
                            verticesToRemove.Add(originVert);
                            if (processedHEs.IndexOf(travel.Twin) != processedHEs.Count - 1)
                            {
                                travel.Origin = travel.Twin.getTarget();
                                //  = travel.Twin.Origin;
                            }
                            else
                            {
                                // clip to part inside box
                                clippedDCEL.Add(closeInterVert);
                                clippedDCEL.Add(farInterVert);
                                travel.Origin = closeInterVert;
                                travel.Twin.Origin = farInterVert;
                                closeInterVert.Leaving = travel;
                                farInterVert.Leaving = travel.Twin;
                            }
                            if (outgoingHE != null)
                            {
                                // add halfedges between this one and the last halfedge 
                                // that went outside the box
                                link(container, outgoingHE, outgoingSide, travel, closeInterSide, clippedDCEL);
                            }

                            if (incomingHE == null)
                            {
                                // this is the latest he to go inside the box
                                incomingHE = travel;
                                incomingSide = closeInterSide;
                            }
                            // also latest to go outside so record where we exit for linking later
                            outgoingHE = travel;
                            outgoingSide = farInterSide;
                            processedHEs.Add(travel);
                        }
                        else
                            error = true;
                    }
                    // case 3. the edge is going outside the box
                    else if (inside && !nextInside)
                    {
                        if (intsCount == 1)
                        {
                            verticesToRemove.Add(originVert);
                            if (processedHEs.IndexOf(travel.Twin) != processedHEs.Count - 1)
                                travel.Twin.Origin = travel.Twin.getTarget();
                            else
                            {
                                travel.Twin.Origin = closeInterVert;
                                closeInterVert.Leaving = travel.Twin;
                            }
                            outgoingHE = travel;
                            outgoingSide = closeInterSide;
                            processedHEs.Add(travel);
                        }
                        else
                            error = true;
                    }

                    // case 4...the edge is going into the box
                    else if (!inside && nextInside)
                    {
                        if (intsCount == 1)
                        {
                            verticesToRemove.Add(originVert);
                            if (processedHEs.IndexOf(travel.Twin) != processedHEs.Count - 1)
                                travel.Origin = travel.Twin.getTarget();
                            else
                            {
                                travel.Origin = closeInterVert;
                                closeInterVert.Leaving = travel;
                            }

                            // link this halfedge to the last one that went out of the box
                            if (outgoingHE != null)
                                link(container, outgoingHE, outgoingSide, travel, closeInterSide, clippedDCEL);

                            if (incomingHE == null)
                            {
                                incomingHE = travel;
                                incomingSide = closeInterSide;
                            }
                            processedHEs.Add(travel);
                        }
                        else
                            error = true;
                    }
                    travel = nextHE;
                    inside = nextInside;
                } while (travel != he && !error);

                if (error)
                    return null!;

                // link the last and first hes inside the box
                if(outerComponentDirty && incomingHE != null)
                {
                    link(container, outgoingHE!, outgoingSide, incomingHE, incomingSide, clippedDCEL);
                }

                // set outer component for site's defining halfedge
                //if (outerComponentDirty)
                    //site.face.outerComponent = incomingHE;
            }

            foreach (Vertex v in verticesToRemove)
                clippedDCEL.removeVertex(v);

            return clippedDCEL;
        }


        /// <summary>
        /// add intermediate halfedges along a box's boundary
        /// between a start edge leaving the box
        /// and a end edge re-entering the box
        /// this is identical to my sewUpBoundaryPoints in FortuneTracer.cs
        /// </summary>
        /// <param name="container"></param>
        /// <param name="start"></param>
        /// <param name="startSide"></param>
        /// <param name="end"></param>
        /// <param name="endSide"></param>
        /// <param name="queryDCEL"></param>
        private void link(BBox container, HalfEdge start, boxBorders startSide, HalfEdge end, boxBorders endSide, DCEL queryDCEL = null!)
        {
            HalfEdge he = start;
            boxBorders side = startSide;
            int sideInt = (int) side;
            HalfEdge next;
            HalfEdge nextTwin;
            //HalfEdge origTwinNext = null!;
            //HalfEdge origTwinPrev = null!;

            while(side != endSide)
            {
                // sewing up intermediary corner halfedges
                // between start and end
                sideInt = (sideInt + 1) % 4;
                next = he.Next;
                nextTwin = next.Twin;
                next.Prev = he;
                next.Origin = he.getTarget();
                nextTwin = new();
                nextTwin.Twin = next;
                nextTwin.Origin = createCorner(container, (boxBorders) sideInt, queryDCEL);
                he = next;
            }

            // final boundary halfedge between last box he and end
            he.Next = new();
            next = he.Next;
            next.Prev = he;
            end.Prev = next;
            next.Next = end;
            next.Origin = he.getTarget();
            nextTwin = next.Twin;
            nextTwin = new();
            nextTwin.Twin = next;
            nextTwin.Origin = end.Origin;

            return;
        }
    }
}

/*
bool error = false;
std::unordered_set<HalfEdge*> processedHalfEdges;
std::unordered_set<Vertex*> verticesToRemove;
for (const Site& site : mSites)
{
HalfEdge* halfEdge = site.face->outerComponent;
bool inside = box.contains(halfEdge->origin->point);
bool outerComponentDirty = !inside;
HalfEdge* incomingHalfEdge = nullptr; // First half edge coming in the box
HalfEdge* outgoingHalfEdge = nullptr; // Last half edge going out the box
Box::Side incomingSide, outgoingSide;
do
{
std::array<Box::Intersection, 2> intersections;
int nbIntersections = box.getIntersections(halfEdge->origin->point, halfEdge->destination->point, intersections);
bool nextInside = box.contains(halfEdge->destination->point);
HalfEdge* nextHalfEdge = halfEdge->next;
// The two points are outside the box 
if (!inside && !nextInside)
{
    // The edge is outside the box
    if (nbIntersections == 0)
    {
        verticesToRemove.emplace(halfEdge->origin);
        removeHalfEdge(halfEdge);
    }
    // The edge crosses twice the frontiers of the box
    else if (nbIntersections == 2)
    {
        verticesToRemove.emplace(halfEdge->origin);
        if (processedHalfEdges.find(halfEdge->twin) != processedHalfEdges.end())
        {
            halfEdge->origin = halfEdge->twin->destination;
            halfEdge->destination = halfEdge->twin->origin;
        }
        else
        {
            halfEdge->origin = createVertex(intersections[0].point);
            halfEdge->destination = createVertex(intersections[1].point);
        }
        if (outgoingHalfEdge != nullptr)
            link(box, outgoingHalfEdge, outgoingSide, halfEdge, intersections[0].side);
        if (incomingHalfEdge == nullptr)
        {
           incomingHalfEdge = halfEdge;
           incomingSide = intersections[0].side;
        }
        outgoingHalfEdge = halfEdge;
        outgoingSide = intersections[1].side;
        processedHalfEdges.emplace(halfEdge);
    }
    else
        error = true;
}
// The edge is going outside the box
else if (inside && !nextInside)
{
    if (nbIntersections == 1)
    {
        if (processedHalfEdges.find(halfEdge->twin) != processedHalfEdges.end())
            halfEdge->destination = halfEdge->twin->origin;
        else
            halfEdge->destination = createVertex(intersections[0].point);
        outgoingHalfEdge = halfEdge;
        outgoingSide = intersections[0].side;
        processedHalfEdges.emplace(halfEdge);
    }
    else
        error = true;
}
// The edge is coming inside the box
else if (!inside && nextInside)
{
    if (nbIntersections == 1)
    {
        verticesToRemove.emplace(halfEdge->origin);
        if (processedHalfEdges.find(halfEdge->twin) != processedHalfEdges.end())
            halfEdge->origin = halfEdge->twin->destination;
        else
            halfEdge->origin = createVertex(intersections[0].point);
        if (outgoingHalfEdge != nullptr)
            link(box, outgoingHalfEdge, outgoingSide, halfEdge, intersections[0].side);
        if (incomingHalfEdge == nullptr)
        {
           incomingHalfEdge = halfEdge;
           incomingSide = intersections[0].side;
        }
        processedHalfEdges.emplace(halfEdge);
    }
    else
        error = true;
}
halfEdge = nextHalfEdge;
// Update inside
inside = nextInside;
} while (halfEdge != site.face->outerComponent);
// Link the last and the first half edges inside the box
if (outerComponentDirty && incomingHalfEdge != nullptr)
link(box, outgoingHalfEdge, outgoingSide, incomingHalfEdge, incomingSide);
// Set outer component
if (outerComponentDirty)
site.face->outerComponent = incomingHalfEdge;
}
// Remove vertices
for (auto& vertex : verticesToRemove)
removeVertex(vertex);
// Return the status
return !error;             
 */

/*
 void VoronoiDiagram::link(Box box, HalfEdge* start, Box::Side startSide, HalfEdge* end, Box::Side endSide)
{
    HalfEdge* halfEdge = start;
    int side = static_cast<int>(startSide);
    while (side != static_cast<int>(endSide))
    {
        side = (side + 1) % 4;
        halfEdge->next = createHalfEdge(start->incidentFace);
        halfEdge->next->prev = halfEdge;
        halfEdge->next->origin = halfEdge->destination;
        halfEdge->next->destination = createCorner(box, static_cast<Box::Side>(side));
        halfEdge = halfEdge->next;
    }
    halfEdge->next = createHalfEdge(start->incidentFace);
    halfEdge->next->prev = halfEdge;
    end->prev = halfEdge->next;
    halfEdge->next->next = end;
    halfEdge->next->origin = halfEdge->destination;
    halfEdge->next->destination = end->origin;
}
 */