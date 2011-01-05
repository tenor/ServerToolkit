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

using System;
using System.Collections.Generic;
using System.Threading;

namespace ServerToolkit.BufferManagement
{
    internal class MemoryBlock : IMemoryBlock
    {

        readonly long startLoc, endLoc, length;
        readonly IMemorySlab owner;

        internal MemoryBlock(long StartLocation, long Length, IMemorySlab Slab)
        {
            if (StartLocation < 0)
            {
                throw new ArgumentOutOfRangeException("StartLocation", "StartLocation must be greater than 0");
            }
            startLoc = StartLocation;

            if (Length <= 0)
            {
                throw new ArgumentOutOfRangeException("Length", "Length must be greater than 0");
            }

            if ((StartLocation + Length - 1) < startLoc)
            {
                throw new ArgumentOutOfRangeException("EndLocation", "EndLocation must be equal or greater than StartLocation");
            }
            endLoc = StartLocation + Length - 1;
            length = Length;
            if (Slab == null) throw new ArgumentNullException("Slab");
            this.owner = Slab;
        }


        public virtual long StartLocation
        {
            get
            {
                return startLoc;
            }
        }

        public virtual long EndLocation
        {
            get
            {
                return endLoc;
            }
        }

        public virtual long Length
        {
            get { return length; }
        }

        public virtual IMemorySlab Slab
        {
            get { return owner; }
        }

    }

    internal class MemorySlab : IMemorySlab
    {
        protected Dictionary<long, IMemoryBlock> dictStartLoc = new Dictionary<long, IMemoryBlock>();
        protected Dictionary<long, IMemoryBlock> dictEndLoc = new Dictionary<long, IMemoryBlock>();
        protected SortedDictionary<long, SortedDictionary<long, IMemoryBlock>> freeBlocksList = new SortedDictionary<long, SortedDictionary<long, IMemoryBlock>>();
        protected object sync = new object();

        protected readonly bool is64BitMachine;
        protected readonly long length;
        protected readonly BufferPool pool;
        protected long largest = 0;
        protected byte[] array;


        internal MemorySlab(long TotalLength, BufferPool Pool)
        {

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
                    AddFreeBlock(0, TotalLength);
                    this.length = TotalLength;
                    this.pool = Pool;
                    array = new byte[TotalLength];
                }
            }
        }

        public long LargestFreeBlockSize
        {
            get { return GetLargest(); }
        }

        public long TotalLength
        {
            get
            {
                return length;
            }
        }

        public byte[] Array
        {
            get
            {
                return array;
            }
        }

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

        protected void SetLargest(long Value)
        {
            if (is64BitMachine)
            {
                largest = Value;
            }
            else
            {
                Interlocked.Exchange(ref largest, Value);
            }

        }

        public virtual bool TryAllocate(long Size, out IMemoryBlock AllocatedBlock)
        {
            AllocatedBlock = null;
            lock (sync)
            {
                if (GetLargest() < Size) return false;

                //search freeBlocksList looking for the smallest available free block
                long[] keys = new long[freeBlocksList.Count];
                freeBlocksList.Keys.CopyTo(keys, 0);
                int index = System.Array.BinarySearch<long>(keys, Size);
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
                foreach (KeyValuePair<long,IMemoryBlock> kvPair in freeBlocksList[keys[index]])
                {
                    foundBlock = kvPair.Value;
                    break;
                }

                //Remove existing free block
                RemoveFreeBlock(foundBlock);


                if (foundBlock.Length == Size)
                {
                    //Perfect match
                    AllocatedBlock = foundBlock;
                }
                else
                {
                    //FoundBlock is larger than requested block size

                    AllocatedBlock = new MemoryBlock(foundBlock.StartLocation, Size, this);

                    long newFreeStartLocation = AllocatedBlock.EndLocation + 1;
                    long newFreeSize = foundBlock.Length - Size;

                    //add new Freeblock with smaller space
                    AddFreeBlock(newFreeStartLocation, newFreeSize);


                }

            }

            return true;

        }

        protected void AddFreeBlock(long StartLocation, long Length)
        {
            SortedDictionary<long, IMemoryBlock> innerList;
            if (!freeBlocksList.TryGetValue(StartLocation, out innerList))
            {
                innerList = new SortedDictionary<long, IMemoryBlock>();
                freeBlocksList.Add(Length, innerList);
            }

            MemoryBlock newFreeBlock = new MemoryBlock(StartLocation, Length, this);
            innerList.Add(StartLocation, newFreeBlock);
            dictStartLoc.Add(StartLocation, newFreeBlock);
            dictEndLoc.Add(StartLocation + Length - 1, newFreeBlock);
            if (GetLargest() < Length)
            {
                SetLargest(Length);
            }
        }

        protected void RemoveFreeBlock(IMemoryBlock Block)
        {
            dictStartLoc.Remove(Block.StartLocation);
            dictEndLoc.Remove(Block.EndLocation);
            if (freeBlocksList[Block.Length].Count == 1)
            {
                freeBlocksList.Remove(Block.Length);
                if (GetLargest() == Block.Length)
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
                freeBlocksList[Block.Length].Remove(Block.StartLocation);
            }
        }

        public virtual void Free(IMemoryBlock AllocatedBlock)
        {
            lock (sync)
            {

                //TODO: Roll commected out code into a test and have it check all startlocs and endlocs to make sure there was no overlap
                //Verify that Allocated block was truly allocated
                /*
                IMemoryBlock foundBlock;
                if ((dictStartLoc.TryGetValue(AllocatedBlock.StartLocation, out foundBlock)) || foundBlock.Length != AllocatedBlock.Length)
                {
                    throw new InvalidOperationException("AllocatedBlock was not found in MemorySlab");
                }
                 */

                long? newFreeStartLocation = null;
                long newFreeSize = 0;

                if (AllocatedBlock.StartLocation > 0)
                {

                    //Check if block before this one is free

                    IMemoryBlock blockBefore;
                    if (dictEndLoc.TryGetValue(AllocatedBlock.StartLocation - 1, out blockBefore))
                    {
                        //Yup, so delete it
                        newFreeStartLocation = blockBefore.StartLocation;
                        newFreeSize += blockBefore.Length;
                        RemoveFreeBlock(blockBefore);
                    }

                }

                //Include  AllocatedBlock
                if (!newFreeStartLocation.HasValue) newFreeStartLocation = AllocatedBlock.StartLocation;
                newFreeSize += AllocatedBlock.Length;

                if (AllocatedBlock.EndLocation + 1 < TotalLength)
                {
                    // Check if block after this one is free
                    IMemoryBlock blockAfter;
                    if (dictStartLoc.TryGetValue(AllocatedBlock.EndLocation + 1, out blockAfter))
                    {
                        //Yup, delete it
                        newFreeSize += blockAfter.Length;
                        RemoveFreeBlock(blockAfter);
                    }
                }

                //Mark entire contiguous block as free
                AddFreeBlock(newFreeStartLocation.Value, newFreeSize);

            }

            if (GetLargest() == TotalLength)
            {
                //This slab is empty. prod pool to do some cleanup
                if (pool != null)
                {
                    pool.TryFreeSlab();
                }
            }

        }
    }
}
