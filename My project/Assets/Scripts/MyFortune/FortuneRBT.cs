/* filename: RFortuneRBT.cs
 author: Sam Ford (stf8464)
 desc: red-black tree specialized for Fortune's Algo uses no keys. 
Has linked list for separating beachline order from node order
started: 12/19/22
https://www.programiz.com/dsa/red-black-tree tutorial I followed
https://www.cs.usfca.edu/~galles/visualization/RedBlack.html Animated App for comparison (dups in left here)
*/

using System.Collections.Generic;
using System;

namespace FortuneAlgo
{
    //reps a node in the RedBlackTree
    public class FRBNode<T>
    {
        private int _color;
        private float _key;
        private FRBNode<T> _parent;
        private FRBNode<T> _left;
        private FRBNode<T> _right;
        private FRBNode<T> _pred; //linked list of beachline order
        private FRBNode<T> _succ; //linked list of beachline order
        private T _obj = default(T)!;

        //properties
        public int color { get => this._color; set => this._color = value; }
        public float key { get => this._key; set => this._key = value; } // remove
        public FRBNode<T> parent { get => this._parent; set => this._parent = value; }
        public FRBNode<T> left { get => this._left; set => this._left = value; }
        public FRBNode<T> right { get => this._right; set => this._right = value; }
        public FRBNode<T> pred { get => this._pred; set => this._pred = value; }
        public FRBNode<T> succ { get => this._succ; set => this._succ = value; }

        public T obj { get => this._obj; set => this._obj = value; }

        //constructor
        public FRBNode(float key, T obj = default!, int color = 1)
        {
            this._color = color; // 1 for red, 0 for black
            this._key = key;
            this._parent = null!;
            this._left = null!;
            this._right = null!;
            this._pred = null!;
            this._succ = null!;
            this._obj = obj;
        }

        /*print col&key only*/
        public void _minPrint()
        {
            string colStr = (this.color == 0) ? "B" : "R";
            Console.WriteLine($"{colStr}:{this.key}");
            return;
        }

        /*print all about it*/
        public void _fullPrint()
        {
            string colStr = (this.color == 0) ? "B" : "R";
            float pKey = this.parent != null ? this.parent.key : float.NaN;
            float lKey = this.left != null ? this.left.key : float.NaN;
            float rKey = this.right != null ? this.right.key : float.NaN;
            Console.WriteLine($"{colStr}K: {this.key} P: {pKey} L: {lKey} R: {rKey}");
            return;
        }

        /*squished*/
        public override string ToString()
        {
            string colStr = (this.color == 0) ? "B" : "R";
            return $"{colStr}:{this.key}";
        }
    }

    /*
    Red/Black Property: Every node is colored, either red or black.
    Root Property: The root is black.
    Leaf Property: Every leaf (NIL) is black.
    Red Property: If a red node has children then, the children are always black.
    Depth Property: For each node, any simple path from this node to any of its descendant 
    leaf has the same black-depth (the number of black nodes).
    */
    public class FortuneRBT<T>
    {
        /*Red-Black Tree that
        maintains a balanced binary search tree
        by imposing coloring of the levels of a BST*/
        private FRBNode<T> NIL = new FRBNode<T>(0.0f, default(T)!, 0);
        public FRBNode<T> _root;
        private int _nodeCount;

        public FRBNode<T> root { get => this._root; set => this._root = value; }
        public int nodeCount { get => this._nodeCount; set => this._nodeCount = value; }

        public FortuneRBT()
        {
            //this is a black leaf node for comparison ops
            this._root = this.NIL;
            this._nodeCount = 0;
            return;
        }

        /*Private helper methods*/

        //check if a given node is NIL
        private bool _isNIL(FRBNode<T> node)
        {
            return node == this.NIL;
        }

        /*balancing mechanism for RBT
        a childTree y moves its left child to its
        parent x, and then becomes x's parent
        el is x here*/
        private void _leftRot(FRBNode<T> el)
        {
            FRBNode<T> foster = el.right;
            el.right = foster.left;

            //if there's a left subtree, make el it's parent
            if (foster.left != this.NIL)
                foster.left.parent = el;


            foster.parent = el.parent; //transfer left child of foster to el
            foster.left = this.NIL; //for clarity

            FRBNode<T> treeRoot = el.parent;
            if (treeRoot == null)
                this.root = foster;
            else if (el == treeRoot.left)
                treeRoot.left = foster;
            else
                treeRoot.right = foster;

            foster.left = el; //el is now adopted
            el.parent = foster; //el is now adopted
            return;
        }

        /*balancing mechanism for RBT
        a childTree y moves its right child to its
        parent x, and then becomes x's parent
        el is x here*/
        private void _rightRot(FRBNode<T> el)
        {
            FRBNode<T> foster = el.left;
            el.left = foster.right;

            //if there's a right subtree, make el it's parent
            if (foster.right != this.NIL)
                foster.right.parent = el;

            foster.parent = el.parent; //transfer right child of foster to el
            foster.right = this.NIL; //for clarity

            FRBNode<T> treeRoot = el.parent;
            if (treeRoot == null)
                this.root = foster;
            else if (el == treeRoot.right)
                treeRoot.right = foster;
            else
                treeRoot.left = foster;

            foster.right = el; //el is now adopted
            el.parent = foster; //el is now adopted
            return;
        }

        /*Helper for ensuring tree is RBT after insertion*/
        private void _insRepair(FRBNode<T> el)
        {
            FRBNode<T> p = el.parent;
            FRBNode<T> gp = p.parent;
            //begin loop
            while (p.color == 1)
            {
                if (p == gp.left)
                {
                    if (gp.right.color == 1)
                    { //if gp's right is red, make both black
                        gp.left.color = 0;
                        gp.right.color = 0;
                        gp.color = 1;
                        el = gp;
                    }
                    else // changed because this is an error when I originally typed it -- 12/18
					{
						if (p.right == el)
						{ //if parent's right is el, leftRot el
							el = p;
							this._leftRot(el);
						}
                    //in all other cases, color the parent black, grandparent red, rightRot gp
                        p.color = 0;
                        gp.color = 1;
                        this._rightRot(gp);
                    }
                }
                else
                {
                    if (gp.left.color == 1)
                    { //if gp's right is red, make both black
                        gp.left.color = 0;
                        gp.right.color = 0;
                        gp.color = 1;
                        el = gp;
                    }
                    else // changed because this is an error when I originally typed it -- 12/18
					{
						if (p.left == el)						
						{ //if parent's left is el, rightRot el
							el = p;
							this._rightRot(el);
						}
                    //in all other cases, color the parent black, grandparent black, leftRot gp
                        p.color = 0;
                        gp.color = 1;
                        this._leftRot(gp);
                    }
                }
                if (el == this.root)
                    break;
                //update for next iter
                p = el.parent;
                gp = p.parent;
            }//endloop

            this.root.color = 0; //finally color the root black
            return;
        }

        /*Helper for the delete method. Causes n2 to replace or parent n1*/
        private void _transplant(FRBNode<T> n1, FRBNode<T> n2)
        {
            if (n1.parent == null)
                this.root = n2;
            else if (n1 == n1.parent.left)
                n1.parent.left = n2;
            else
                n1.parent.right = n2;

            n2.parent = n1.parent!;
            return;
        }

        /*helper to retain black-depth property of tree
       after deleting a node from the RBT
        x (the parent) is occupying the deletedNode's orig spot
        the parent has an extra black*/
        private void _delRepair(FRBNode<T> x)
        {
            FRBNode<T> parent = null!;
            FRBNode<T> sib = null!;
            //begin while
            while (x != this.root && x.color == 0)
            {
                parent = x.parent;
                if (x == parent.left)
                {
                    sib = parent.right;
                    if (sib.color == 1)
                    { //if right child of x's parent is red
                        sib.color = 0;
                        parent.color = 1;
                        this._leftRot(parent);
                        sib = x.parent.right; //update sibling after _leftRot
                    }
                    else if (sib.right.color == 0 && sib.left.color == 0)
                    {
                        sib.color = 1;
                        x = parent; //assign parent of x to x
                    }
                    else // changed because this is an error when I originally typed it -- 12/18
                    {
						 if (sib.right.color == 0)
						 {
							sib.left.color = 0;
							sib.color = 1;
							this._rightRot(sib);
							sib = parent.right; //assign rightChild of parent to sib
						 }
						 
                        sib.color = parent.color;
                        parent.color = 0;
                        sib.right.color = 0;
                        this._leftRot(parent);
                        x = this.root; //assign x as root of tree && terminate next iter
                    }
                }
                else
                { //same as previous block except right -> left and left -> right
                    sib = parent.left;
                    if (sib.color == 1)
                    { //if right child of x's parent is red
                        sib.color = 0;
                        parent.color = 1;
                        this._rightRot(parent);
                        sib = x.parent.left; //update sibling after _rightRot
                    }
                    else if (sib.left.color == 0 && sib.right.color == 0)
                    {
                        sib.color = 1;
                        x = parent; //assign parent of x to x
                    }
                    else  // changed because this is an error when I originally typed it -- 12/18
					{
						if (sib.left.color == 0)
						{
							sib.right.color = 0;
							sib.color = 1;
							this._leftRot(sib);
							sib = parent.left; //assign left of parent to sib
						}
                    
                        sib.color = parent.color;
                        parent.color = 0;
                        sib.left.color = 0;
                        this._rightRot(parent);
                        x = this.root; //assign x as root of tree and terminate next iter
                    }
                }
            }//end while

            x.color = 0; //finally color x black and the RBT is balanced
            return;
        }

        /*helper to ensure two nodes of the same datatype*/
        private bool _checkKeyTypes(FRBNode<T> n1, FRBNode<T> n2)
        {
            return n1.key.GetType() == n2.key.GetType();
        }

        /*Public methods*/
		
		/*if the root is NIL, then we're empty*/
        public bool isEmpty()
        {
            return this.root == this.NIL;
        }
		
		//all visible leaves are black and have NIL as child nodea
        public bool isLeaf(FRBNode<T> node)
        {
            return ( _isNIL(node.right) ) && ( _isNIL(node.left) );
        }
		
        //get the root of the tree
        public FRBNode<T> getRoot() { return this.root; }

        /*find and return a node in the list if it exists*/
        // TODO 12/19... must change for FA
        public FRBNode<T> find(float key, T obj = default(T)!, FRBNode<T> start = null!)
        {
            FRBNode<T> el = start;

            if (start == null) //if el not provided, start at root
                el = this.root;

            FRBNode<T> found = this.NIL;
            while (el != this.NIL)
            {
                if ((el.key == key) && (EqualityComparer<T>.Default.Equals(obj, default(T))
                    || EqualityComparer<T>.Default.Equals(el.obj, obj)))
                    found = el;
                if (el.key <= key)
                    el = el.right;
                else
                    el = el.left;
            }

            if (found == this.NIL)
                return null!;

            return found;
        }

        /*nsert an item into the list. New nodes always red*/
        // TODO 12/19... must change for FA
        public int insert(float item, T obj = default(T)!)
        {
            FRBNode<T> el = new FRBNode<T>(item, obj);
            el.left = this.NIL;
            el.right = this.NIL;

            FRBNode<T> parent = null!;
            FRBNode<T> currNode = this.root;
            float elKey = el.key;

            if (!(this.isEmpty()) && !(this._checkKeyTypes(currNode, el)))
            {
                Console.WriteLine("Mismatched keys used in RBT nodes, aborting operation");
                return -1;
            }
            //travel to a leafNode
			//TODO Compare against breakpoint calc rather than key
            while (currNode != this.NIL)
            {
                parent = currNode;
                if (elKey < currNode.key)
                    currNode = currNode.left;
                else
                    currNode = currNode.right;
            }

            el.parent = parent;
            if (parent == null)
                this.root = el;

            else if (elKey < parent.key)
                parent.left = el;
            else
                parent.right = el;

            if (el.parent == null)
            {
                el.color = 0;
                return 0;
            }
            if (el.parent.parent == null)
                return 0;

            this._insRepair(el);
            this.nodeCount++;

            return 0;
        }

        /*Delete a node with the given key and possibly provided obj*/
        // TODO 12/19... must change for FA
        public int delete(float key, T obj = default(T)!, FRBNode<T> start = null!)
        {
            FRBNode<T> sNode = start;
            if (start == null) //if start not provided, start at root
                sNode = this.root;

            FRBNode<T> found = this.find(key, obj, sNode);

            if (found == null)
                return -1;

            FRBNode<T> foundCopy = found;
            int origCol = foundCopy.color;
            FRBNode<T> parent = found.parent;

            if (found.left == this.NIL)
            {
                parent = found.right;
                this._transplant(found, found.right);
            }
            else if (found.right == this.NIL)
            {
                parent = found.left;
                this._transplant(found, found.left);
            }
            else
            {
                //change left -> right and vice versa to affect which node
                //replaces parent. Change this to match convention
                //of where key of same val goes in insert and find, too
                foundCopy = this.minimum(found.right);
                origCol = foundCopy.color;
                parent = foundCopy.right;

                if (foundCopy.parent == found)
                    parent.parent = foundCopy;
                else
                {
                    this._transplant(foundCopy, foundCopy.right);
                    foundCopy.right = found.right;
                    foundCopy.right.parent = foundCopy;
                }

                this._transplant(found, foundCopy);
                foundCopy.left = found.left;
                foundCopy.left.parent = foundCopy;
                foundCopy.color = found.color;
            }

            if (origCol == 0) //call repair if we deleted a black node
                this._delRepair(parent);

            this.nodeCount--;
            return 0;
        }

        /*get leftmost ancestor of a tree*/
        public FRBNode<T> minimum(FRBNode<T> start = null!)
        {
            FRBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            while (el != null && el.left != this.NIL)
                el = el.left;

            return el;
        }

        /*get rightmost ancestor of a tree*/
        public FRBNode<T> maximum(FRBNode<T> start = null!)
        {
            FRBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            while (el != null && el.right != this.NIL)
                el = el.right;

            return el;
        }

        /*Get node with largest key < start.key*/
        // TODO 12/19... must change for FA.... YAGNI
        public FRBNode<T> getPred(FRBNode<T> start = null!)
        {
            FRBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            if ((el != null) && (el.left != this.NIL))
                return this.maximum(el.left);

            if (el == null)
                return null;

            FRBNode<T> parent = el.parent;
            while ((parent != null) && (parent != this.NIL) && (el == parent.left))
            {
                el = parent;
                parent = parent.parent;
            }
            return parent;
        }

        /*Get node with smallest key > start.key*/
        // TODO 12/19... must change for FA.... YAGNI
        public FRBNode<T> getSucc(FRBNode<T> start = null!)
        {
            FRBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            if ((el != null) && (el.right != this.NIL))
                return this.minimum(el.right);

            if (el == null)
                return null;

            FRBNode<T> parent = el.parent;
            while ((parent != null) && (parent != this.NIL) && (el == parent.right))
            {
                el = parent;
                parent = parent.parent;
            }
            return parent;
        }


        //convenience method for getting sibling if one exists
        public FRBNode<T> getSibling(FRBNode<T> start = null!)
        {
            FRBNode<T> el = start;

            if (start == null)
                return null!;

            return (el.parent.left == el) ? el.parent.right : el.parent.left;
        }

        /*obliterate this RBT*/
        public void clearRBT()
        {
            this.root = this.NIL;
            this.nodeCount = 0;
            return;
        }

        /*print the bloody RBT level by level*/
        public void _printRBT(bool printObj = false)
        {
            if (this.root == this.NIL)
            {
                Console.WriteLine("Empty RBT");
                return;
            }

            List<FRBNode<T>> nodes = new List<FRBNode<T>> { this.root };
            int currLevel = 0;
            int lvlNodesLeft = (int)Math.Pow(2, currLevel);
            int size = nodes.Count;
            int i = 0;
            //beginwhile
            while (i < size)
            {
                if (nodes[i] != null)
                {
                    nodes.Add(nodes[i].left);
                    nodes.Add(nodes[i].right);
                    size += 2;
                    if (nodes[i].left != null || nodes[i].right != null)
                    {
                        string col = (nodes[i].color == 0) ? "B" : "R";
                        if(!printObj)
                            Console.Write($"{nodes[i].key}:{col} ");
                        else
                            Console.Write($"{nodes[i].obj}:(({col}))  ");

                    }
                    else
                        Console.Write("NIL ");
                }
                lvlNodesLeft--;
                if (lvlNodesLeft == 0)
                {
                    Console.WriteLine($"\tRBT Level {currLevel}\n");
                    currLevel++;
                    lvlNodesLeft = (int)Math.Pow(2, currLevel);
                }
                i++;
            }
            //endwhile
            if (lvlNodesLeft > 0)
                Console.WriteLine($"\tRBT Level {currLevel}\n");

            return;
        }

        /*Print trees inorder: LPR*/
        public void inOrderPrint(FRBNode<T> x)
        {
            if (x != null)
            {
                this.inOrderPrint(x.left);
                x._minPrint();
                this.inOrderPrint(x.right);
            }
            return;
        }

        /// <summary>
        /// grab all elements in the tree
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public List<T> inOrderGrab(FRBNode<T> x = null!)
        {
            List<T> rbtEls = new List<T>();
            if (x != null)
            {
                rbtEls.AddRange(this.inOrderGrab(x.left));
                if (x != this.NIL)
                    rbtEls.Add(x.obj);
                
                rbtEls.AddRange(this.inOrderGrab(x.right));
            }

            return rbtEls;
        }

        /// <summary>
        /// grab all elements in the tree
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public List<T> inOrderGrabInternals(FRBNode<T> x = null!)
        {
            List<T> rbtEls = new List<T>();
            if (x != null)
            {
                rbtEls.AddRange(this.inOrderGrabInternals(x.left));
                if (!this.isLeaf(x) && !_isNIL(x))
                    rbtEls.Add(x.obj);

                rbtEls.AddRange(this.inOrderGrabInternals(x.right));
            }

            return rbtEls;
        }
    }
}