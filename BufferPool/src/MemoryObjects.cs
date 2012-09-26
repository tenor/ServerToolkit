#region License
/*
   Copyright 2011 Sunny Ahuwanya (www.ahuwanya.net)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
#endregion

namespace ServerToolkit.BufferManagement
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Represents a defined block in memory
    /// </summary>
    internal sealed class MemoryBlock : IMemoryBlock
    {
        readonly long startLoc, endLoc, length;
        readonly IMemorySlab owner;

        /// <summary>
        /// Initializes a new instance of the MemoryBlock class
        /// </summary>
        /// <param name="startLocation">Offset where memory block starts in slab</param>
        /// <param name="length">Length of memory block</param>
        /// <param name="slab">Slab to be associated with the memory block</param>
        internal MemoryBlock(long startLocation, long length, IMemorySlab slab)
        {
            if (startLocation < 0)
            {
                throw new ArgumentOutOfRangeException("startLocation", "StartLocation must be greater than 0");
            }

            startLoc = startLocation;

            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length", "Length must be greater than 0");
            }

            endLoc = startLocation + length - 1;
            this.length = length;
            if (slab == null) throw new ArgumentNullException("slab");
            this.owner = slab;

            //TODO: When this class is converted to a struct, consider implementing IComparer, IComparable -- first figure out what sorted dictionary uses those Comparer things for
        }

        /// <summary>
        /// Gets the offset in the slab where the memory block begins.
        /// </summary>
        public long StartLocation
        {
            get
            {
                return startLoc;
            }
        }

        //TODO: EndLocation should be calculated. It's not referenced as often as start location and length

        /// <summary>
        /// Gets the offset in the slab where the memory block ends.
        /// </summary>
        /// <remarks> EndLocation = StartLocation + Length - 1</remarks>
        public long EndLocation
        {
            get
            {
                return endLoc;
            }
        }

        /// <summary>
        /// Gets the length of the memory block, in bytes.
        /// </summary>
        /// <remarks> Length = EndLocation - StartLocation + 1</remarks>
        public long Length
        {
            get { return length; }
        }

        /// <summary>
        /// Gets the containing slab
        /// </summary>
        public IMemorySlab Slab
        {
            get { return owner; }
        }

    }

    /// <summary>
    /// Represents a large fixed-length memory block where smaller variable-length memory blocks are dynamically allocated
    /// </summary>
    internal sealed class MemorySlab : IMemorySlab
    {
        private readonly bool is64BitMachine;
        private readonly long slabSize;
        private readonly BufferPool pool;

        private Dictionary<long, IMemoryBlock> dictStartLoc = new Dictionary<long, IMemoryBlock>();
        private Dictionary<long, IMemoryBlock> dictEndLoc = new Dictionary<long, IMemoryBlock>();
        private SortedDictionary<long, SortedDictionary<long, IMemoryBlock>> freeBlocksList = new SortedDictionary<long, SortedDictionary<long, IMemoryBlock>>();
        private object sync = new object();

        private long largest;
        private byte[] array;

        /// <summary>
        /// Initializes a new instance of the MemorySlab class
        /// </summary>
        /// <param name="size">Size, in bytes, of slab</param>
        /// <param name="pool">BufferPool associated with the slab</param>
        internal MemorySlab(long size, BufferPool pool)
        {

            if (size < 1)
            {
                //Can't have zero length or -ve slabs
                throw new ArgumentOutOfRangeException("size");
            }

            //NOTE: Pool parameter is allowed to be null for testing purposes

            if (System.IntPtr.Size > 4)
            {
                is64BitMachine = true;
            }
            else
            {
                is64BitMachine = false;
            }

            // lock is unnecessary in this instance constructor
            //lock (sync)
            //{
                IMemoryBlock first;
                if (!dictStartLoc.TryGetValue(0, out first))
                {
                    AddFreeBlock(0, size, false);
                    this.slabSize = size;
                    this.pool = pool;
                    // GC.Collect(); //Perform Garbage Collection before creating large array -- commented out but may be useful
                    array = new byte[size];
                }
            //}
        }

        /// <summary>
        /// Gets the last known largest unallocated contiguous block size.
        /// </summary>
        public long LargestFreeBlockSize
        {
            get { return GetLargest(); }
        }

        /// <summary>
        /// Gets the size, in bytes, of the memory slab.
        /// </summary>
        public long Size
        {
            get
            {
                return slabSize;
            }
        }

        /// <summary>
        /// Gets the underlying byte array that is wrapped by the memory slab
        /// </summary>
        public byte[] Array
        {
            get
            {
                return array;
            }
        }

        /// <summary>
        /// Attempts to allocate a memory block of a specified length.
        /// </summary>
        /// <param name="length">Length, in bytes, of memory block</param>
        /// <param name="allocatedBlock">Allocated memory block</param>
        /// <returns>True, if memory block was allocated. False, if otherwise</returns>
        public bool TryAllocate(long length, out IMemoryBlock allocatedBlock)
        {
            return TryAllocate(length, length, out allocatedBlock) > 0;
        }

        /// <summary>
        /// Attempts to allocate a memory block of length between minLength and maxLength (both inclusive)
        /// </summary>
        /// <param name="minLength">The minimum acceptable length</param>
        /// <param name="maxLength">The maximum acceptable length</param>
        /// <param name="allocatedBlock">Allocated memory block</param>
        /// <returns>Length of allocated block if successful, zero otherwise</returns>
        /// <remarks>This overload is useful when multiple threads are concurrently working on the slab and the caller wants to allocate a block up to a desired size</remarks>
        public long TryAllocate(long minLength, long maxLength, out IMemoryBlock allocatedBlock)
        {
            if (minLength > maxLength) throw new ArgumentException("minLength is greater than maxLength", "minLength");
            if (minLength <= 0) throw new ArgumentOutOfRangeException("minLength must be greater than zero", "minLength");
            if (maxLength <= 0) throw new ArgumentOutOfRangeException("maxLength must be greater than zero", "maxLength");

            allocatedBlock = null;
            lock (sync)
            {
                if (freeBlocksList.Count == 0) return 0;

                long[] keys = new long[freeBlocksList.Count];
                freeBlocksList.Keys.CopyTo(keys, 0);

                //Leave if the largest free block cannot hold minLength
                if (keys[keys.LongLength - 1] < minLength)
                {
                    return 0;
                }

                //search freeBlocksList looking for the smallest available free block than can fit maxLength
                long length = maxLength;
                int index = System.Array.BinarySearch<long>(keys, maxLength);
                if (index < 0)
                {
                    index = ~index;
                    if (index >= keys.Length)
                    {
                        //index is set to the largest free block which can hold minLength
                        index = keys.Length - 1;

                        //length is set to the size of that free block
                        length = keys[index];
                    }
                }


                //Grab the first memoryblock in the freeBlockList innerSortedDictionary
                //There is guanranteed to be an innerSortedDictionary with at least 1 key=value pair
                //TODO: Simplify this
                IMemoryBlock foundBlock = null;
                foreach (KeyValuePair<long, IMemoryBlock> pair in freeBlocksList[keys[index]])
                {
                    foundBlock = pair.Value;
                    break;
                }



                if (foundBlock.Length == length)
                {
                    //Perfect match:

                    //Remove existing free block -- and set Largest if need be
                    RemoveFreeBlock(foundBlock, false);

                    allocatedBlock = foundBlock;
                }
                else
                {
                    //FoundBlock is larger than requested block size

                    //Shrink the existing free memory block by the new allocation
                    ShrinkFreeMemoryBlock(foundBlock, foundBlock.Length - length);

                    allocatedBlock = new MemoryBlock(foundBlock.StartLocation, length, this);


                }

                return length;
            }

        }

        /// <summary>
        /// Frees an allocated memory block.
        /// </summary>
        /// <param name="allocatedBlock">Allocated memory block to be freed</param>
        /// <remarks>This method does not verify if the allocatedBlock is indeed from this slab. Callers should make sure that the allocatedblock belongs to the right slab.</remarks>
        public void Free(IMemoryBlock allocatedBlock)
        {
            //NOTE: This method can call the pool to do some cleanup (which holds locks), therefore do not call this method from within any lock
            //Or you'll get into a deadlock.

            lock (sync)
            {
                //Attempt to coalesce/merge free blocks around the allocateblock to be freed.
                long? newFreeStartLocation = null;
                long newFreeSize = 0;

                if (allocatedBlock.StartLocation > 0)
                {

                    //Check if block before this one is free

                    IMemoryBlock blockBefore;
                    if (dictEndLoc.TryGetValue(allocatedBlock.StartLocation - 1, out blockBefore))
                    {
                        //Yup, so remove the free block
                        newFreeStartLocation = blockBefore.StartLocation;
                        newFreeSize += blockBefore.Length;
                        RemoveFreeBlock(blockBefore, true);
                    }

                }

                //Include  AllocatedBlock
                if (!newFreeStartLocation.HasValue) newFreeStartLocation = allocatedBlock.StartLocation;
                newFreeSize += allocatedBlock.Length;

                if (allocatedBlock.EndLocation + 1 < Size)
                {
                    // Check if block next to (below) this one is free
                    IMemoryBlock blockAfter;
                    if (dictStartLoc.TryGetValue(allocatedBlock.EndLocation + 1, out blockAfter))
                    {
                        //Yup, remove the free block
                        newFreeSize += blockAfter.Length;
                        RemoveFreeBlock(blockAfter, true);
                    }
                }

                //Mark entire contiguous block as free -- and set Largest if need be:
                //The length of the AddFreeBlock call will always be longer than or equals to any of the RemoveFreeBlock
                // calls, so it's SetLargest logic will always work.
                AddFreeBlock(newFreeStartLocation.Value, newFreeSize, false);

            }

            if (GetLargest() == Size)
            {
                //This slab is empty. prod pool to do some cleanup
                if (pool != null)
                {
                    pool.TryFreeSlabs();
                }
            }

        }

        /// <summary>
        /// Sets a new value as the largest unallocated contiguous block size
        /// </summary>
        /// <param name="value">Integral value of new largest unallocated block size</param>
        private void SetLargest(long value)
        {
            if (is64BitMachine)
            {
                largest = value;
                //Interlocked.Exchange(ref largest, value);
            }
            else
            {
                Interlocked.Exchange(ref largest, value);
            }

        }

        /// <summary>
        /// Marks an allocated block as unallocated
        /// </summary>
        /// <param name="startLocation">Offset of block in slab</param>
        /// <param name="length">Length of block</param>
        /// <param name="suppressSetLargest">True to not have this method update LargestFreeBlockSize</param>
        /// <remarks>Set suppressSetLargest when the caller will the LargestFreeBlockSize after this method is called</remarks>
        private void AddFreeBlock(long startLocation, long length, bool suppressSetLargest)
        {
            SortedDictionary<long, IMemoryBlock> innerList;
            if (!freeBlocksList.TryGetValue(length, out innerList))
            {
                innerList = new SortedDictionary<long, IMemoryBlock>();
                freeBlocksList.Add(length, innerList);
            }

            MemoryBlock newFreeBlock = new MemoryBlock(startLocation, length, this);
            innerList.Add(startLocation, newFreeBlock);
            dictStartLoc.Add(startLocation, newFreeBlock);
            dictEndLoc.Add(startLocation + length - 1, newFreeBlock);
            if (!suppressSetLargest && GetLargest() < length)
            {
                SetLargest(length);
            }
        }

        
        /// <summary>
        /// Marks an unallocated contiguous block as allocated
        /// </summary>
        /// <param name="block">newly allocated block</param>
        private void RemoveFreeBlock(IMemoryBlock block, bool suppressSetLargest)
        {
            ShrinkOrRemoveFreeMemoryBlock(block, 0, suppressSetLargest);
        }

        /// <summary>
        /// Marks an unallocated contiguous block as allocated, and then marks an allocated block as unallocated
        /// </summary>
        /// <param name="block">newly allocated block</param>
        /// <param name="shrinkTo">The length of the smaller free memory block</param>
        /// <param name="suppressSetLargest">True to not have this method update LargestFreeBlockSize</param>
        /// <remarks>
        /// Set suppressSetLargest when the caller will the LargestFreeBlockSize after this method is called</remarks>
        /// <remarks>
        private void ShrinkFreeMemoryBlock(IMemoryBlock block, long shrinkTo)
        {
            ShrinkOrRemoveFreeMemoryBlock(block, shrinkTo, false);
        }


        /// <summary>
        /// Marks an unallocated contiguous block as allocated, and then marks an allocated block as unallocated
        /// </summary>
        /// <param name="block">newly allocated block</param>
        /// <param name="shrinkTo">The length of the smaller free memory block</param>
        /// <param name="suppressSetLargest">True to not have this method update LargestFreeBlockSize</param>
        /// <remarks>
        /// Set suppressSetLargest when the caller will the LargestFreeBlockSize after this method is called</remarks>
        /// <remarks>
        /// Do not call this method directly, instead call ShrinkFreeMemoryBlock() or the FreeMemoryBlock()
        /// </remarks>
        private void ShrinkOrRemoveFreeMemoryBlock(IMemoryBlock block, long shrinkTo, bool suppressSetLargest)
        {
            //NOTE: Do not call this method directly, instead call ShrinkFreeMemoryBlock() or RemoveFreeBlock()

            //If shrinking confirm that suppressSetLargest is false
            System.Diagnostics.Debug.Assert((shrinkTo > 0 && suppressSetLargest == false) || shrinkTo == 0);
            
            System.Diagnostics.Debug.Assert(shrinkTo <= block.Length);
            System.Diagnostics.Debug.Assert(shrinkTo >= 0 );

            dictStartLoc.Remove(block.StartLocation);
            dictEndLoc.Remove(block.EndLocation);

            bool calcLargest = false;
            if (freeBlocksList[block.Length].Count == 1)
            {
                freeBlocksList.Remove(block.Length);
                if (!suppressSetLargest && GetLargest() == block.Length)
                {
                    //The largest free block was removed, so there will be a new largest
                    calcLargest = true;
                }
            }
            else
            {
                freeBlocksList[block.Length].Remove(block.StartLocation);
            }

            if (shrinkTo > 0)
            {
                AddFreeBlock(block.StartLocation + (block.Length - shrinkTo), shrinkTo, !calcLargest); 
            }

            if (calcLargest)
            {
                //Get the true largest
                if (freeBlocksList.Count == 0)
                {
                    SetLargest(0);
                }
                else
                {
                    long[] indices = new long[freeBlocksList.Count];
                    freeBlocksList.Keys.CopyTo(indices, 0);
                    SetLargest(indices[indices.LongLength - 1]);
                }
            }
        }

        /// <summary>
        /// Gets the largest unallocated contiguous block size in the slab
        /// </summary>
        /// <returns>The size of the largest free contiguous block</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private long GetLargest()
        {
            //This IF statement should be sufficiently complex to prevent inline optimization,
            //however the method is also marked [MethodImpl(MethodImplOptions.NoInlining)] to be extra sure

            if (is64BitMachine)
            {
                return largest;
            }
            else
            {
                return Interlocked.Read(ref largest);
            }
        }
    }
}
