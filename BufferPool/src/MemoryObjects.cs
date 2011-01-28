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
    using System.Threading;

    /// <summary>
    /// Represents a defined block in memory
    /// </summary>
    internal class MemoryBlock : IMemoryBlock
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
        }

        /// <summary>
        /// Gets the offset in the slab where the memory block begins.
        /// </summary>
        public virtual long StartLocation
        {
            get
            {
                return startLoc;
            }
        }

        /// <summary>
        /// Gets the offset in the slab where the memory block ends.
        /// </summary>
        /// <remarks> EndLocation = StartLocation + Length - 1</remarks>
        public virtual long EndLocation
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
        public virtual long Length
        {
            get { return length; }
        }

        /// <summary>
        /// Gets the containing slab
        /// </summary>
        public virtual IMemorySlab Slab
        {
            get { return owner; }
        }

    }

    /// <summary>
    /// Represents a large fixed-length memory block where smaller variable-length memory blocks are dynamically allocated
    /// </summary>
    internal class MemorySlab : IMemorySlab
    {
        protected readonly bool is64BitMachine;
        protected readonly long slabSize;
        protected readonly BufferPool pool;

        protected Dictionary<long, IMemoryBlock> dictStartLoc = new Dictionary<long, IMemoryBlock>();
        protected Dictionary<long, IMemoryBlock> dictEndLoc = new Dictionary<long, IMemoryBlock>();
        protected SortedDictionary<long, SortedDictionary<long, IMemoryBlock>> freeBlocksList = new SortedDictionary<long, SortedDictionary<long, IMemoryBlock>>();
        protected object sync = new object();

        protected long largest = 0;
        protected byte[] array;

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
                throw new ArgumentOutOfRangeException("Size");
            }

            //Pool parameter is allowed to be null for testing purposes

            if (System.IntPtr.Size > 4)
            {
                is64BitMachine = true;
            }
            else
            {
                is64BitMachine = false;
            }

            lock (sync)
            {
                IMemoryBlock first;
                if (!dictStartLoc.TryGetValue(0, out first))
                {
                    AddFreeBlock(0, size);
                    this.slabSize = size;
                    this.pool = pool;
                    // GC.Collect(); //Perform Garbage Collection before creating large array -- may be useful
                    array = new byte[size];
                }
            }
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
        public virtual bool TryAllocate(long length, out IMemoryBlock allocatedBlock)
        {
            allocatedBlock = null;
            lock (sync)
            {
                if (GetLargest() < length) return false;

                //search freeBlocksList looking for the smallest available free block
                long[] keys = new long[freeBlocksList.Count];
                freeBlocksList.Keys.CopyTo(keys, 0);
                int index = System.Array.BinarySearch<long>(keys, length);
                if (index < 0)
                {
                    index = ~index;
                    if (index >= keys.LongLength)
                    {
                        return false;
                    }
                }

                //Grab the first memoryblock in the freeBlockList innerSortedDictionary
                //There is guanranteed to be an innerSortedDictionary with at least 1 key=value pair
                IMemoryBlock foundBlock = null;
                foreach (KeyValuePair<long, IMemoryBlock> kvPair in freeBlocksList[keys[index]])
                {
                    foundBlock = kvPair.Value;
                    break;
                }

                //Remove existing free block
                RemoveFreeBlock(foundBlock);


                if (foundBlock.Length == length)
                {
                    //Perfect match
                    allocatedBlock = foundBlock;
                }
                else
                {
                    //FoundBlock is larger than requested block size

                    allocatedBlock = new MemoryBlock(foundBlock.StartLocation, length, this);

                    long newFreeStartLocation = allocatedBlock.EndLocation + 1;
                    long newFreeSize = foundBlock.Length - length;

                    //add new Freeblock with the smaller remaining space
                    AddFreeBlock(newFreeStartLocation, newFreeSize);


                }

            }

            return true;

        }

        /// <summary>
        /// Frees an allocated memory block.
        /// </summary>
        /// <param name="allocatedBlock">Allocated memory block to be freed</param>
        /// <remarks>This method does not detect if the allocatedBlock is indeed from this slab. Callers should make sure that the allocatedblock belongs to the right slab.</remarks>
        public virtual void Free(IMemoryBlock allocatedBlock)
        {
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
                        //Yup, so delete it
                        newFreeStartLocation = blockBefore.StartLocation;
                        newFreeSize += blockBefore.Length;
                        RemoveFreeBlock(blockBefore);
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
                        //Yup, delete it
                        newFreeSize += blockAfter.Length;
                        RemoveFreeBlock(blockAfter);
                    }
                }

                //Mark entire contiguous block as free
                AddFreeBlock(newFreeStartLocation.Value, newFreeSize);

            }

            if (GetLargest() == Size)
            {
                //This slab is empty. prod pool to do some cleanup
                if (pool != null)
                {
                    pool.TryFreeSlab();
                }
            }

        }

        /// <summary>
        /// Sets a new value as the largest unallocated contiguous block size
        /// </summary>
        /// <param name="value">Integral value of new largest unallocated block size</param>
        protected void SetLargest(long value)
        {
            if (is64BitMachine)
            {
                //largest = Value;
                Interlocked.Exchange(ref largest, value);
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
        protected void AddFreeBlock(long startLocation, long length)
        {
            SortedDictionary<long, IMemoryBlock> innerList;
            if (!freeBlocksList.TryGetValue(startLocation, out innerList))
            {
                innerList = new SortedDictionary<long, IMemoryBlock>();
                freeBlocksList.Add(length, innerList);
            }

            MemoryBlock newFreeBlock = new MemoryBlock(startLocation, length, this);
            innerList.Add(startLocation, newFreeBlock);
            dictStartLoc.Add(startLocation, newFreeBlock);
            dictEndLoc.Add(startLocation + length - 1, newFreeBlock);
            if (GetLargest() < length)
            {
                SetLargest(length);
            }
        }

        /// <summary>
        /// Marks an unallocated contiguous block as allocated
        /// </summary>
        /// <param name="block">newly allocated block</param>
        protected void RemoveFreeBlock(IMemoryBlock block)
        {
            dictStartLoc.Remove(block.StartLocation);
            dictEndLoc.Remove(block.EndLocation);
            if (freeBlocksList[block.Length].Count == 1)
            {
                freeBlocksList.Remove(block.Length);
                if (GetLargest() == block.Length)
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
            else
            {
                freeBlocksList[block.Length].Remove(block.StartLocation);
            }
        }

        /// <summary>
        /// Gets the largest unallocated contiguous block size in the slab
        /// </summary>
        /// <returns>The size of the largest free contiguous block</returns>
        protected long GetLargest()
        {
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
