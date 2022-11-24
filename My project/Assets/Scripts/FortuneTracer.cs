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
https://www.boost.org/doc/libs/1_80_0/boost/polygon/voronoi_builder.hpp
https://www.emathhelp.net/calculators/algebra-2/parabola-calculator/ -- parabola resources
parabola calculator: https://www.omnicalculator.com/math/parabola
geogebra intersection tool: https://www.geogebra.org/m/bduwwjqn
circumcenter calculator: https://www.omnicalculator.com/math/circumcenter-of-a-triangle
https://www.boost.org/doc/libs/1_80_0/boost/polygon/voronoi_builder.hpp
https://www.geeksforgeeks.org/find-height-binary-tree-represented-parent-array/ get height of BST Node of element in sorted array in O(n) time
helpful for sorted dictionary approach
https://github.com/pvigier/FortuneAlgorithm
*/

/// TODO:
/// 1. How to clip the diagram in a large bounding box. 
/// 2. How to detect intersection with each face and the ice rink box
// MAJOR DESIGN STUFF:
// 1. Switch from RedBlackTree to SortedDictionary for beachline if needed


using FortuneAlgo;
using System;
using System.Collections;
using System.Collections.Generic; // sorted dictionary for beachline
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;


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

        public static float _toleranceThreshold = 1e-3f;
		private const float _beachlineBoost = 10e6f;
        private const float _convergeDivisor = 10f;
        private int _siteCount;
        private List<Vector2> _sites;
        private MaxHeap<VoronoiEvent> _pq;
        // we use only site events in the beachline per Dave Mount's Lecture Notes
        private RedBlackTree<float> _beachLine;
        //given as lowerLeft and upperRight corners
        //of 2d face of VoronoiObj in Unity. Bounds Voronoi Diagram
        private BBox _bbox;
        public VoronoiDiagram _vd;


        /*------------------------------- CONSTRUCTORS -------------------------------*/
        public FortuneTracer(List<Vector2> sites, BBox bbox)
        {
            this._sites = sites;
            this._siteCount = _sites.Count;
            this._bbox =  bbox;
            this._pq = null!;
            this._beachLine = null!;
            this._vd = null!;
        }

        public FortuneTracer()
        {
            this._sites = null!;
            this._siteCount = 0;
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

        /* set two half edges as each other's next and prev
         * he1's next = he2
         * he2's prev = he1
         */
        private void setHENextPrev(HalfEdge he1, HalfEdge he2)
        {
            he1.Next = he2;
            he2.Prev = he1;
        }

        /// <summary>
        /// Makes an edge comprised of two halfedges whose twins are known
        /// </summary>
        /// <param name="pos"> the origin of the left halfedge and the target of the right halfedge</param>
        /// <param name="voronoiDCEL"></param>
        /// <returns> The left halfedge whose origin is the given pos</returns>
        private HalfEdge makeEdge(Vector2 pos, DCEL voronoiDCEL)
        {
            Vertex posVertex = new Vertex(pos);
            HalfEdge rightHE = new(voronoiDCEL.InfiniteVertex); //right halfedge
            HalfEdge leftHE = new(posVertex); //left halfedge
            setHETwins(leftHE, rightHE);
            voronoiDCEL.Add(leftHE, rightHE);
            posVertex.Leaving = leftHE;
            voronoiDCEL.Add(posVertex);

            return leftHE;
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
            float ypTerm2 = (site.Y + sweepCoord) / 2;
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

            //if (s1.X < x1 && x1 < s2.X)
            //    return x1;

            //return x2;
            if (s1.Y < s2.Y)
                return Math.Max(x1, x2);

            return Math.Min(x1, x2);
        }

        /// <summary>
        /// get the predecessor leaf of a given leaf node
        /// </summary>
        /// <param name="start"></param>
        /// <param name="beach"></param>
        /// <returns></returns>
        private RBNode<RegionNode> getLeafPred(RBNode<RegionNode> start, RedBlackTree<RegionNode> beach) 
        {
            // ensure we are using a leaf node.
            if (start == null || start.obj.isInternalNode() || !beach.isLeaf(start) )
            {
                Console.WriteLine($"Something wrong in getLeafPred w {start}");
                beach._printRBT();
                return null!;
            }

            RBNode<RegionNode> leafPred = null!;
            RBNode<RegionNode> travel = start;
            do
            {
                leafPred = beach.getPred(travel);
                travel = leafPred;
            } while (leafPred != null && leafPred.obj.isInternalNode());
            return leafPred!;
        }

        /// <summary>
        /// get the successor leaf of a given leaf node
        /// </summary>
        /// <param name="start"></param>
        /// <param name="beach"></param>
        /// <returns></returns>
        private RBNode<RegionNode> getLeafSucc(RBNode<RegionNode> start, RedBlackTree<RegionNode> beach)
        {
            // ensure we are using a leaf node.
            if (start == null || start.obj.isInternalNode() || !beach.isLeaf(start))
            {
                Console.WriteLine($"Something wrong in getLeafSucc w {start}");
                beach._printRBT();
                return null!;
            }

            RBNode<RegionNode> leafSucc = null!;
            RBNode<RegionNode> travel = start;
            do
            {
                leafSucc = beach.getSucc(travel);
                travel = leafSucc;
            } while (leafSucc != null && leafSucc.obj.isInternalNode());
            return leafSucc!;
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
         * right determined from start's perspective....Returns true if it's on the line as well.
         *  https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
         */
        public bool isRightOfLine(Vector2 start, Vector2 end, Vector2 point)
        {
            return ((end.X - start.X) * (point.Y - start.Y) - (end.Y - start.Y) * (point.X - start.X)) <= 0;
        }

        /*
		* helper for detectCircleEvent
		* check if two breakpoints of a consecutive triple converge....
		* this means the bisectors that define them move in opposite directions and never intersect at the circumcenter
        * breakpts converge if the center of the circle
        * defined by the 3 sites is "in front" of the middle site
        * check if circleCenter is right of the lines formed from the left and middle sites
        * and the middle and right sites
		* https://stackoverflow.com/questions/9612065/breakpoint-convergence-in-fortunes-algorithm
		*/
        private bool doesTripleConverge(Vector2 circCenter, Vector2 left, Vector2 mid, Vector2 right)
        {
            return isRightOfLine(left, mid, circCenter) && isRightOfLine(mid, right, circCenter);
        }

        /*
		* determine if we have a circle event between 3 sites. 
		* Get Distance between circumcenter and site then find min y value of circle.
		* If it's below sweepline and the left and right breakpts converge, we've a circle event to add to the queue
        */
        public bool detectCircleEvent(RBNode<RegionNode> left, RBNode<RegionNode> mid, RBNode<RegionNode> right, 
            float sweepCoord, MaxHeap<VoronoiEvent> eventQueue, DCEL voronoiDCEL)
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
            {
                Console.WriteLine($"Computed distances in circleEvent don't check out for {s1} {s2} {s3}");
                return false;
            }

            // SWEEP-AXIS-MOVE-DEPENDENT
            float circBottomY = circCenter.Y - dist1;
            if (circBottomY >= sweepCoord)
			{
				Console.WriteLine($"Computed cc {circCenter} for {s1} {s2} {s3} above y-Coord {sweepCoord}");
                return false;
			}

            RegionNode leftBp = left.parent.obj;
            RegionNode rightBp = right.parent.obj;
            // if these don't converge by either method, we don't want them...
            if (!doesTripleConverge(circCenter, s1, s2, s3) &&
                !checkIfCloserInFuture(circCenter, leftBp, rightBp, sweepCoord, dist1))
                return false;

            List<RBNode<RegionNode>> sortedLeaves = new List<RBNode<RegionNode>> { left, mid, right };

            Console.WriteLine($"Triple:{s1},{s2},{s3} converges! Adding cc {circCenter} to eventQueue");

            Vertex check = new(circCenter);
            if (voronoiDCEL.Vertices.Contains(check))
            {
                Console.WriteLine($"Triple:{s1},{s2},{s3} converges! but {circCenter} already exists in DCEL verts!");
                return false;
            }
            //add circle event to EventQueue... give a reference to the squeezedArcNode
            VoronoiEvent circEvent = new VoronoiEvent(circCenter, circBottomY, mid, circCenter);
            eventQueue.InsertElementInHeap(circBottomY, circEvent);

            // set reference to circEvent in the middleNode that will get squeezed out
            mid.obj.leafCircleEvent = circEvent;
            return true;
        }

        /*------------------------------- SITE EVENT METHODS -------------------------------*/
        /// <summary>
        /// private helper used by insertAndSplit 
		/// traverse the beachline represented by the RedBlackTree
        /// using getBreakPt at internal nodes....
        ///  go left if new site pi < breakPtX at an internal node
        ///  stop when we reach a leaf node.
        /// </summary>
        /// <param name="piSite"></param>
        /// <param name="beach"></param>
        /// <returns></returns>
        private RBNode<RegionNode> findArcAboveSite(Vector2 piSite, RedBlackTree<RegionNode> beach)
        {
            RBNode<RegionNode> parent = null!;
            RBNode<RegionNode> arcNode = beach.getRoot();

            float bpX = 0.0f;
            List<Vector2> bpSites = null!;
            Console.WriteLine($"Finding arc above site {piSite}");

            //travel to a leafNode representing a lone arc
            while (!beach.isLeaf(arcNode))
            {
                parent = arcNode;
                bpSites = arcNode.obj.regionSites;
                if (bpSites.Count != 2)
                    Console.WriteLine($"Supposed internal arc has {bpSites.Count} instead of 2 arcs.");

                bpX = getBreakPtX(bpSites[0], bpSites[1], piSite.Y);
                if (piSite.X < bpX)
                {
                    arcNode = arcNode.left;
                    Console.WriteLine("Left");
                }
                else
                {
                    arcNode = arcNode.right;
                    Console.WriteLine("Right");
                }
            }

            return arcNode;
        }

        /*
		* splits an arc in twain as described in handleSiteEvent.
		* only called when there is at least one arc in the beachline since we get at most 2n-1 arcs...
		*/
        public RBNode<RegionNode> insertAndSplit(Vector2 piSite,RedBlackTree<RegionNode> beach,
            DCEL voronoiDCEL, RBNode<RegionNode> targetNode = null!)
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
            float breakPtY;

            //we can use this divisor to allow us to exploit an infinite range
            float levelFactor = 1f;
            RBNode<RegionNode> temp = pjNode;
            
            while(temp.parent != null) 
            {
                temp = temp.parent;
                levelFactor += 1;
            }

            float boostVal1 = (_beachlineBoost) / ((float) Math.Pow(_convergeDivisor,levelFactor));
            float boostVal2 = (boostVal1) / _convergeDivisor;
            // inserting left->right per level seems to minimize rotation...10/29
            // however, the longevity of this insertion pattern is dubious..
            // Try average as values for inner leaf nodes.
            // 11/16 -- INSERT THE BREAKPT NODE FIRST so it can become an internal rather than a leaf...
            // ... verified 11/17.. prepare to test...

            if (piSite.X < pjSite.X)
            {
                // fixed breakPtX.... values for the beachline are still finnicky. TODO 11/22
                Console.WriteLine("Splitting toward left");
                pipjVal = getBreakPtX(piSite, pjSite, piSite.Y);
                pipjVal = pjNode.key;
                pjpiVal = pipjVal - (boostVal1);
                upperPjVal = pipjVal + (boostVal1);
                lowerPjVal = pjpiVal - (boostVal2);

                piVal = (pipjVal + pjpiVal) / 2f;
                piNode = new RegionNode(piSite, piVal);

                if (piVal == pipjVal || piVal == pjpiVal)
                    Console.WriteLine("Pi value is equal to pipj value in insertAndSplit.");

                //pj = internal pipj;
                breakPtY = yOnParabFromSiteAndX(pjSite, piSite.Y, piSite.X);
                breakPt = new Vector2(piSite.X, breakPtY);

                pipjHE = makeEdge(breakPt, voronoiDCEL); //right edge
                pjpiHE = makeEdge(breakPt, voronoiDCEL); // left edge

                setHENextPrev(pipjHE.Twin, pjpiHE);
                setHENextPrev(pjpiHE.Twin, pipjHE);

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
            Console.WriteLine("Splitting toward right");

            // Case 2:  pi.x >= pj.x... verified 11/17.. prepare to test
            pjpiVal = getBreakPtX(pjSite, piSite, piSite.Y);
            pjpiVal = pjNode.key;
            pipjVal = pjpiVal + (boostVal1);
            upperPjVal = pjpiVal - (boostVal1);
            lowerPjVal = pipjVal + (boostVal2);

            piVal = (pipjVal + pjpiVal) / 2f;
            piNode = new RegionNode(piSite, piVal);

            if (piVal == pipjVal || piVal == pjpiVal)
                Console.WriteLine("Pi value is equal to pipj value in insertAndSplit.");

            // pj = internal pjpi;
            breakPtY = yOnParabFromSiteAndX(pjSite, piSite.Y, piSite.X);
            breakPt = new Vector2(piSite.X, breakPtY);

            pjpiHE = makeEdge(breakPt, voronoiDCEL); //left edge
            pipjHE = makeEdge(breakPt, voronoiDCEL); //right edge

            setHENextPrev(pipjHE.Twin, pjpiHE);
            setHENextPrev(pjpiHE.Twin, pipjHE);

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
            setHENextPrev(leftLeftHE,rightRightHE);
            setHENextPrev(newRightHE,leftRightHE);
            setHENextPrev(rightLeftHE,newLeftHE);

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
			Vector2 piSite = eventQueue.peekTopOfObjHeap().eventSite;
            float piX = piSite.X;

            //first parabola if beachline hasn't been filled already
            if (beach.isEmpty())
			{
				beach.insert(piX, new RegionNode(piSite, piX));
				return;
			}
			
			RBNode<RegionNode> piNode = insertAndSplit(piSite, beach, voronoiDCEL);
			RBNode<RegionNode> piSucc = getLeafSucc(piNode, beach);
			RBNode<RegionNode> piPred = getLeafPred(piNode, beach);
			
			if(piPred != null)
			{
				RBNode<RegionNode> piPredPred = null!;
				piPredPred = getLeafPred(piPred, beach); 
				if(piPredPred != null)
					detectCircleEvent(piPredPred, piPred, piNode, sweepCoord, eventQueue, voronoiDCEL);
			}
			
			if(piSucc != null)
			{
				RBNode<RegionNode> piSuccSucc = null!;
				piSuccSucc = getLeafSucc(piSucc, beach);
				if(piSuccSucc != null)
					detectCircleEvent(piNode, piSucc, piSuccSucc, sweepCoord, eventQueue, voronoiDCEL);				
			}
			return;
		}

        /// <summary>
        /// handle valid circle event. 
        /// Draws a Voronoi Vertex and removes arc from beachline
        /// </summary>
        /// <param name="circleEvent"></param>
        /// <param name="eventQueue"></param>
        /// <param name="beach"></param>
        /// <param name="voronoiDCEL"></param>
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
            RBNode<RegionNode> squeezedSucc = getLeafSucc(squeezedNode, beach)!;
            RBNode<RegionNode> squeezedPred = getLeafPred(squeezedNode, beach)!;
            RBNode<RegionNode> squeezedSuccSucc = null!;
            RBNode<RegionNode> squeezedPredPred = null!;
            RBNode<RegionNode> queryParentNode = null!;
			HalfEdge parentInternalHE = null!;
			HalfEdge higherInternalHE = null!;
            Vector2 circleCenter = circleEvent.circleEventCenter;
            Vector2 updatedSite = default;
            int updatedIdx = -1;
			
			// 2. add circle center to vertices and bind 2 halfEdges to it.
            HalfEdge fromCircleHE = makeEdge(circleCenter, voronoiDCEL);

            // 2. update the parent, DO NOT DELETE.
            // update all breakpts to no longer have squeezed
            // pi,pj,pk on the sweep-line status is replaced with pi,pk
            if (squeezedParent != null)
            {
                updatedSite = (squeezedNode == squeezedParent.right) ? squeezedSucc.obj.regionSites[0] : squeezedPred.obj.regionSites[0];
                squeezedParent.obj.updateInternal(squeezedFocus, updatedSite, circleCenter, voronoiDCEL);
				parentInternalHE = squeezedParent.obj.dcelEdge;
				squeezedParent.obj.dcelEdge = fromCircleHE;

                updatedSite = (squeezedNode == squeezedParent.right) ? squeezedPred.obj.regionSites[0] : squeezedSucc.obj.regionSites[0];
                queryParentNode = (squeezedNode == squeezedParent.right) ? squeezedSucc.parent : squeezedPred.parent;
                updatedIdx = (squeezedNode == squeezedParent.right) ? 0 : 1;				
            }
			
			// we check for another internal node further up the tree that contains the squeezed site
            if (queryParentNode != null)
            {
                //2. update higher up internal node
                // obj.regionSites.Contains(squeezedFocus)
                while ( !(queryParentNode.obj.regionSites[updatedIdx] == squeezedFocus) && queryParentNode != null)
					queryParentNode = queryParentNode.parent;
				
				if(queryParentNode != null) 
				{
					queryParentNode.obj.updateInternal(squeezedFocus, updatedSite, circleCenter, voronoiDCEL);
                    higherInternalHE = queryParentNode.obj.dcelEdge;
					queryParentNode.obj.dcelEdge = fromCircleHE;
                    setCircleHEPtrs(parentInternalHE, higherInternalHE, fromCircleHE, fromCircleHE.Twin);
				}				
            }

            // 1. disable all circle events involving squeezed
            // and get nodes for future circle events
            squeezedArc.leafDisableCircleEvent();
            if (squeezedSucc != null)
            {
                squeezedSucc.obj.leafDisableCircleEvent();
                squeezedSuccSucc = getLeafSucc(squeezedSucc, beach)!;
            }
            if (squeezedPred != null)
            {
                squeezedPred.obj.leafDisableCircleEvent();
                squeezedPredPred = getLeafPred(squeezedPred, beach)!;
            }

            // 1. delete squeezedArc a and its parent
            beach.delete(squeezedArc.weight, squeezedArc);

            // 3. detect future circle events involving new neighbors
            if((squeezedPred != null) && (squeezedSucc != null) &&  (squeezedSuccSucc != null))
                detectCircleEvent(squeezedPred, squeezedSucc, squeezedSuccSucc, sweepCoord, eventQueue, voronoiDCEL);
            if ((squeezedPredPred != null) && (squeezedPred != null) &&  (squeezedSucc != null))
                detectCircleEvent(squeezedPredPred, squeezedPred, squeezedSucc, sweepCoord, eventQueue, voronoiDCEL);

            return;
		}


        /*------------------------------- Main Algo Prep, Loop, and Finish -------------------------------*/
        
        private void printFortuneInfo(MaxHeap<VoronoiEvent> eventQueue, RedBlackTree<RegionNode> beach, int iters)
        {
            Console.WriteLine();
            beach._printRBT(true);
            Console.WriteLine($"\nIter {iters}. Press anything to continue\n\n");
            Console.ReadLine();
        }

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
            // TODO 11/23!!!...
            /*
            Make sure that every vertex of the diagram is contained inside the box.
            Clip every infinite edge.
            Close the cells.
            */
            BBox diagramBox = new();
            diagramBox.setExtentsGivenDCEL(voronoiDCEL);
            List<RegionNode> unBoundedBps = beach.inOrderGrabInternals(beach.root);

            Console.WriteLine("Final RBT:\n");
            beach._printRBT(true);

            return voronoiDCEL;
        }

        /// <summary>
        /// algorithm that executes Fortune's algo for Voronoi diagrams
        /// via using sweepline and beachline of parabolas 
        ///
        /// </summary>
        /// <param name="eventQueue"></param>
        /// <param name="beach"></param>
        /// <returns>a built DCEL</returns>
        public DCEL fortuneAlgo(MaxHeap<VoronoiEvent> eventQueue, RedBlackTree<RegionNode> beach, bool debug = false)
        {
			DCEL voronoiDCEL = new();
            float eventY;
            VoronoiEvent currEvent;
            int iters = 1;

            while (!eventQueue.heapEmpty()) 
            {
                if (debug)
                    printFortuneInfo(eventQueue, beach, iters);

                eventY = (float) eventQueue.peekTopOfHeap();
                currEvent = eventQueue.peekTopOfObjHeap();

                if (currEvent.isSiteEvent())
                    handleSiteEvent(eventY, eventQueue, beach, voronoiDCEL);
                else if(!currEvent.isSiteEvent() && currEvent.circleEventIsActive)
                    handleCircleEvent(currEvent, eventQueue, beach, voronoiDCEL);

                eventQueue.extractHeadOfHeap();
                iters++;
            }
            //TODO
            //3.Cleanup any remaining intermediate state via bounding box clipping
            clipVoronoiDiagram(voronoiDCEL, beach, this._bbox);
            return voronoiDCEL;
        }

        // initialize eventQueueLine
		// sort by events maximal Y then by X
        public MaxHeap<VoronoiEvent> initEventQueue(List<Vector2> sites)
        {
            List<Vector2> orderedSites = new List<Vector2>(sites);
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
            List<Vector2> sites1 = new List<Vector2> { new Vector2(358, 168), new Vector2(464, 389), new Vector2(758, 590), new(682, 254) };
            List<Vector2> sites2 = new List<Vector2> { new Vector2(401,320), new Vector2(617,315)};
            List<Vector2> sites3 = new List<Vector2> { new Vector2(401, 320), new Vector2(617, 315), new(510,162) };
            List<Vector2> sites4 = new List<Vector2> { new Vector2(401, 320), new Vector2(617, 315), new(509, 474), new Vector2(510,162) };
            List<Vector2> sites = new List<Vector2> { new Vector2(37, 645), new Vector2(367,435), new Vector2(562,297), new(785, 103) };
            MaxHeap<VoronoiEvent> eventQueue = initEventQueue(sites4);
            RedBlackTree<RegionNode> beach = initBeachSO();

            // 11/18...TO TEST
            vorDiagram = fortuneAlgo(eventQueue, beach, false);
            return;
        }
    }
}