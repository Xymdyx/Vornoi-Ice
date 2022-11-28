
/* desc: a generic maxHeap that can be used to shuffle around objects such that
 * the highest value is at the front of an array. Used in Photon Mapping
 * with its children located at 2i + 2i+1
 * author: Xymdyx 
 * date: 4/22/22
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace FortuneAlgo
{
    public class MaxHeap<T>
    {
        private const bool debug = false;

        private List<T> ptrList = null!; //the objects we're shuffling along with the numbers given, if any
        private List<double> arr;
        private int sizeOfHeap; //sizeOfHeap corresponds to indices. 1-indexed
        private int sizeLimit; //none unless specified
        private T _prevObjRoot = default(T)!; //null for generics

        public List<T> objMaxMHeap { get => this.ptrList; }
        public List<double> doubleMazHeap { get => this.arr; }
        public T prevObjRoot { get => this._prevObjRoot; } //when we pop the head out
        public int heapSize { get => this.sizeOfHeap; }

        // Create a constructor  
        public MaxHeap(int sizeLimit = 0)
        {
            //We are adding size+1, because array index 0 will be blank.  
            if (sizeLimit < 0)
                sizeLimit = 0;

            //We are adding size+1, because array index 0 will be blank.  
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

        public double peekTopOfHeap()
        {
            if (heapSize == 0)
                return 0;

            return this.arr[1]; //root is really at 1
        }

        public T peekTopOfObjHeap()
        {
            if (heapSize == 0)
                return default!;

            return this.ptrList[1]; //root is really at 1
        }

        /// <summary>
        /// given a key value, find the corresponding object if any
        /// </summary>
        /// <param name="key">
        /// the object we're trying to find given the key </param>
        /// <returns></returns>
        public T grabObjGivenKeyVal(double key) 
        {
            if (this.hasObjects())
            {
                int findIdx = this.arr.IndexOf(key);

                if (findIdx != -1)
                    Console.WriteLine($"Couldn't find object with key {key}");
                    return this.ptrList[findIdx];
            }
            return default!;
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
                Console.WriteLine("MaxHeap is empty");
                return;
            }

            //if the list is full, we don't want to insert anything unless it's less than the root
            if (heapFull())
            {
                if (value >= peekTopOfHeap())
                    return;
                extractHeadOfHeap();
            }
			
			int startPos;

            if (!hasSizeLimit())
            {
                arr.Add(value);
                ptrList.Add(obj);
				startPos = arr.Count - 1;
            }
            else
            {
                //Insertion of value inside the array happens at the last index of the  array, which is the heapSize. Should cover popping case
                arr[sizeOfHeap + 1] = value;
                ptrList[sizeOfHeap + 1] = obj;
				startPos = sizeOfHeap + 1;
            }
            sizeOfHeap++;
            HeapifyBottomToTop(startPos);
            if (debug)
            {
                Console.WriteLine("Inserted " + value + " successfully in Heap !");
                printHeapEls();
            }
        }

        //helper for inserting an element
        private void HeapifyBottomToTop(int index)
        {
            int parent = index / 2;

            // We are at root of the tree. Hence no more Heapifying is required.  
            if (index <= 1)
                return;

            // If Current value is greater than its parent, then we need to swap  
            // TYPO fixed.. 11/20
            if (arr[index] > arr[parent])
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

        //general delete 10/06/22
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

            arr[sizeOfHeap] = double.MinValue;
            ptrList[sizeOfHeap] = default(T)!; //just to highlight how we overwrote it... should never be reached since sizeOfHeap corresponds to indices
            arr.RemoveAt(sizeOfHeap);
            ptrList.RemoveAt(sizeOfHeap);
            sizeOfHeap--;
            HeapifyTopToBottom(delIdx);

            if (debug)
            {
                Console.WriteLine("Successfully extracted value  from Heap.");
                printHeapEls();
            }

            return delVal;
        }

        //Extract Head of Heap  
        public double extractHeadOfHeap()
        {
            return DeleteElementInHeap(this.arr[1]);
        }

        //helper for removing the topmost element
        private void HeapifyTopToBottom(int index)
        {
            int left = index * 2;
            int right = (index * 2) + 1;
            int largestChild = 0;

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
            { //If both children are there, find largest child
                if (arr[left] > arr[right])
                    largestChild = left;
                else
                    largestChild = right;

                if (arr[index] < arr[largestChild])
                { //If Parent is greater than smallest child, then swap  
                    double tmp = arr[index];
                    arr[index] = arr[largestChild];
                    arr[largestChild] = tmp;

                    //ditto for the objects we're shuffling
                    if (ptrList != null)
                        swapObjs(index, largestChild);
                }
            }
            HeapifyTopToBottom(largestChild);
            return;
        }

        // free the whole heap so it's never used again
        public void deleteHeap()
        {
            if (debug)
                Console.WriteLine("Deleting heap...");

            this.sizeOfHeap = -1;
            this.ptrList = null!;
            this.arr = null!;
            this._prevObjRoot = default(T)!;
        }

        //debug message to print out all elements of heap
        public void printHeapEls()
        {
            if (this.heapEmpty())
                Console.WriteLine("\nMaxHeap is empty");

            Console.WriteLine("Printing all the elements of this MaxHeap...");// Printing from 1 because 0th cell is dummy  

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
                Console.WriteLine("\nMaxHeap is empty");

            int leftNode = 1;
            int rightNode = 2;
            int level = 0;
            //print level by level.. There are 2^n elements in a n-level tree.
            while (leftNode <= sizeOfHeap)
            {
                Console.Write($"\nMaxHeap Level {level + 1} :");
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

        /*
        //testing for max heap
        public static void testHeap()
        {
            MaxHeap<int> intHeap = new MaxHeap<int>(20);
            for (int i = 0; i < 100; i++)
                intHeap.InsertElementInHeap(i, i);


            Console.WriteLine("First Heap :");
            intHeap.printHeapEls();
            Console.WriteLine("Deleting heap...");
            intHeap.deleteHeap();

            intHeap = new MaxHeap<int>(20);

            for (int i = 100; i >= 0; i--)
                intHeap.InsertElementInHeap(i, i);

            Console.WriteLine("Second Heap :");
            intHeap.printHeapEls();
            Console.WriteLine("Deleting heap...");
            intHeap.deleteHeap();


            // Custom inputs
            intHeap = new MaxHeap<int>(9);
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
        }
        */
    }
}