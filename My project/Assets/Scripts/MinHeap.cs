
/* desc: a generic minHeap that can be used to shuffle around objects such that
 * the lowest value is at the front of an array. Used in Fortune's beachline algo
 for Vornoi Diagrams.
 * with its children located at 2i + 2i+1
 * author: Xymdyx 
 * date: 4/22/22
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace FortuneAlgo
{
    public class MinHeap<T>
    {
        private const bool debug = false;
        private List<T> ptrList = null!; //the objects we're shuffling along with the numbers given, if any
        private List<double> arr;
        private int sizeOfHeap; //sizeOfHeap corresponds to indices. 1-indexed for real heap elements
        private int sizeLimit; //none unless specified
        private T _prevObjRoot = default(T)!; //null for generics

        public List<T> objMinMHeap { get => this.ptrList; }
        public List<double> doubleMinHeap { get => this.arr; }
        public T prevObjRoot { get => this._prevObjRoot; } //when we pop the head out
        public int heapSize { get => this.sizeOfHeap; }

        // Create a constructor  
        public MinHeap(int sizeLimit = 0)
        {
            //We are adding size+1, because array index 0 will be blank.  
            if (sizeLimit < 0)
                sizeLimit = 0;

            this.arr = new List<double>(new double[sizeLimit + 1]);
            for (int idx = 1; idx < arr.Count; idx++)
                arr[idx] = double.MaxValue;

            this.ptrList = new List<T>(new T[sizeLimit + 1]);
            this.sizeLimit = sizeLimit;
            this.sizeOfHeap = 0;
        }

        // does this minHeap have an imposed size limit?
        // there will always be one blank element in the heaps
        public bool hasSizeLimit()
        {
            return sizeLimit > 0;
        }

        //get top value of heap without deleting
        public double peekTopOfHeap()
        {
            if (heapSize == 0)
                return 0;

            return this.arr[1]; //root is really at 1
        }

        //print heap size
        public int getHeapSize(bool print = false)
        {
            if (print)
                Console.WriteLine("The size of the heap is:" + sizeOfHeap);

            return sizeOfHeap;
        }

        //do we have an object array in addition?
        public bool hasObjects()
        {
            return this.ptrList.Count > 0;
        }

        //returns if the heap is full
        public bool heapFull()
        {
            return (this.sizeLimit > 0) && (this.sizeOfHeap == this.sizeLimit)
            && (this.arr.Count - 1 == this.sizeLimit);
        }

        //returns if the heap is empty
        public bool heapEmpty()
        {
            return this.sizeOfHeap == 0;
        }

        // call this if we're also shuffling an object list
        public void swapObjs(int idx1, int idx2)
        {
            T tmpObj = ptrList[idx1];
            ptrList[idx1] = ptrList[idx2];
            ptrList[idx2] = tmpObj;
        }

        //insert into tree
        public void InsertElementInHeap(double value, T obj)
        {

            if (sizeOfHeap < 0) //this means we freeed the tree at one point and have accessed it again.
            {
                Console.WriteLine("MinHeap is empty");
                return;
            }

            //if the list is full, we don't want to insert anything
            //unless it's lower than the root (this can change depending on use of heap) 
            if (heapFull())
            {
                if (value >= peekTopOfHeap())
                    return;
                extractHeadOfHeap();
            }

            //Insertion of value inside the array happens at the last index of the  array, which is the heapSize. Should cover popping case
            if (!hasSizeLimit())
            {
                arr.Add(value);
                ptrList.Add(obj);
            }
            else
            {
                arr[sizeOfHeap + 1] = value;
                ptrList[sizeOfHeap + 1] = obj;
            }

            sizeOfHeap++;
            HeapifyBottomToTop(sizeOfHeap);
            if (debug)
            {
                Console.WriteLine("Inserted " + value + " successfully in Heap !");
                printHeap();
            }
        }

        //helper for inserting an element
        public void HeapifyBottomToTop(int index)
        {
            int parent = index / 2;

            // We are at root of the tree. Hence no more Heapifying is required.  
            if (index <= 1)
                return;

            // If Current value is less than its parent, then we need to swap  
            if (arr[index] < arr[parent])
            {
                double tmp = arr[index];
                arr[index] = arr[parent];
                arr[parent] = tmp;

                //ditto for the objects we're shuffling
                if (ptrList != null)
                    swapObjs(index, parent);
            }
            HeapifyBottomToTop(parent);
            return;
        }

        //Extract Head of Heap
        public double DeleteElementInHeap(double item, T obj = default!)
        {
            if (sizeOfHeap == 0)
            {
                Console.WriteLine("Heap is empty !");
                return -1;
            }

            if (debug)
            {
                Console.WriteLine("Head of the Heap is: " + arr[1]);
                Console.WriteLine($"Trying to extract {item} now...");
            }

            int delIdx = this.arr.IndexOf(item);
            double delVal = arr[delIdx];
            _prevObjRoot = ptrList[delIdx];
            arr[delIdx] = arr[sizeOfHeap]; //Replacing with last element of the array  
            ptrList[delIdx] = ptrList[sizeOfHeap]; //Replacing with last element of the array  

            arr[sizeOfHeap] = double.MaxValue;
            ptrList[sizeOfHeap] = default(T)!; //just to highlight how we overwrote it... should never be reached since sizeOfHeap corresponds to indices
            sizeOfHeap--;
            HeapifyTopToBottom(delIdx);

            if (debug)
            {
                Console.WriteLine("Successfully extracted value  from Heap.");
                printHeap();
            }

            return delVal;
        }

        public double extractHeadOfHeap()
        {
            return DeleteElementInHeap(this.arr[0]);
        }

        //helper for removing the topmost element
        public void HeapifyTopToBottom(int index)
        {
            int left = index * 2;
            int right = (index * 2) + 1;
            int smallestChild = 0;

            //If there is no child of this node, then nothing to do. Just return.  
            if (sizeOfHeap < left)
            {
                return;
            }
            else if (sizeOfHeap == left)
            {
                //If there is only a left child
                if (arr[index] < arr[left]) //left nodes always less than right ones for both min/max heaps
                {
                    double tmp = arr[index];
                    arr[index] = arr[left];
                    arr[left] = tmp;

                    //ditto for the objects we're shuffling
                    if (ptrList != null)
                        swapObjs(index, left);

                }
                return;
            }
            else
            { //If both children are there, find smallest child
                if (arr[left] < arr[right])
                    smallestChild = left;
                else
                    smallestChild = right;

                if (arr[index] > arr[smallestChild])
                { //If Parent is greater than smallest child, then swap  
                    double tmp = arr[index];
                    arr[index] = arr[smallestChild];
                    arr[smallestChild] = tmp;

                    //ditto for the objects we're shuffling
                    if (ptrList != null)
                        swapObjs(index, smallestChild);
                }
            }
            HeapifyTopToBottom(smallestChild);
            return;
        }

        // free the whole heap so it's never used again
        public void deleteHeap()
        {
            if (debug)
                Console.WriteLine("Deleting heap...");

            if (this.hasObjects())
                this.ptrList.Clear();

            this.sizeOfHeap = -1;
            this.sizeLimit = -1;
            this.ptrList = null;
            this.arr.Clear();
            this.arr = null;
            this._prevObjRoot = default(T)!;
        }

        //debug message to print out all elements of heap
        public void printHeapEls()
        {
            if (this.heapEmpty())
                Console.WriteLine("\nMinHeap is empty");

            Console.WriteLine("Printing all the elements of this Heap...");// Printing from 1 because 0th cell is dummy  

            for (int i = 1; i <= sizeOfHeap; i++)
                Console.WriteLine(arr[i] + " ");

            Console.WriteLine("\n");
            return;
        }

        //debug message to print out all elements of obj heap
        public void printObjHeapEls()
        {
            if (!(this.hasObjects()))
                Console.WriteLine("\n No ObjectHeap to print");

            Console.WriteLine("Printing all the elements of Object Heap...");// Printing from 1 because 0th cell is dummy  
            for (int i = 1; i <= sizeOfHeap; i++)
                Console.WriteLine(ptrList[i] + " ");

            Console.WriteLine("\n");
            return;
        }

        //debug util print
        public void printHeap()
        {
            if (this.heapEmpty())
                Console.WriteLine("\nMinHeap is empty");

            int leftNode = 1;
            int rightNode = 2;
            int level = 0;
            //print level by level.. There are 2^n elements in a n-level tree.
            while (leftNode <= sizeOfHeap)
            {
                Console.Write($"\nMinHeap Level {level + 1} :");
                for (int i = leftNode; i < rightNode; i++)
                {
                    if (i <= sizeOfHeap)
                        Console.Write($"{this.arr[i]} ");
                }
                leftNode += (int)Math.Pow(2, level); // 1, 2, 4, 8,16, 48
                rightNode = leftNode + (int)Math.Pow(2, (level + 1)); // 2,4, 8,16, 48  
                level++;
            }

            return;
        }

        //debug util print for underlying obj mh
        public void printObjHeap()
        {
            if (!(this.hasObjects()))
                Console.WriteLine("\nMinObjHeap is empty");

            int leftNode = 1;
            int rightNode = 2;
            int level = 0;
            //print level by level.. There are 2^n elements in a n-level tree.
            while (leftNode <= sizeOfHeap)
            {
                Console.Write($"\nMinObjHeap Level {level + 1} :");
                for (int i = leftNode; i < rightNode; i++)
                {
                    if (i <= sizeOfHeap)
                        Console.Write($"{this.ptrList[i]} ");
                }
                leftNode += (int)Math.Pow(2, level);
                rightNode = leftNode + (int)Math.Pow(2, (level + 1));
                level++;
            }
            Console.WriteLine("\n");
            return;
        }
    }
}
/*testing for max heap
static void Main()
{
    MinHeap<int> intHeap = new MinHeap<int>();
    for (int i = 0; i < 100; i++)
        intHeap.InsertElementInHeap(i, i);


    Console.WriteLine("First Heap :");
    intHeap.printHeapEls();
    Console.WriteLine("Deleting heap...");
    intHeap.deleteHeap();

    intHeap = new MinHeap<int>(20);

    for (int i = 100; i >= 0; i--)
        intHeap.InsertElementInHeap(i, i);

    Console.WriteLine("Second Heap :");
    intHeap.printHeapEls();
    Console.WriteLine("Deleting heap...");
    intHeap.deleteHeap();


    // Custom inputs
    intHeap = new MinHeap<int>(9);
    intHeap.InsertElementInHeap(5, 5);
    intHeap.InsertElementInHeap(3, 3);
    intHeap.InsertElementInHeap(17, 17);
    intHeap.InsertElementInHeap(10, 10);
    intHeap.InsertElementInHeap(84, 84);
    intHeap.InsertElementInHeap(19, 19);
    intHeap.InsertElementInHeap(6, 6);
    intHeap.InsertElementInHeap(22, 22);
    intHeap.InsertElementInHeap(9, 9);

    intHeap.InsertElementInHeap(78, 78);
    intHeap.InsertElementInHeap(210, 210);

    Console.WriteLine(" GFG Test:");

    intHeap.printHeapEls();
    Console.WriteLine("Deleting heap...");
    intHeap.deleteHeap();

}*/

/* old extractHeadofHeap
public double extractHeadOfHeap()
{
if (sizeOfHeap == 0)
{
    Console.WriteLine("Heap is empty !");
    return -1;
}

if (debug)
{
    Console.WriteLine("Head of the Heap is: " + arr[1]);
    Console.WriteLine("Extracting it now...");
}

double extractedValue = arr[1];
_prevObjRoot = ptrList[1];
arr[1] = arr[sizeOfHeap]; //Replacing with last element of the array  
ptrList[1] = ptrList[sizeOfHeap]; //Replacing with last element of the array  

arr[sizeOfHeap] = double.MaxValue;
ptrList[sizeOfHeap] = default(T)!; //just to highlight how we overwrote it... should never be reached since sizeOfHeap corresponds to indices
sizeOfHeap--;
HeapifyTopToBottom(1);

if (debug)
{
    Console.WriteLine("Successfully extracted value from Heap.");
    printHeapEls();
}

return extractedValue;
//return DeleteElementInHeap(this.arr[0]);
}
 */



