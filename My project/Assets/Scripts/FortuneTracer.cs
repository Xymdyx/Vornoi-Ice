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
https://stackoverflow.com/questions/16695440/boost-intrusive-binary-search-trees/18264705
*/

// MAJOR DESIGN STUFF:
// 1. Finish handle circle event
// 2. Finish site event
// 2. Switch from RedBlackTree to SortedDictionary for beachline
/*
//https://www.boost.org/doc/libs/1_80_0/boost/polygon/voronoi_builder.hpp
// https://www.geeksforgeeks.org/find-height-binary-tree-represented-parent-array/ get height of BST Node of element in sorted array in O(n) time
// helpful for sorted dictionary approach
*/

using System;
using System.Collections;
using System.Collections.Generic; // sorted dictionary for beachline
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
//https://www.boost.org/doc/libs/1_80_0/boost/polygon/voronoi_builder.hpp
// https://www.emathhelp.net/calculators/algebra-2/parabola-calculator/ -- parabola resources
// parabola calculator: https://www.omnicalculator.com/math/parabola
// geogebra intersection tool: https://www.geogebra.org/m/bduwwjqn
// circumcente calculator: https://www.omnicalculator.com/math/circumcenter-of-a-triangle

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

        public static float _toleranceThreshold = 1e-5f;
		private const float _beachlineBoost = 1e-10f;
        private const float _convergeDivisor = 10f;
        private int _siteCount;
        private List<Vector2> _sites;
        private MaxHeap<VoronoiEvent> _pq;
        // we use only site events in the beachline per Dave Mount's Lecture Notes
        private RedBlackTree<float> _beachLine;
        private BBox _bbox;
        public VoronoiDiagram _vd;


        /*------------------------------- CONSTRUCTORS -------------------------------*/
        public FortuneTracer(List<Vector2> sites, BBox bbox)
        {
            this._sites = sites;
            this._siteCount = _sites.Count;
            //given as lowerLeft and upperRight corners
            //of 2d face of VoronoiObj in Unity. Bounds Voronoi Diagram
            this._bbox =  bbox;
            this._pq = null!;
            this._beachLine = null!;
            this._vd = null!;
        }

        public FortuneTracer()
        {
            this._sites = null!;
            this._siteCount = 0;
            //given as lowerLeft and upperRight corners
            //of 2d face of VoronoiObj in Unity. Bounds Voronoi Diagram
            this._bbox = null!;
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
        private void setHETwins(HalfEdge he1, HalfEdge he2)
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
            float b = 2 * (s2.X * (s1.Y - sweepCoord) - (s1.X * (s2.Y - sweepCoord)));
            float c = (s1.X * s1.X * (s2.Y - sweepCoord)) - (s2.X * s2.X)
                * (s1.Y - sweepCoord) + ((s1.Y - s2.Y) * (s1.Y - sweepCoord) * (s2.Y - sweepCoord));

            // if a=0, quadratic formula does not apply
            if (Math.Abs(a) < _toleranceThreshold)
                return -c / b;

            float det = (float)Math.Sqrt(b * b - (4 * a * c));
            float x1 = (-b + det) / (2 * a);
            float x2 = (-b - det) / (2 * a);

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

            // D = 2 * [Ax * (By - Cy) + Bx * (Cy - Ay) + Cx * (Ay - By)]
            float d = 2 * (s1.X * (s2.Y - s3.Y) + s2.X * (s3.Y - s1.Y) + s3.X * (s1.Y - s2.Y));
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
        public bool detectCircleEvent(RBNode<RegionNode> left, RBNode<RegionNode> mid, RBNode<RegionNode> right, float sweepCoord, MaxHeap<VoronoiEvent> eventQueue)
        {
			
			if (left == null || mid == null || right == null)
            {
                Console.WriteLine("One or more sites passed to detectCircleEvent is null");
                return false;
            }
			
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

            //add circle event to EventQueue... give a reference to the squeezedArcNode
            VoronoiEvent circEvent = new VoronoiEvent(circCenter, circBottomY, mid, circCenter);
            eventQueue.InsertElementInHeap(circBottomY, circEvent);

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
        private RBNode<RegionNode> insertAndSplit(Vector2 piSite, MaxHeap<VoronoiEvent> eventQueue,
                RedBlackTree<RegionNode> beach, DCEL voronoiDCEL, RBNode<RegionNode> targetNode = null!)
        {
            RBNode<RegionNode> pjNode = (targetNode != null) ? targetNode : findArcAboveSite(piSite, beach);

            RegionNode piNode = null!;
            RegionNode pjArc = pjNode.obj;
            Vector2 pjSite = pjArc.regionSites[0];
            Vector2 breakPt = default;
            List<Vector2> regionDuo = null!;
            HalfEdge pipjHE = new();
            HalfEdge pjpiHE = new();

            float pipjVal = 0.0f;
            float pjpiVal = 0.0f;
            float piVal = 0.0f;
            float upperPjVal = 0.0f;
            float lowerPjVal = 0.0f;

            //we can use this divisor to allow us to exploit an infinite range
            float levelDivisor = 1f;
            RBNode<RegionNode> temp = pjNode;
            
            while(temp.parent != null) 
            {
                temp = temp.parent;
                levelDivisor += 1;
            }

            // inserting left->right per level seems to minimize rotation...10/29
            // however, the longevity of this insertion pattern is dubious..
            // Try average as values for inner leaf nodes. Maybe used the height of returned node for divisor...
            // 11/16 -- INSERT THE BREAKPT NODE FIRST so it can become an internal rather than a leaf...
            // ... verified 11/17.. prepare to test
            if (piSite.X < pjSite.X)
            {
                pipjVal = getBreakPtX(piSite, pjSite, piSite.Y);
                pjpiVal = pipjVal - (_beachlineBoost / levelDivisor);
                upperPjVal = pipjVal + (_beachlineBoost / levelDivisor);
                lowerPjVal = pjpiVal - (_beachlineBoost / (levelDivisor + 1));

                piVal = (pipjVal + pjpiVal) / 2f;
                piNode = new RegionNode(piSite, piVal);

                //pj = internal pipj;
                breakPt = new Vector2(pipjVal, piSite.Y);
                pipjHE.Origin = voronoiDCEL.InfiniteVertex; //right halfedge
                pjpiHE.Origin = new Vertex(pipjHE, breakPt); //left halfedge
                setHETwins(pipjHE, pjpiHE);
                regionDuo = new List<Vector2> { piSite, pjSite };
                pjArc.leafToInternal(regionDuo, pipjHE);

                //pj.val = pipjVal and insertion
                pjNode.key = pipjVal;
                beach.insert(pjpiVal, new RegionNode(pjSite, piSite, pjpiHE, pjpiVal)); //breakpt first - 11/16
                beach.insert(upperPjVal, new RegionNode(pjSite, upperPjVal));
                beach.insert(lowerPjVal, new RegionNode(pjSite, lowerPjVal));
                beach.insert(piVal, piNode);
                return beach.find(piVal);
            }

            // Case 2:  pi.x >= pj.x... verified 11/17.. prepare to test
            pjpiVal = getBreakPtX(pjSite, piSite, piSite.Y);
            pipjVal = pjpiVal + (_beachlineBoost / levelDivisor);
            upperPjVal = pjpiVal - (_beachlineBoost / levelDivisor);
            lowerPjVal = pipjVal + (_beachlineBoost / (levelDivisor + 1));

            piVal = (pipjVal + pjpiVal) / 2f;
            piNode = new RegionNode(piSite, piVal);

            // pj = internal pjpi;
            breakPt = new Vector2(pjpiVal, piSite.Y);
            pjpiHE.Origin = new Vertex(pjpiHE, breakPt); //left he
            pipjHE.Origin = voronoiDCEL.InfiniteVertex; //right he
            setHETwins(pjpiHE, pipjHE);
            regionDuo = new List<Vector2> { pjSite, piSite };
            pjArc.leafToInternal(regionDuo, pjpiHE);

            // pi.val = pjpiVal and insertion
            pjNode.key = pjpiVal;
            beach.insert(pipjVal, new RegionNode(piSite, pjSite, pipjHE, pipjVal)); // changed to be first 11/16			
            beach.insert(upperPjVal, new RegionNode(pjSite, upperPjVal));
            beach.insert(piVal, piNode);
            beach.insert(lowerPjVal, new RegionNode(pjSite, lowerPjVal));

            return beach.find(piVal);
        }


        /*------------------------------- CIRCLE EVENT METHODS -------------------------------*/

        /// <summary>
        /// private helper function for handleCircleEvent
        /// that sets the halfedge pointers appropriately
        /// left halfedges come from starting breakpt
        /// right halfedges end at the voronoi vertex
        /// </summary>
        private void setCircleHEPtrs(HalfEdge parentHE, HalfEdge upParentHE, HalfEdge newLeftHE, HalfEdge newRightHE)
        {
            // grab the left halfedges of each then we know their twins are the right halfedges
            HalfEdge parentLeftHE = (parentHE.Origin != newLeftHE.Origin) ? parentHE : parentHE.Twin;
            HalfEdge upParentLeftHE = (upParentHE.Origin != newLeftHE.Origin) ? upParentHE : parentHE.Twin;

            // determine leftmost left HE then we get the rest
            HalfEdge leftLeftHE = (parentLeftHE.Origin.Position.X <= upParentLeftHE.Origin.Position.X) ? parentLeftHE : upParentLeftHE;
            HalfEdge rightLeftHE = (parentLeftHE == leftLeftHE) ? upParentLeftHE : parentLeftHE;
            HalfEdge leftRightHE = leftLeftHE.Twin;
            HalfEdge rightRightHE = rightLeftHE.Twin;

            // set prevs and nexts so we define faces in CCW fashion
            leftLeftHE.Next = rightRightHE;
            rightRightHE.Prev = leftLeftHE;
            leftRightHE.Prev = newRightHE;
            newRightHE.Next = leftRightHE;
            newLeftHE.Prev = rightLeftHE;
            rightLeftHE.Next = newLeftHE;

            return;
        }

        /*------------------------------- HANDLE EVENT METHODS -------------------------------*/

        /// <summary>
        /// handle site event. Splits an arc in half and modifies the RBT
        /// </summary>
        /// <param name="sweepCoord"></param>
        /// <param name="eventQueue"></param>
        /// <param name="beach"></param>
        /// <param name="voronoiDCEL"></param>
        public void handleSiteEvent(float sweepCoord, MaxHeap<VoronoiEvent> eventQueue, RedBlackTree<RegionNode> beach, DCEL voronoiDCEL)
		{
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
			
			Vector2 piSite = eventQueue.peekTopOfObjHeap().eventSite;
            float piX = piSite.X;

            //first parabola if beachline hasn't been filled already
            if (beach.isEmpty())
			{
				beach.insert(piX, new RegionNode(piSite, piX));
				return;
			}
			
			RBNode<RegionNode> piNode = insertAndSplit(piSite, eventQueue, beach, voronoiDCEL);
			RBNode<RegionNode> piSucc = beach.getSucc(piNode);
			RBNode<RegionNode> piPred = beach.getPred(piNode);
			
			if(piPred != null)
			{
				RBNode<RegionNode> piPredPred = null!;
				piPredPred = beach.getPred(piPred);
				if(piPredPred != null)
					detectCircleEvent(piPredPred, piPred, piNode, sweepCoord, eventQueue);
			}
			
			if(piSucc != null)
			{
				RBNode<RegionNode> piSuccSucc = null!;
				piSuccSucc = beach.getSucc(piSucc);
				if(piSuccSucc != null)
					detectCircleEvent(piNode, piSucc, piSuccSucc, sweepCoord, eventQueue);				
			}
			return;
		}

		/* 
		 * handle valid circle event. 
		 * Draws a Voronoi Vertex and removes arc from beachline
		 */
		public void handleCircleEvent(VoronoiEvent circleEvent, MaxHeap<VoronoiEvent> eventQueue, RedBlackTree<RegionNode> beach, DCEL voronoiDCEL)
		{
            if ((voronoiDCEL == null) || (eventQueue == null) || (circleEvent == null) || (beach == null))
            {
                Console.WriteLine("Something's null in handleCircleEvent, stopping now");
                return;
            }

            if (!circleEvent.circleEventIsActive)
                return;

            float sweepCoord = circleEvent.weight;
            RBNode<RegionNode> squeezedNode = circleEvent.squeezedArcLeaf!;
            RegionNode squeezedArc = squeezedNode.obj;
            Vector2 squeezedFocus = squeezedArc.regionSites[0];

            // grab parent breakpt and grandparent breakpt
            RBNode<RegionNode> squeezedParent = squeezedNode.parent!;
			RBNode<RegionNode> squeezedGrandParent = null!;
			
			if(squeezedParent != null)
				squeezedGrandParent = squeezedParent.parent;
			
            // initialize neighboring arcs
            RBNode<RegionNode> squeezedSucc = beach.getSucc(squeezedNode)!;
            RBNode<RegionNode> squeezedPred = beach.getPred(squeezedNode)!;
            RBNode<RegionNode> squeezedSuccSucc = null!;
            RBNode<RegionNode> squeezedPredPred = null!;
            RBNode<RegionNode> queryParentNode = null!;
			HalfEdge parentInternalHE = null!;
			HalfEdge higherInternalHE = null!;
            Vector2 circleCenter = circleEvent.circleEventCenter;
            Vector2 updatedSite = default;

			
			// 2. add circle center to vertices and bind 2 halfEdges to it. IN PROGRESS 11/12
            HalfEdge fromCircleHE = new();
            HalfEdge toCircleHE = new();
            Vertex fromCircleVert = new(fromCircleHE, circleCenter);
            // set toCircle's next to be edge that doesn't have circleCenter as origin.
            toCircleHE.Origin = voronoiDCEL.InfiniteVertex;
            // set fromCircle's prev to be edge that doesn't have circleCenter as origin.
            fromCircleHE.Origin = fromCircleVert;
            setHETwins(fromCircleHE, toCircleHE);
            voronoiDCEL.Add(toCircleHE, fromCircleHE);
            voronoiDCEL.Add(fromCircleVert);

            // update all breakpts to no longer have squeezed
            // the triple of consecutive sites
            // pi,pj,pk on the sweep-line status is replaced with pi,pk

			// 2. update the parent, DO NOT DELETE.            
            if (squeezedParent != null)
            {
                updatedSite = (squeezedNode == squeezedParent.right) ? squeezedPred.obj.regionSites[0] : squeezedSucc.obj.regionSites[1];
                squeezedParent.obj.updateInternal(squeezedFocus, updatedSite, circleCenter, voronoiDCEL);
				parentInternalHE = squeezedParent.obj.dcelEdge;
				squeezedParent.obj.dcelEdge = toCircleHE;
				
                queryParentNode = (squeezedNode == squeezedParent.right) ? squeezedSucc.parent : squeezedPred.parent;
            }
			
			// we check for another internal node further up the tree that contains the squeezed site
            if (queryParentNode != null)
            {
                //2. update higher up internal node
                while ( !(queryParentNode.obj.regionSites.Contains(squeezedFocus)) && queryParentNode != null)
					queryParentNode = queryParentNode.parent;
				
				if(queryParentNode != null) 
				{
					queryParentNode.obj.updateInternal(squeezedFocus, updatedSite, circleCenter, voronoiDCEL);
                    higherInternalHE = queryParentNode.obj.dcelEdge;
					queryParentNode.obj.dcelEdge = fromCircleHE;
                    setCircleHEPtrs(parentInternalHE, higherInternalHE, fromCircleHE, toCircleHE);
				}				
            }

            // 1. disable all circle events involving squeezed
            // and get nodes for future circle events
            squeezedArc.leafCircleEvent.disableCircleEvent();
            if (squeezedSucc != null)
            {
                squeezedSucc.obj.leafCircleEvent.disableCircleEvent();
                squeezedSuccSucc = beach.getSucc(squeezedSucc)!;
            }
            if (squeezedPred != null)
            {
                squeezedPred.obj.leafCircleEvent.disableCircleEvent();
                squeezedPredPred = beach.getPred(squeezedPred)!;
            }

            // update the higher-up internal node with pj to represent pipk...
            // this should be an adjacent arc's parent != removed arc's parent, aka a grand(x times) parent

            // 1. delete squeezedArc a and its parent
            beach.delete(squeezedArc.weight, squeezedArc);

            // add 3 new records to half-edge records that end at the circle center...i.e. the end vertex for two half-edges
            // the prev, and the next...? 11/4

            // 3. detect future circle events involving new neighbors
            if((squeezedPred != null) && (squeezedSucc != null) &&  (squeezedSuccSucc != null))
                detectCircleEvent(squeezedPred, squeezedSucc, squeezedSuccSucc, sweepCoord, eventQueue);
            if ((squeezedPredPred != null) && (squeezedPred != null) &&  (squeezedSucc != null))
                detectCircleEvent(squeezedPredPred, squeezedPred, squeezedSucc, sweepCoord, eventQueue);

            /*
             1. Delete leaf y that reps disappearing arc a from T. Update tuples repping breakpoints/edges at
			 Internal nodes. Rebalance as needed... Delete all circle events involving a from Q via using ptrs
			 from predecessor and successor of y in T (circle event w a as middle is being handled in this method)
            */

            /*2. Add center of circle causing the event as a vertex record to the DCEL d storing the VD. Make 2 half-edges
				corresponding to new breakpt of the beachline. Set ptrs between them appropriately. Attach 3 new records
				to half-edge records that end at the vertex.*/

            /*3. Check new triples of consecutive arcs that has former left neighbor of a as its middle to see if 2 new breakpoints/edges
				of the triple converge. If so, insert corresponding circle event into Q, and set ptrs between new circle event and corresponding
				leaf of T. Do same for triple where former right neighbor is middle arc....
			*/
            return;
		}


        /*------------------------------- Main Algo Prep, Loop, and Finish -------------------------------*/

        /// <summary>
        /// once we've gone through Fortune's algo, we clean up intermediate state
        /// </summary>
        /// <param name="voronoiDCEL"> the unbounded DCEL that represents
        /// the Voronoi Diagram </param>
        /// <param name="beach"> the beachline that may still have infinite edges 
        /// that must be clipped </param>
        /// <param name="bbox"> the Bounding Box for the voronoi diagram that we
        /// will clip the infinite edges remaining in beach to </param>
        /// <returns></returns>
        public DCEL clipVoronoiDiagram(DCEL voronoiDCEL, RedBlackTree<RegionNode> beach, BBox bbox)
        {
            // TODO 11/18...
            // get this working once we can confirm the algo runs correctly
            // extend infiinite halfedges and see which segments of the bounding box they intersect
            // set the infinite vertices to their intersection pt with the bounding box

            // foreach remaining halfedge (acquired by inorder traversal and taking internal nodes HEs):
                // find line repped by the edge
                // find all intersections with bbox edges
                // take closest intersection point that lies on the halfedge
            return voronoiDCEL;
        }

        /// <summary>
        /// algorithm that executes Fortune's algo for Voronoi diagrams
        /// via using sweepline and beachline of parabolas 
        /// </summary>
        /// <param name="eventQueue"></param>
        /// <param name="beach"></param>
        /// <returns></returns>
        // @returns a built DCEL
        public DCEL fortuneAlgo(MaxHeap<VoronoiEvent> eventQueue, RedBlackTree<RegionNode> beach)
        {
			DCEL voronoiDCEL = new();
            float eventY;
            VoronoiEvent currEvent;
            //Dcel<LineSegment, Vector2> voronoiDCEL = new Dcel<LineSegment, Vector2>();
            //1.Fill the event queue with site events for each input site.
            //2.While the event queue still has items in it:
            //    If the next event on the queue is a site event:
            //        Add the new site to the beachline
            //    Otherwise it must be an edge-intersection event:
            //        Remove the squeezed cell from the beachline
            while (!eventQueue.heapEmpty()) 
            {
                eventY = (float) eventQueue.peekTopOfHeap();
                currEvent = eventQueue.peekTopOfObjHeap();
                Debug.Assert(currEvent.eventSite.Y == eventY);

                if (currEvent.isSiteEvent())
                    handleSiteEvent(eventY, eventQueue, beach, voronoiDCEL);
                else if(!currEvent.isSiteEvent() && currEvent.circleEventIsActive)
                    handleCircleEvent(currEvent, eventQueue, beach, voronoiDCEL);

                eventQueue.extractHeadOfHeap();
            }
            //TODO
            //3.Cleanup any remaining intermediate state via bounding box clipping
            clipVoronoiDiagram(voronoiDCEL, beach, this._bbox);
            return voronoiDCEL;
        }

        // initialize eventQueueLine
		// sort by events maximal Y then by X
        public MaxHeap<VoronoiEvent> initEventQueue()
        {
            List<Vector2> orderedSites = new List<Vector2>(this._sites);
            List<VoronoiEvent> events = new List<VoronoiEvent>();
            // sort by minimal y then by x
            orderedSites.Sort((s1, s2) =>
            {
                int ret = s1.Y.CompareTo(s2.Y);
                return ret != 0 ? ret : s1.X.CompareTo(s2.X);
            });

            // reverse to be maximal y
            orderedSites.Reverse();
            VoronoiEvent ev = null!;
            MaxHeap<VoronoiEvent> pq = new MaxHeap<VoronoiEvent>();
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
            // handle edge cases where several starting
            // points are within a certain range TODO 11/12
            return new RedBlackTree<RegionNode>();
        }

        //main algorithm for this class
        public void fortuneMain()
        {
            DCEL vorDiagram = null!;
            //test inits
            MaxHeap<VoronoiEvent> eventQueue = initEventQueue();
            RedBlackTree<RegionNode> beach = initBeachSO();
            // 11/18...TO TEST
            // test insert and split
            // test
            //vorDiagram = fortuneAlgo(eventQueue, beach);
            return;
        }
    }
}

/* used while trying to figure out how to get break pt using circle with sites pi and pj and touching sweep
* https://www.emathzone.com/tutorials/geometry/equation-of-a-circle-given-two-points-and-tangent-line.html - 2 methods
* https://www.youtube.com/watch?v=nRAT0cyp74o -- via distance. These are great if the line has 1 perpendicular line...
* https://www.youtube.com/watch?v=DsaYcD_Ab9I&t=194s -- circle touches a line. Doesn't work for sweepline
 */