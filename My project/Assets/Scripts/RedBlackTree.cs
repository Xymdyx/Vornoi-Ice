/* filename: RedBlackTree.cs
 author: Sam Ford (stf8464)
 desc: utility this-balancing binary search tree class (sweep-line status)
       inserts duplicates into right child of a given parent 
 due: Thursday 10/6
https://www.programiz.com/dsa/red-black-tree tutorial I followed
https://www.cs.usfca.edu/~galles/visualization/RedBlack.html Animated App for comparison (dups in left here)
*/

using System.Collections.Generic;
using System;

namespace FortuneAlgo
{
    //reps a node in the RedBlackTree
    public class RBNode<T>
    {
        private int _color;
        private float _key;
        private RBNode<T> _parent;
        private RBNode<T> _left;
        private RBNode<T> _right;
        private T _obj = default(T)!;

        //properties
        public int color { get => this._color; set => this._color = value; }
        public float key { get => this._key; set => this._key = value; }
        public RBNode<T> parent { get => this._parent; set => this._parent = value; }
        public RBNode<T> left { get => this._left; set => this._left = value; }
        public RBNode<T> right { get => this._right; set => this._right = value; }
        public T obj { get => this._obj; set => this._obj = value; }

        //constructor
        public RBNode(float key, T obj = default!, int color = 1)
        {
            this._color = color; // 1 for red, 0 for black
            this._key = key;
            this._parent = null!;
            this._left = null!;
            this._right = null!;
            this._obj = obj;
        }


        //hacky code that's going to be refactored because this shouldn't be a thing in an RBT
        //since it could screw up key vals
        //only call when called by RBT itthis in uniformScale 10/6
		//TODO... for Fortune internal nodes, must use breakpoint when traversing tree
        public void updateVal(float scale)
        {
            //      if(this.obj is sn){
            //          this.obj.treeVal = this.obj._getYAtX(scale);
            //          this.key = this.obj.treeVal;
            //}
            return;
        }

        /*print col&key only*/
        public void _minPrint()
        {
            string colStr = (this.color == 0) ? "B" : "R";
            Console.WriteLine($"{colStr}{this.key}");
            return;
        }

        /*print all about it*/
        public void _fullPrint()
        {
            string colStr = (this.color == 0) ? "B" : "R";
            Console.WriteLine($"{colStr}K: {this.key} P: {this.parent.key} L: {this.left.key} R: {this.right.key}");
            return;
        }

        /*squished*/
        public override string ToString()
        {
            string colStr = (this.color == 0) ? "B" : "R";
            return $"{colStr}K:{this.key}P:{this.parent.key}L:{this.left.key}R:{this.right.key}";
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
    public class RedBlackTree<T>
    {
        /*Red-Black Tree that
        maintains a balanced binary search tree
        by imposing coloring of the levels of a BST*/
        private RBNode<T> NIL = new RBNode<T>(0.0f, default(T)!, 0);
        public RBNode<T> _root;
        private int _nodeCount;

        public RBNode<T> root { get => this._root; set => this._root = value; }
        public int nodeCount { get => this._nodeCount; set => this._nodeCount = value; }

        public RedBlackTree()
        {
            //this is a black leaf node for comparison ops
            this._root = this.NIL;
            this._nodeCount = 0;
            return;
        }

        /*Private helper methods*/

        //check if a given node is NIL
        private bool _isNIL(RBNode<T> node)
        {
            return node == this.NIL;
        }

        /*balancing mechanism for RBT
        a childTree y moves its left child to its
        parent x, and then becomes x's parent
        el is x here*/
        private void _leftRot(RBNode<T> el)
        {
            RBNode<T> foster = el.right;
            el.right = foster.left;

            //if there's a left subtree, make el it's parent
            if (foster.left != this.NIL)
                foster.left.parent = el;


            foster.parent = el.parent; //transfer left child of foster to el
            foster.left = this.NIL; //for clarity

            RBNode<T> treeRoot = el.parent;
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
        private void _rightRot(RBNode<T> el)
        {
            RBNode<T> foster = el.left;
            el.left = foster.right;

            //if there's a right subtree, make el it's parent
            if (foster.right != this.NIL)
                foster.right.parent = el;

            foster.parent = el.parent; //transfer right child of foster to el
            foster.right = this.NIL; //for clarity

            RBNode<T> treeRoot = el.parent;
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
        private void _insRepair(RBNode<T> el)
        {
            RBNode<T> p = el.parent;
            RBNode<T> gp = p.parent;
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
                    else if (p.right == el)
                    { //if parent's right is el, leftRot el
                        el = p;
                        this._leftRot(el);
                    }
                    else
                    { //in all other cases, color the parent black, grandparent red, rightRot gp
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
                    else if (p.left == el)
                    { //if parent's left is el, rightRot el
                        el = p;
                        this._rightRot(el);
                    }
                    else
                    { //in all other cases, color the parent black, grandparent black, leftRot gp
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
        private void _transplant(RBNode<T> n1, RBNode<T> n2)
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
        private void _delRepair(RBNode<T> x)
        {
            RBNode<T> parent = null!;
            RBNode<T> sib = null!;
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
                    else if (sib.right.color == 0)
                    {
                        sib.left.color = 0;
                        sib.color = 1;
                        this._rightRot(sib);
                        sib = parent.right; //assign rightChild of parent to sib
                    }
                    else
                    {
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
                    else if (sib.left.color == 0)
                    {
                        sib.right.color = 0;
                        sib.color = 1;
                        this._leftRot(sib);
                        sib = parent.left; //assign left of parent to sib
                    }
                    else
                    {
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
        private bool _checkKeyTypes(RBNode<T> n1, RBNode<T> n2)
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
        public bool isLeaf(RBNode<T> node)
        {
            return ( _isNIL(node.right) ) && ( _isNIL(node.left) );
        }
		
        //get the root of the tree
        public RBNode<T> getRoot() { return this.root; }

        /*find and return a node in the list if it exists*/
        public RBNode<T> find(float key, T obj = default(T)!, RBNode<T> start = null!)
        {
            RBNode<T> el = start;

            if (start == null) //if el not provided, start at root
                el = this.root;

            RBNode<T> found = this.NIL;
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
            {
                //string objStr = if obj != null ? $" && obj {obj}" : "";
                //Console.WriteLine($"Couldn't find node with key {key}" + objStr + " in RBT");
                return null!;
            }

            return found;
        }

        /*nsert an item into the list. New nodes always red*/
        public int insert(float item, T obj = default(T)!)
        {
            RBNode<T> el = new RBNode<T>(item, obj);
            el.left = this.NIL;
            el.right = this.NIL;

            RBNode<T> parent = null!;
            RBNode<T> currNode = this.root;
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
        public int delete(float key, T obj = default(T)!, RBNode<T> start = null!)
        {
            RBNode<T> sNode = start;
            if (start == null) //if start not provided, start at root
                sNode = this.root;

            RBNode<T> found = this.find(key, obj, sNode);

            if (found == null)
                return -1;

            RBNode<T> foundCopy = found;
            int origCol = foundCopy.color;
            RBNode<T> parent = found.parent;

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
        public RBNode<T> minimum(RBNode<T> start = null!)
        {
            RBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            while (el != null && el.left != this.NIL)
                el = el.left;

            return el;
        }

        /*get rightmost ancestor of a tree*/
        public RBNode<T> maximum(RBNode<T> start = null!)
        {
            RBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            while (el != null && el.right != this.NIL)
                el = el.right;

            return el;
        }

        /*Get node with largest key < start.key*/
        public RBNode<T> getPred(RBNode<T> start = null!)
        {
            RBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            if ((el != null) && (el.left != this.NIL))
                return this.maximum(el.left);

            if (el == null)
                return null;

            RBNode<T> parent = el.parent;
            while ((parent != null) && (parent != this.NIL) && (el == parent.left))
            {
                el = parent;
                parent = parent.parent;
            }
            return parent;
        }

        /*Get node with smallest key > start.key*/
        public RBNode<T> getSucc(RBNode<T> start = null!)
        {
            RBNode<T> el = start;
            if (start == null) //if start not provided, start at root
                el = this.root;

            if ((el != null) && (el.right != this.NIL))
                return this.minimum(el.right);

            if (el == null)
                return null;

            RBNode<T> parent = el.parent;
            while ((parent != null) && (parent != this.NIL) && (el == parent.right))
            {
                el = parent;
                parent = parent.parent;
            }
            return parent;
        }


        //convenience method for getting sibling if one exists
        public RBNode<T> getSibling(RBNode<T> start = null!)
        {
            RBNode<T> el = start;

            if (start == null)
                return null!;

            return (el.parent.left == el) ? el.parent.left : el.parent.right;
        }


        /*swap a node with its immediate successor*/
        public void swapAdjacent(RBNode<T> n1)
        { //UNSURE TODO 10/6
            RBNode<T> succ = this.getSucc(n1);
            this.delete(n1.key, n1.obj);
            this.delete(succ.key, succ.obj);
            this.insert(succ.key, n1.obj);
            this.insert(n1.key, succ.obj);

            return;
        }

        /*obliterate this RBT*/
        public void clearRBT()
        {
            this.root = this.NIL;
            this.nodeCount = 0;
            return;
        }

        /*print the bloody RBT level by level*/
        public void _printRBT()
        {
            if (this.root == this.NIL)
            {
                Console.WriteLine("Empty RBT");
                return;
            }

            List<RBNode<T>> nodes = new List<RBNode<T>> { this.root };
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
                        Console.Write($"{nodes[i].key}:{col} ");
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
        public void inOrderPrint(RBNode<T> x)
        {
            if (x != null)
            {
                this.inOrderPrint(x.left);
                x._minPrint();
                this.inOrderPrint(x.right);
            }
            return;
        }

        /*
        UNIFORMLY update trees inorder: LPR
        The underlying scale and updateVal funcs must ensure ordering
        maintained post-update otherwise DO NOT use. Called when left pt appears
        */
        public List<T> inOrderUpdate(float scale, RBNode<T> x = null!)
        {
            List<T> updatedEls = new List<T>();
            if (x != null)
            {
                updatedEls.AddRange(this.inOrderUpdate(scale, x.left));
                if (x != this.NIL)
                {
                    x.updateVal(scale);
                    updatedEls.Add(x.obj);
                }
                updatedEls.AddRange(this.inOrderUpdate(scale, x.right));
            }

            return updatedEls;
        }
    }
}
/*debug main
static void Main(str[] args) 
{
Console.WriteLine("RBT Test 1");
RedBlackTree<float> testRBT = new RedBlackTree<float>();
testRBT.insert(10);
testRBT.delete(10);
testRBT.insert(10);
testRBT.insert(20);
testRBT.insert(30);
testRBT.insert(70);
testRBT.insert(5);
testRBT.insert(20);
testRBT.insert(71);

testRBT._printRBT();
testRBT.delete(71);
testRBT.delete(30);
Console.WriteLine("After deletion:");
testRBT._printRBT();
testRBT.inOrderPrint(testRBT.root);
testRBT.clearRBT();

Console.WriteLine("\nRBT Test 2");
testRBT.insert(55);
testRBT.insert(40);
testRBT.insert(65);
testRBT.insert(60);
testRBT.insert(75);
testRBT.insert(57);
testRBT._printRBT();
Console.WriteLine("After deletion:");
testRBT.delete(57);
testRBT._printRBT();
testRBT.clearRBT();
testRBT._printRBT();

Console.WriteLine("\nRBT Test 3");
for (int i = 1; i < 17; i++)
    testRBT.insert(i);

testRBT.insert(31);
testRBT.insert(0);
testRBT.insert(32);
testRBT._printRBT();
testRBT.clearRBT();
testRBT._printRBT();

Console.WriteLine("\nRBT Test 4");
testRBT.insert(10);
testRBT.insert(10);
testRBT.insert(10);
testRBT.insert(10);
testRBT.insert(20);
RBNode<float> tNode = testRBT.find(10);
RBNode<float> tSucc = testRBT.getSucc(tNode);
Console.WriteLine($"{tNode.key} succ: {tSucc.key}");
}*/

