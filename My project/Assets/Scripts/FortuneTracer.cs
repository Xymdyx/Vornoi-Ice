/* filename: FortuneTracer.cs
 author: Sam Ford (stf8464)
 desc: class that represents Fortune's Algorithm for
        tracing Vornoi diagrams using a sweepline method.
        Sweepline sweeps downward from max y to min y
 due: Thursday 11/29
https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
https://blog.ivank.net/fortunes-algorithm-and-implementation.html
https://jacquesheunis.com/post/fortunes-algorithm/
https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
*/
using CSHarpSandBox;
using Ethereality.DoublyConnectedEdgeList;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
//https://www.boost.org/doc/libs/1_80_0/boost/polygon/voronoi_builder.hpp

// section header:

/*------------------------------- SECTION HEADER -------------------------------*/

/* this flag is added when a part of the program
 * depends on how we define sweepline's axis and movement
 * remember we are sweeping from high y down to low y
 */
//SWEEP-AXIS-MOVE-DEPENDENT//

namespace FortuneAlgo
{
    public class FortuneTracer
    {
        /*------------------------------- FIELDS -------------------------------*/

        private const float _toleranceThreshold = 1e-8f;
		private const float _beachlineBoost = 1e-10f;
        private const float _convergeDivisor = 10f;
        private int _siteCount;
        private List<Vector2> _sites;
        private MinHeap<VoronoiEvent> _pq;
        // we use only site events in the beachline per Dave Mount's Lecture Notes
        private RedBlackTree<float> _beachLine;
        // private _bBox;
        public VoronoiDiagram _vd;


        /*------------------------------- CONSTRUCTOR -------------------------------*/
        public FortuneTracer(List<Vector2> sites)
        {
            this._sites = sites;
            this._siteCount = _sites.Count;
            //given as lowerLeft and upperRight corners
            //of 2d face of VoronoiObj in Unity. Bounds Voronoi Diagram
            //this._bBox =  bBox;
            this._pq = null!;
            this._beachLine = null!;
            this._vd = null!;
        }


        /*------------------------------- UTIL METHODS -------------------------------*/
        /*
        * gets Euclidean Distance of 2 sites if it's the desired distance metric 
        */
        private float computeEuclidDist(Vector2 s1, Vector2 s2)
        {
            return Vector2.Distance(s1, s2);
        }

		/* checks if a difference is nearly zero*/
		private bool aroundZero(float diff)
		{
			return (-(_toleranceThreshold) <= diff) && (diff <= _toleranceThreshold);
		}

        /* set two half edges as each other's twins */
        private void setHETwins(HalfEdge<LineSegment, Vector2> he1, HalfEdge<LineSegment, Vector2> he2)
        {
            he1.Twin = he2;
            he2.Twin = he1;
        }

        /*------------------------------- FORTUNE ALGO COMMON METHODS -------------------------------*/
        /*
         * get parabola from site(focus) and sweepline(directrix). Site event
         * if F = (f1,f2)
         * d = ax + by + c
         * P = (x,y)... we get |PF|^2 = |PD|^2
         * parab eqn = (ax + by + c)^2 / (a^2 + b^2) = (x - f1)^2 + (y - f2)^2
         * LHS = Hesse Normal Form of line to get distance |Pl|.. We manipulate this below
         * https://en.wikipedia.org/wiki/Parabola#Definition_as_a_locus_of_points
         * https://jacquesheunis.com/post/fortunes-algorithm/
         */
        public float yOnParabFromSiteAndX(Vector2 site, float sweepCoord, float x)
        {
            // given our defn of f and the dir, we can do this given an x coord
            // for d as y = yd and f = (f1,f2), we can find yp (parabola yCoord) st
            // yp = ( (x - f1)^2 / 2(f2 - yd) ) + (f2 + yd)/2
            float ypTerm1 = (float)Math.Pow(x - site.X, 2) / (float)(2 * (site.Y - sweepCoord));
            float ypTerm2 = site.Y + sweepCoord / 2;
            return ypTerm1 + ypTerm2;
        }

        /*
        * intersection of 2 parabs...aka getting the breakpoint between 2 sites
        * via computing parabola intersection given coord of sweepLine
        * Computed every time we need to determine if an inserted arc + or - of one in the RBT.
        * https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
        * https://github.com/jacobdweightman/fortunes-algorithm/blob/master/js/breakpoint.js -- Credited
        */
        public float getBreakPtX(Vector2 s1, Vector2 s2, float sweepCoord)
        {
            // given y =  a1x^2 + b1x + c1, y =  a2x^2 + b2^x + c2
            // solve (a1 - a2)x^2 + (b1 - b2)y + (c1 - c2)
            // solve quad formula

            float a = s2.Y - s1.Y;
            float b = 2 * (s2.X * (s1.Y - sweepCoord) - s1.X * (s2.Y - sweepCoord));
            float c = (s1.X * s1.X * (s2.Y - sweepCoord)) - (s2.X * s2.X)
                * (s1.Y - sweepCoord) + (s1.Y - s2.Y) * (s1.Y - sweepCoord) * (s2.Y - sweepCoord);

            // if a=0, quadratic formula does not apply
            if (Math.Abs(a) < 0.001)
                return -c / b;

            float x1 = (-b + (float)Math.Sqrt(b * b - (4 * a * c))) / (2 * a);
            float x2 = (-b - (float)Math.Sqrt(b * b - (4 * a * c))) / (2 * a);

            if (s1.X < x1 && x1 < s2.X)
                return x1;

            return x2;
        }


        /*------------------------------- CIRCLE EVENT DETECTION METHODS -------------------------------*/
        /*
         * given 3 points, computes circumcenter of their triangle
         * so we can join the 2 Voronoi edges for bisectors (pi,pj) and (pj, pk) to it.
         * schedules Voronoi Vertex events
         * https://en.wikipedia.org/wiki/Circumscribed_circle#Circumcircle_equations
         */
        public Vector2 getCircumCenter(Vector2 s1, Vector2 s2, Vector2 s3)
        {
            if (s1 == default || s2 == default || s3 == default)
                return default!;

            float s1Scale = s1.X * s1.X + s1.Y * s1.Y;
            float s2Scale = s2.X * s2.X + s2.Y * s2.Y;
            float s3Scale = s3.X * s3.X + s3.Y * s3.Y;

            // D = 2 * |Ax * (By - Cy) + Bx * (Cy - Ay) + Cx * (Ay - By)|
            float d = 2 * Math.Abs(s1.X * (s2.Y - s3.Y) + s2.X * (s3.Y - s1.Y) + s3.X * (s1.Y - s2.Y));
            //Cart Coords of CC Ux = [(Ax^2 + Ay^2) * (By - Cy) + (Bx^2 + By^2) * (Cy - Ay) + (Cx^2 + Cy^2) * (Ay - By)] / D
            float Ux = (s1Scale * (s2.Y - s3.Y) + s2Scale * (s3.Y - s1.Y) + s3Scale * (s1.Y - s2.Y)) / d;
            //Cart Coords of CC Uy = [(Ax^2 + Ay^2) * (Cx - Bx) + (Bx^2 + By^2) * (Ax - Cx) + (Cx^2 + Cy^2) * (Bx - Ax)] / D
            float Uy = (s1Scale * (s3.X - s2.X) + s2Scale * (s1.X - s3.X) + s3Scale * (s2.X - s1.X)) / d;

            return new Vector2(Ux, Uy);
        }

        /* 
         * given RegionNodes bp1 and bp2 are part of a circle event, 
         * detect if at a future arbitrary point
         * that they become closer to the computed circumcircleCenter
         * assuming we sweep top-down on the y-axis
         * https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
         */
        private bool checkIfCloserInFuture(Vector2 circCenter, RegionNode bp1, RegionNode bp2, float sweepCoord, float initDist)
        {
            // this increment is arbitrary,
            // what's important is the distance closes for both breakpts
            // as we approach the circumcircle's center
            float sweepYIncrement = (circCenter.Y - sweepCoord) / _convergeDivisor;

            //get future points
            float futureSweepY = sweepCoord + sweepYIncrement;
            float futureBp1X = getBreakPtX(bp1.regionSites[0], bp1.regionSites[1], futureSweepY);
            float futureBp2X = getBreakPtX(bp2.regionSites[0], bp2.regionSites[1], futureSweepY);
            Vector2 futureBp1 = new Vector2(futureBp1X, futureSweepY);
            Vector2 futureBp2 = new Vector2(futureBp2X, futureSweepY);

            bool futureBp1Closer = computeEuclidDist(futureBp1, circCenter) < initDist;
            bool futureBp2Closer = computeEuclidDist(futureBp2, circCenter) < initDist;

            return futureBp1Closer && futureBp2Closer;
        }

        /*
         * given a start and end pts of a line, determine if a point is right of the line defined by them
         *  https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
         */
        private bool isRightOfLine(Vector2 start, Vector2 end, Vector2 point)
        {
            return ((end.X - start.X) * (point.Y - start.Y) - (end.Y - start.Y) * (point.X - start.X)) <= 0;
        }

        /*
		* helper for detectCircleEvent
		* check if two breakpoints of a consecutive triple converge....
		* this means the bisectors that define them move in opposite directions and never intersect at the circumcenter
		* https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
		*/
        private bool doesTripleConverge(Vector2 circCenter, Vector2 left, Vector2 mid, Vector2 right)
        {
            /* original idea:
            * check if the ray between the two breakpoint's start and current point 
            * if the circCenter lies along both rays, then surely they converge
            * as they would have to head in the same direction... This should work 
            * since the breakpts trace out straight lines
            */

            /* StackOverflow: 
            * breakpts converge if the center of the circle
            * defined by the 3 sites is "in front" of the middle site
            * check if circleCenter is right of the lines formed from the left and middle sites
            * and the middle and right sites
            */
            return isRightOfLine(left, mid, circCenter) && isRightOfLine(mid, right, circCenter);
        }

        /*
		* determine if we have a circle event between 3 sites. 
		* Get Distance between circumcenter and site then find min y value of circle.
		* If it's below sweepline and the left and right breakpts converge, we've a circle event to add to the queue
        */
        public bool detectCircleEvent(RBNode<RegionNode> left, RBNode<RegionNode> mid, RBNode<RegionNode> right, float sweepCoord, MinHeap<VoronoiEvent> sweep)
        {
            RegionNode leftArc = left.obj;
            RegionNode midArc = mid.obj;
            RegionNode rightArc = right.obj;

            // guard clauses
            if (leftArc.isInternalNode() || midArc.isInternalNode() || rightArc.isInternalNode())
            {
                Console.WriteLine("detecting circleEvent not between 3 sites");
                return false;
            }

            Vector2 s1 = leftArc.regionSites[0];
            Vector2 s2 = midArc.regionSites[0];
            Vector2 s3 = rightArc.regionSites[0];
            Vector2 circCenter = getCircumCenter(s1, s2, s3);

            float dist1 = computeEuclidDist(s1, circCenter);
            float dist2 = computeEuclidDist(s2, circCenter);
            float dist3 = computeEuclidDist(s3, circCenter);

            float diff12 = dist1 - dist2;
            float diff13 = dist1 - dist3;
            float diff23 = dist2 - dist3;

            if (!(aroundZero(diff12)) || !(aroundZero(diff13)) || !(aroundZero(diff23)))
                Console.WriteLine($"Computed distances in circleEvent don't check out for {s1} {s2} {s3}");

            // SWEEP-AXIS-MOVE-DEPENDENT
            float circBottomY = circCenter.Y - dist1;
            if (circBottomY >= sweepCoord)
                return false;

            RegionNode leftBp = left.parent.obj;
            RegionNode rightBp = right.parent.obj;
            // if these don't converge by either method, we don't want them...
            if (!doesTripleConverge(circCenter, s1, s2, s3) &&
                !checkIfCloserInFuture(circCenter, leftBp, rightBp, sweepCoord, dist1))
                return false;

            //TODO
            //add circle event to EventQueue... give a reference to the squeezedArcNode
            VoronoiEvent circEvent = new VoronoiEvent(circCenter, circBottomY, mid, circCenter);
            sweep.InsertElementInHeap(circBottomY, circEvent);

            // set reference to circEvent in the middleNode that will get squeezed out
            mid.obj.leafCircleEvent = circEvent;
            return true;
        }

        /*------------------------------- SITE EVENT METHODS -------------------------------*/

        /*
		* private helper used by insertAndSplit 
		* traverse the beachline represented by the RedBlackTree
		* using getBreakPt at internal nodes....
		* go left if new site pi < breakPtX at an internal node
		* stop when we reach a leaf node.
		*/
        private RBNode<RegionNode> findArcAboveSite(Vector2 piSite, RedBlackTree<RegionNode> beach)
        {
            RBNode<RegionNode> parent = null!;
            RBNode<RegionNode> arcNode = beach.getRoot();

            float bpX = 0.0f;
            List<Vector2> bpSites = null!;

            //travel to a leafNode representing a lone arc
            while (!beach.isLeaf(arcNode))
            {
                parent = arcNode;
                bpSites = arcNode.obj.regionSites;
                if (bpSites.Count != 2)
                    Console.WriteLine($"Supposed internal arc has {bpSites.Count} instead of 2 arcs.");

                bpX = getBreakPtX(bpSites[0], bpSites[1], piSite.Y);
                if (piSite.X < bpX)
                    arcNode = arcNode.left;
                else
                    arcNode = arcNode.right;
            }

            return arcNode;
        }


        /*
		* splits an arc in twain as described in handleSiteEvent. 2 cases, one where focus (pi.x) is < pj.x
		* and another where pi.x >= pj.x
		* only called when there is at least one arc in the beachline since we get at most 2n-1 arcs...
		*/
        private void insertAndSplit(Vector2 piSite, MinHeap<VoronoiEvent> sweep, RedBlackTree<RegionNode> beach)
        {
            RBNode<RegionNode> pjNode = findArcAboveSite(piSite, beach);
            RegionNode pjArc = pjNode.obj;
            Vector2 pjSite = pjArc.regionSites[0];
            Vector2 breakPt = default;
            List<Vector2> regionDuo = null!;
            float pipjVal = 0.0f;
            float pjpiVal = 0.0f;
            float piVal = 0.0f;

            //we can use this divisor to allow us to exploit an infinite range.. 11/3 TODO
            float levelDivisor = 2f;

            // TODO...inserting left->right per level seems to minimize rotation...10/29
            // however, the longevity of this insertion pattern is dubious..
            // Try average as values for inner leaf nodes. Maybe used the height of returned node for divisor...
            if (pjSite.X > piSite.X)
            {
                pipjVal = getBreakPtX(piSite, pjSite, piSite.Y);
                pjpiVal = pipjVal - _beachlineBoost;

                piVal = (pipjVal + pjpiVal) / 2f;

                //pj = internal pipj;
                breakPt = new Vector2(pipjVal, piSite.Y);
                regionDuo = new List<Vector2> { piSite, pjSite };
                pjArc.leafToInternal(regionDuo, breakPt);

                //pj.val = pipjVal and insertion
                pjNode.key = pipjVal;
                beach.insert(pjpiVal, new RegionNode(pjSite, piSite, pjpiVal));
                beach.insert(pipjVal + _beachlineBoost, new RegionNode(pjSite, pipjVal + _beachlineBoost));
                beach.insert(piVal, new RegionNode(pjSite, piVal));
                beach.insert(pjpiVal + _beachlineBoost / 2f, new RegionNode(piSite, pjpiVal + _beachlineBoost / 2f));
                return;
            }

            // Case 2: pj.x <= pi.x
            pjpiVal = getBreakPtX(pjSite, piSite, piSite.Y);
            pipjVal = pjpiVal + _beachlineBoost;
            piVal = (pipjVal + pjpiVal) / 2f;

            // pj = internal pjpi;
            breakPt = new Vector2(pjpiVal, piSite.Y);
            regionDuo = new List<Vector2> { pjSite, piSite };
            pjArc.leafToInternal(regionDuo, breakPt);

            // pi.val = pjpiVal and insertion
            pjNode.key = pjpiVal;
            beach.insert(pjpiVal - _beachlineBoost, new RegionNode(pjSite, pjpiVal - _beachlineBoost));
            beach.insert(pipjVal, new RegionNode(piSite, pjSite, pipjVal));
            beach.insert(piVal, new RegionNode(piSite, piVal));
            beach.insert(pipjVal + _beachlineBoost / 2f, new RegionNode(pjSite, pipjVal + _beachlineBoost / 2f));

            return;
        }


        /*------------------------------- CIRCLE EVENT METHODS -------------------------------*/
        //removes an arc as described in handleCircleEvent.
        private void removeArc(float sweepCoord, MinHeap<VoronoiEvent> sweep, RedBlackTree<RegionNode> beach)
		{
			//TODO
			return;
		}


        /*------------------------------- HANDLE EVENT METHODS -------------------------------*/

        /* 
		 * handle site event. Splits an arc in half and modifies the RBT
		 */
        public void handleSiteEvent(float sweepCoord, MinHeap<VoronoiEvent> sweep, RedBlackTree<RegionNode> beach)
		{
			//TODO
			/*
			1. if T empty, insert arc immediately, else continue.
			
			2. Search in T for arc a vertically above new site pi. If a has a ptr to a circle event,
			we deactivate that event in the event queue Q as it's a false alarm
			
			3. replace leaf of T that reps a with a subtree having 3 leaves, the middle of which is pi.
			
			The other two internal nodes are edges and have <pjpi> and <pipj> to represent edges formed by splitting a, pj's arc.
			There are 2 cases here, one where pi's left of alpha and the other right or equal to alpha.
			
			4. Create new half edge records in VD DCEL for edge separating V(pi) and V(pj, which is being traced out by <pipj> and <pjpi>
			
			5. Check consecutive triples for circle event. 
			There are 2 new ones that we need to check pjLPred,pjL,pi and pi,pjR,pjRS. If so, add circle event into Q along with a ptr betwn
			the node in T, which also gets a ptr to the node in Q.
			*/
			return;
		}
		
		/* 
		 * handle valid circle event. 
		 * Draws a Voronoi Vertex and removes arc from beachline
		 */
		public void handleCircleEvent(VoronoiEvent circleEvent, MinHeap<VoronoiEvent> sweep, RedBlackTree<RegionNode> beach, Dcel<LineSegment,Vector2> vd)
		{
            if (vd == null || sweep == null || circleEvent == null || beach == null)
            {
                Console.WriteLine("Something's null in handleCircleEvent, stopping now");
                return;
            }

            float sweepCoord = circleEvent.weight;
            RBNode<RegionNode> squeezedNode = circleEvent.squeezedArcLeaf!;
            RegionNode squeezedArc = squeezedNode.obj;
            Vector2 squeezedFocus = squeezedArc.regionSites[0];

            // update breakpts with a and delete all circleEvents
            RBNode<RegionNode> squeezedParent = squeezedNode.parent!;
			RBNode<RegionNode> squeezedGrandParent = null!;
			
			if(squeezedParent != null)
				squeezedGrandParent = squeezedParent.parent;
			
            RBNode<RegionNode> squeezedSucc = beach.getSucc(squeezedNode)!;
            RBNode<RegionNode> squeezedPred = beach.getPred(squeezedNode)!;


            // update all breakpts to no longer have squeezed
            if (squeezedParent != null)
                squeezedParent.obj.internalToLeaf(squeezedFocus);
			if (squeezedGrandParent != null)
                squeezedGrandParent.obj.internalToLeaf(squeezedFocus);

            // disable all circle events involving squeezed 
            squeezedArc.leafCircleEvent.disableCircleEvent();
            if (squeezedSucc != null)
                squeezedSucc.obj.leafCircleEvent.disableCircleEvent();
            if (squeezedPred != null)
                squeezedPred.obj.leafCircleEvent.disableCircleEvent();

            beach.delete(squeezedArc.weight, squeezedArc);


            // add circle center to vertices and bind 2 halfEdges to it. IN PROGRESS 11/3
            Vector2 circleCenter = circleEvent.circleEventCenter;
            LineSegment upDangling = new LineSegment(circleCenter);
            LineSegment downDangling = upDangling.makeOwnTwin();

            Vertex<LineSegment, Vector2> eventCenter = new Vertex<LineSegment, Vector2>(circleCenter);
            HalfEdge<LineSegment, Vector2> newHEDown = new HalfEdge<LineSegment, Vector2>(downDangling, eventCenter);
            HalfEdge<LineSegment, Vector2> newHEUp = new HalfEdge<LineSegment, Vector2>(upDangling, eventCenter);
            setHETwins(newHEUp, newHEDown);
            eventCenter.HalfEdges.Add(newHEDown);
            eventCenter.HalfEdges.Add(newHEUp);

            vd.Vertices.Append(eventCenter);
            /*
             1. Delete leaf y that reps disappearing arc a from T. Update tuples repping breakpoints/edges at
			 Internal nodes. Rebalance as needed... Delete all circle events involving a from Q via using ptrs
			 from predecessor and successor of y in T (circle event w a as middle is being handled in this method)
            */

            //TODO
            /*2. Add center of circle causing the event as a vertex record to the DCEL d storing the VD. Make 2 half-edges
				corresponding to new breakpt of the beachline. Set ptrs between them appropriately. Attach 3 new records
				to half-edge records that end at the vertex.*/

            /*3. Check new triples of consecutive arcs that has former left neighbor of a as its middle to see if 2 new breakpoints/edges
				of the triple converge. If so, insert corresponding circle event into Q, and set ptrs between new circle event and corresponding
				leaf of T. Do same for triple where former right neighbor is middle arc....
			*/
            return;
		}


        /*------------------------------- MAIN ALGO & PREP -------------------------------*/

        // algorithm that executes Fortune's algo for Voronoi diagrams
        // via using sweepline and beachline of parabolas 
        // @returns a built DCEL
        public void fortuneAlgo(MinHeap<VoronoiEvent> sweep, RedBlackTree<RegionNode> beach)
        {
			//TODO
			
			//1.Fill the event queue with site events for each input site.
			//2.While the event queue still has items in it:
			//    If the next event on the queue is a site event:
			//        Add the new site to the beachline
			//    Otherwise it must be an edge-intersection event:
			//        Remove the squeezed cell from the beachline
			//3.Cleanup any remaining intermediate state
            return;
        }

        // initialize SweepLine
		// TODO may need to switch to maximal Y or switch beachline to be ordered by X...
        public MinHeap<VoronoiEvent> initSweep()
        {
            List<Vector2> orderedSites = new List<Vector2>(this._sites);
            List<VoronoiEvent> events = new List<VoronoiEvent>();
            // sort by minimal y then x
            orderedSites.Sort((s1, s2) =>
            {
                int ret = s1.X.CompareTo(s2.X);
                return ret != 0 ? ret : s1.Y.CompareTo(s2.Y);
            });

            VoronoiEvent ev = null!;
            MinHeap<VoronoiEvent> pq = new MinHeap<VoronoiEvent>();
            foreach (Vector2 site in orderedSites)
            {
                ev = new VoronoiEvent(site, site.Y);
                pq.InsertElementInHeap(site.Y, ev);
            }

            return pq;
        }

        // initialize beachline with sites only per class notes
        // what is stored in the beachline is the main variation
        // with Fortune's Algorithm. Helpful for cases where multiple
		// beginning sites are within a certain y-value of each other.
        public RedBlackTree<RegionNode> initBeachSO()
        {
            return new RedBlackTree<RegionNode>();
        }

        //main algorithm for this class
        public void fortuneMain()
        {
            MinHeap<VoronoiEvent> sweep = initSweep();
            RedBlackTree<RegionNode> beach = initBeachSO();
            fortuneAlgo(sweep, beach);
            return;
        }
    }
}

/* used while trying to figure out how to get break pt using circle with sites pi and pj and touching sweep
* https://www.emathzone.com/tutorials/geometry/equation-of-a-circle-given-two-points-and-tangent-line.html - 2 methods
* https://www.youtube.com/watch?v=nRAT0cyp74o -- via distance. These are great if the line has 1 perpendicular line...
* https://www.youtube.com/watch?v=DsaYcD_Ab9I&t=194s -- circle touches a line. Doesn't work for sweepline
 */