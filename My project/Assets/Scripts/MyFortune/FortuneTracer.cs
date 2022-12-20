/* filename: FortuneTracer.cs
 author: Sam Ford (stf8464)
 desc: class that represents Fortune's Algorithm for
        tracing Vornoi diagrams using a sweepline method.
        Sweepline sweeps downward from max y to min y
 due: Thursday 11/29
https://math.stackexchange.com/questions/2700033/explanation-of-method-for-finding-the-intersection-of-two-parabolas
https://blog.ivank.net/fortunes-algorithm-and-implementation.html
https://jacquesheunis.com/post/fortunes-algorithm/
https://stackoverflow.com/questions/16695440/boost-intrusive-binary-search-trees/18264705
https://www.boost.org/doc/libs/1_80_0/boost/polygon/voronoi_builder.hpp
https://www.boost.org/doc/libs/1_80_0/boost/polygon/voronoi_builder.hpp
helpful for sorted dictionary approach
https://github.com/pvigier/FortuneAlgorithm
https://github.com/Zalgo2462/VoronoiLib/tree/6e468f60e2129fff8201ffd0b0b9f4777e2892aa/VoronoiLib
*/

/// TODO:
/// 1. How to detect intersection with each face and the ice rink box
/// 2. Figure out insertion method or rethink approach. Still giving issues with keys.
// MAJOR DESIGN STUFF:
// 1. Switch from RedBlackTree to SortedDictionary for beachline if needed


using FortuneAlgo;
using System;
using System.Collections;
using System.Collections.Generic; // sorted dictionary for beachline
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
        /// Makes a dangling edge comprised of two halfedges with one infinite endpoint
        /// </summary>
        /// <param name="pos"> the origin of the left halfedge and the target of the right halfedge</param>
        /// <param name="voronoiDCEL"></param>
        /// <returns> The left halfedge whose origin is the given pos</returns>
        private HalfEdge makeDanglingEdge(Vector2 pos, DCEL voronoiDCEL)
        {
            Vertex posVertex = new Vertex(pos);
            HalfEdge rightHE = new(DCEL.INFINITY); //right halfedge
            HalfEdge leftHE = new(posVertex); //left halfedge
            setHETwins(leftHE, rightHE);
            voronoiDCEL.Add(leftHE, rightHE);
            posVertex.Leaving = leftHE;
            voronoiDCEL.Add(posVertex);

            return leftHE;
        }

        /// <summary>
        /// Makes an edge comprised of two halfedges whose endpoints are known
        /// </summary>
        /// <param name="originPos"> the origin of the left halfedge and the target of the right halfedge</param>
        /// <param name="voronoiDCEL"></param>
        /// <returns> The left halfedge whose origin is the given pos</returns>
        private HalfEdge makeEdge(Vector2 originPos, Vector2 targetPos, DCEL voronoiDCEL)
        {
            if (voronoiDCEL != null && voronoiDCEL.Vertices != null && voronoiDCEL.Vertices.Count >= 2)
            {
                Vertex posVertex = voronoiDCEL.Vertices.FirstOrDefault(v => v.Position == originPos)!;
                Vertex targetVertex = voronoiDCEL.Vertices.FirstOrDefault(v => v.Position == targetPos)!;

                if (posVertex != null! && targetVertex != null!)
                {
                    HalfEdge rightHE = new(targetVertex); //right halfedge
                    HalfEdge leftHE = new(posVertex); //left halfedge
                    setHETwins(leftHE, rightHE);
                    voronoiDCEL.Add(leftHE, rightHE);
                    posVertex.Leaving = leftHE;
                    targetVertex.Leaving = rightHE;

                    return leftHE;
                }
            }
            return null!;

        }

        /*------------------------------- FORTUNE ALGO COMMON METHODS -------------------------------*/

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
                beach._printRBT(true);
                Console.WriteLine();
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
                beach._printRBT(true);
                Console.WriteLine();
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
            Vector2 circCenter = FortuneMath.getCircumCenter(s1, s2, s3);

            float dist1 = FortuneMath.computeEuclidDist(s1, circCenter);
            float dist2 = FortuneMath.computeEuclidDist(s2, circCenter);
            float dist3 = FortuneMath.computeEuclidDist(s3, circCenter);

            float diff12 = dist1 - dist2;
            float diff13 = dist1 - dist3;
            float diff23 = dist2 - dist3;

            if (!(FortuneMath.aroundZero(diff12)) || !(FortuneMath.aroundZero(diff13)) || !(FortuneMath.aroundZero(diff23)))
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
            if (!FortuneMath.doesTripleConverge(circCenter, s1, s2, s3) &&
                !FortuneMath.checkIfCloserInFuture(circCenter, leftBp, rightBp, sweepCoord, dist1))
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

                // bpX = getBreakPtX(bpSites[1], bpSites[0], piSite.Y); same as below
                bpX = FortuneMath.getBreakPtX(bpSites[0], bpSites[1], piSite.Y);
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

            RBNode<RegionNode> pjSuccNode = beach.getSucc(pjNode);
            RBNode<RegionNode> pjPredNode = beach.getSucc(pjNode);

            RegionNode piNode = null!;
            RegionNode pjArc = pjNode.obj;
            Vector2 pjSite = pjArc.regionSites[0];
            List<Vector2> regionDuo = null!;
            HalfEdge pipjHE = new();
            HalfEdge pjpiHE = new();

            float breakPtY = FortuneMath.yOnParabFromSiteAndX(pjSite, piSite.Y, piSite.X);
            Vector2 breakPt = new Vector2(piSite.X, breakPtY);

            float pjKeyVal = float.MaxValue;
            //if (pjPredNode != null && pjSuccNode != null) // attempt 2... try average of pred and succ if they exist 11/28
            //    pjKeyVal = (pjPredNode.key + pjSuccNode.key) / 2f;

            float pipjVal = 0.0f;
            float pjpiVal = 0.0f;
            float piVal = 0.0f;
            float upperPjVal = 0.0f;
            float lowerPjVal = 0.0f;
            //we can use this divisor to allow us to exploit an infinite range
            float levelFactor = 1f;
            RBNode<RegionNode> temp = pjNode;
            
            while(temp.parent != null) 
            {
                temp = temp.parent;
                levelFactor += 1;
            }

            float boostVal1 = (FortuneMath.KEYBOOST) / ((float) Math.Pow(FortuneMath.CONVERGEDIVISOR,levelFactor));
            float boostVal2 = (boostVal1) / FortuneMath.CONVERGEDIVISOR;
            // 11/16 -- INSERT THE BREAKPT NODE FIRST so it can become an internal rather than a leaf...
            // ... verified 11/17.. still buggy.. 11/27
            // averaging works for piVal, boosting doesn't work long term. Consider keys not the approach for this
            if (piSite.X < pjSite.X)
            {
                Console.WriteLine("Splitting toward left");
                pipjVal = FortuneMath.getBreakPtX(piSite, pjSite, piSite.Y);
                pipjVal = (pjKeyVal == float.MaxValue) ? pjNode.key : pjKeyVal; ;
                pjpiVal = pipjVal - (boostVal1);
                upperPjVal = pipjVal + (boostVal1);
                lowerPjVal = pjpiVal - (boostVal2);

                if (piVal == pipjVal || piVal == pjpiVal)
                    Console.WriteLine("Pi value is equal to pipj value in insertAndSplit.");

                //pj = internal pipj;
                pipjHE = makeDanglingEdge(breakPt, voronoiDCEL); //right edge
                pjpiHE = makeDanglingEdge(breakPt, voronoiDCEL); // left edge

                setHENextPrev(pipjHE.Twin, pjpiHE);
                setHENextPrev(pjpiHE.Twin, pipjHE);
                voronoiDCEL.doesHalfEdgeDefineSiteFace(pjSite, pipjHE);
                voronoiDCEL.doesHalfEdgeDefineSiteFace(piSite, pjpiHE);

                regionDuo = new List<Vector2> { piSite, pjSite };
                pjArc.leafToInternal(regionDuo, pipjHE);

                piVal = (pipjVal + pjpiVal) / 2f;
                piNode = new RegionNode(piSite, piVal);

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
            pjpiVal = FortuneMath.getBreakPtX(pjSite, piSite, piSite.Y);

            //try just doing average of pred and succ as new key val
            pjpiVal = (pjKeyVal == float.MaxValue) ? pjNode.key : pjKeyVal;
            pipjVal = pjpiVal + (boostVal1);
            upperPjVal = pjpiVal - (boostVal1);
            lowerPjVal = pipjVal + (boostVal2);

            if (piVal == pipjVal || piVal == pjpiVal)
                Console.WriteLine("Pi value is equal to pipj value in insertAndSplit.");

            // pj = internal pjpi;
            pjpiHE = makeDanglingEdge(breakPt, voronoiDCEL); //left edge
            pipjHE = makeDanglingEdge(breakPt, voronoiDCEL); //right edge

            setHENextPrev(pipjHE.Twin, pjpiHE);
            setHENextPrev(pjpiHE.Twin, pipjHE);
            voronoiDCEL.doesHalfEdgeDefineSiteFace(pjSite, pipjHE.Twin);
            voronoiDCEL.doesHalfEdgeDefineSiteFace(piSite, pjpiHE.Twin);

            regionDuo = new List<Vector2> { pjSite, piSite };
            pjArc.leafToInternal(regionDuo, pjpiHE);

            piVal = (pipjVal + pjpiVal) / 2f;
            piNode = new RegionNode(piSite, piVal);

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
        /// private helper function for handleCircleEvent SETS CW... 11/26!!!
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

            // set prevs and nexts so we define faces in CCW fashion.... actually CW - 11/26
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

            Console.WriteLine($"Handling valid CE {circleEvent}\n");
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
            HalfEdge fromCircleHE = makeDanglingEdge(circleCenter, voronoiDCEL);

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
                while (queryParentNode != null && !(queryParentNode.obj.regionSites[updatedIdx] == squeezedFocus)  )
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
            //if (squeezedParent != null)
            //    beach.delete(squeezedParent.key, squeezedParent.obj);

            // 3. detect future circle events involving new neighbors
            if ((squeezedPred != null) && (squeezedSucc != null) &&  (squeezedSuccSucc != null))
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
        /// given a list of points along the bounding box,
        /// make and link appropriate edges between them
        /// </summary>
        /// <param name="boundPts"></param>
        /// <param name="startHE"></param>
        /// <param name="endHE"></param>
        private void sewUpBoundaryPoints(List<Vector2> boundPts, HalfEdge startHE, HalfEdge endHE, DCEL voronoiDCEL)
        {
            // make edges betwen each boundary point
            // link with the one behind it
            List<HalfEdge> boundHEs = new();
            for(int i = 0; i < boundPts.Count; i++)
            {
                if (i < boundPts.Count - 1)
                {
                    HalfEdge newLeft = makeEdge(boundPts[i], boundPts[i + 1], voronoiDCEL);
                    Debug.Assert(newLeft != null);
                    boundHEs.Add(newLeft);
                }
            }
            boundHEs.Insert(0, endHE);
            boundHEs.Add(startHE);

            for(int i = 1; i < boundHEs.Count; i++)
            {
                setHENextPrev(boundHEs[i - 1], boundHEs[i]);
            }
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
        private DCEL clipVoronoiDiagram(DCEL voronoiDCEL, RedBlackTree<RegionNode> beach, float minEventY, BBox mapBox = null)
        {
            beach._printRBT(true);

            // Ensure every vertex of the diagram is contained inside the box.
            BBox diagramBox = new();
            diagramBox.setExtentsGivenDCEL(voronoiDCEL, this._sites, minEventY);

            // ensure we capture lowest circle event
            if (diagramBox.lowerLeft.Y > minEventY)
                diagramBox.lowerLeft.Y = minEventY;

            if (mapBox != null)
                diagramBox.doesBoxExpandBox(mapBox);

            List<RegionNode> unBoundedBps = beach.inOrderGrabInternals(beach.root);
            unBoundedBps.RemoveAll(x => !(x.isEdgeDangling()));
            float minY = diagramBox.lowerLeft.Y;

            //Clip every infinite edge.
            foreach (RegionNode edge in unBoundedBps)
            {
                float bpX = FortuneMath.getBreakPtX(edge.regionSites[0], edge.regionSites[1], minY);
                float bpY = FortuneMath.yOnParabFromSiteAndX(edge.regionSites[0], minY, bpX);
                float bpY2 = FortuneMath.yOnParabFromSiteAndX(edge.regionSites[1], minY, bpX);
                Vector2 bpVec = new(bpX, bpY);
                edge.fillInfiniteEndPt(bpVec);
            }

            // if we're an unbounded edge, then we use ray-intersection
            // to see where we first hit the bounding box.
            Console.WriteLine("Final RBT:\n");
            List<HalfEdge> unBoundedHEs = voronoiDCEL.getUnBoundedHalfEdges();
            List<Vertex> voronoiVerts = voronoiDCEL.Vertices.ToList();
            foreach(HalfEdge he in unBoundedHEs)
            {
                if(voronoiVerts.Contains(he.Origin) ^ voronoiVerts.Contains(he.getTarget()))
                {
                    HalfEdge properHE = voronoiDCEL.isSharedVertex(he.Origin) ? he : he.Twin;
                    // if the other vertex isn't shared, we must shoot other way, too 11/27
                    Vector2 ray = properHE.getRay();
                    Vector2 boxHit = diagramBox.getFirstIntersection(properHE.Origin.Position, ray);
                    Debug.Assert(boxHit != BBox.DNE);
                    Vertex updateVert = (voronoiVerts.Contains(properHE.Origin)) ? properHE.getTarget() : properHE.Origin;
                    updateVert = he.updateEndPt(updateVert, boxHit);
                    Debug.Assert(updateVert != DCEL.INFINITY);
                    voronoiDCEL.Add(updateVert);
                    voronoiVerts.Add(updateVert);
                }
            }

            // add bounding box vertices to DCEL.
            Vertex boxTR = new(diagramBox.upperRight);
            Vertex boxTL = new(diagramBox.getUpperLeft());
            Vertex boxBR = new(diagramBox.getLowerRight());
            Vertex boxBL = new(diagramBox.lowerLeft);

            voronoiDCEL.Add(boxTR);
            voronoiDCEL.Add(boxTL);
            voronoiDCEL.Add(boxBR);
            voronoiDCEL.Add(boxBL);

            // now we close the cells
            foreach(HalfEdge he in unBoundedHEs)
            {
                if (he.Prev == null ^ he.Next == null)
                {
                    HalfEdge startHE = (he.Prev == null) ? he : he.Twin;
                    HalfEdge tempHE = startHE;
                    do
                    {
                        tempHE = tempHE.Next;
                    } while (tempHE.Next != null && tempHE != startHE);

                    // if we've found the otherside, connect it to the startHE along bounding box
                    if (tempHE != startHE)
                    {
                        Vector2 startPos = startHE.Origin.Position;
                        Vector2 endPos = tempHE.getTarget().Position;
                        List<Vector2> boundPoints = diagramBox.getCornersBetween2BorderPts(startPos, endPos);
                        boundPoints.Insert(0, endPos);
                        boundPoints.Add(startPos);
                        sewUpBoundaryPoints(boundPoints, startHE, tempHE, voronoiDCEL);

                    }
                }
            }
            Console.WriteLine("After bounding:");
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
            initFaces(voronoiDCEL);
            float eventY = float.MaxValue;
            VoronoiEvent currEvent;
            int iters = 1;
            float minEventY = float.MaxValue;
            /// need to keep track of the lowest circle event point to pass. 11/26
            while (!eventQueue.heapEmpty()) 
            {
                if (debug)
                    printFortuneInfo(eventQueue, beach, iters);

                eventY = (float) eventQueue.peekTopOfHeap();
                currEvent = eventQueue.peekTopOfObjHeap();

                if (currEvent.isSiteEvent())
                {
                    handleSiteEvent(eventY, eventQueue, beach, voronoiDCEL);
                    minEventY = eventY;
                }
                else if (!currEvent.isSiteEvent() && currEvent.circleEventIsActive)
                {
                    handleCircleEvent(currEvent, eventQueue, beach, voronoiDCEL);
                    minEventY = eventY;
                }

                //if (eventY < minEventY)
                   // minEventY = eventY;
                eventQueue.extractHeadOfHeap();
                iters++;
            }

            //3.Cleanup any remaining intermediate state via bounding box clipping
            if(this._siteCount > 1)
                clipVoronoiDiagram(voronoiDCEL, beach, minEventY, this._bbox);

            return voronoiDCEL;
        }

        // initialize DCEL Faces
        private void initFaces(DCEL voronoiDCEL)
        {
            for (int s = 0; s < _siteCount; s++)
            {
                Face siteFace = new Face();
                siteFace.Position = this._sites[s];
                siteFace.Tag = s;
                voronoiDCEL.Add(siteFace);
            }
        }

        // initialize eventQueueLine
		// sort by events maximal Y then by X
        private MaxHeap<VoronoiEvent> initEventQueue(List<Vector2> sites)
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
            this._sites = orderedSites;
            this._siteCount = sites.Count;

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
        private RedBlackTree<RegionNode> initBeachSO()
        {
            // handle edge cases where several starting
            // points are within a certain range TODO 11/12
            return new RedBlackTree<RegionNode>();
        }

        //main algorithm for this class
        public void fortuneMain()
        {
            DCEL vorDCEL = null!;

            //test inits
            List<Vector2> sites = new List<Vector2> { new(142, 600), new(450, 521), new(683, 442), new(385, 285) };
            MaxHeap<VoronoiEvent> eventQueue = initEventQueue(sites);
            RedBlackTree<RegionNode> beach = initBeachSO();

            // 11/18...TO TEST
            // makes a bounded VoronoiDiagram
            vorDCEL = fortuneAlgo(eventQueue, beach, false);

            // 11/25... further clip the diagram
            // if necessary to display in a bounding box.
            VoronoiDiagram voronoiDiagram = new(this._sites, vorDCEL, this._bbox);
            DCEL visibleDCEL = voronoiDiagram.clipVDToBBox(this._bbox);
            return;
        }
    }
}