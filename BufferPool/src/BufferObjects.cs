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
    public class ManagedBuffer : IBuffer
    {
        protected bool disposed = false;
        internal IMemoryBlock memoryBlock;
        byte[] slabArray;

        internal ManagedBuffer(IMemoryBlock AllocatedMemoryBlock)
        {
            if (AllocatedMemoryBlock == null) throw new ArgumentNullException("AllocatedMemoryBlock");
            memoryBlock = AllocatedMemoryBlock;
            slabArray = null;
        }


        //Used for Creating an empty (zero-length) buffer
        internal ManagedBuffer(byte[] SlabArray)
        {
            if (SlabArray == null) throw new ArgumentNullException("SlabArray");
            memoryBlock = null;
            slabArray = SlabArray;
        }

        //NOTE: This overload cannot return segments larger than int.MaxValue;
        //TODO: MULTI_ARRAY_SEGMENTS: NOTE: This method should be able to accept length > int.MaxValue after implementing multi-array-segments
        public virtual IList<ArraySegment<byte>> GetArraySegments()
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            //TODO: MULTI_ARRAY_SEGMENTS: NOTE: This int.MaxValue should be removed after implementing multi-array-segments
            if (this.Length <= int.MaxValue)
            {
                return GetArraySegments(0, (int)this.Length);
            }
            else
            {
                return GetArraySegments(0, int.MaxValue);
            }
            
        }

        public virtual IList<ArraySegment<byte>> GetArraySegments(long Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            return GetArraySegments(0, Length);
        }

        public IList<ArraySegment<byte>> GetArraySegments(long Offset, long Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (Length > this.Length || Length < 0)
            {
                throw new ArgumentOutOfRangeException("Length");
            }
            if (Offset > this.Length || Offset < 0)
            {
                throw new ArgumentOutOfRangeException("Offset");
            }

            IList<ArraySegment<byte>> result = new List<ArraySegment<byte>>();
            if (this.Length == 0)
            {
                result.Add(new ArraySegment<byte>(slabArray, 0, 0));
                return result;
            }
            else
            {
                //TODO: MULTI_ARRAY_SEGMENTS: NOTE: This exception should not take place after implementing multi-array-segments
                // and a limit to SlabSize (MaximumSlabSize) is in place, which would probably be int.MaxValue;
                if (Offset + memoryBlock.StartLocation > int.MaxValue)
                {
                    throw new InvalidOperationException("ArraySegment location exceeds int.MaxValue");
                }
                result.Add(new ArraySegment<byte>(memoryBlock.Slab.Array, (int)(Offset + memoryBlock.StartLocation), (int)Length));
                return result;
            }
        }


        public void CopyTo(byte[] DestinationArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            CopyTo(DestinationArray, 0, this.Length);
        }

        public void CopyTo(byte[] DestinationArray, long DestinationIndex, long Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (Length > this.Length) throw new ArgumentException("Buffer length is greater than Destination array length");
            if (this.Length == 0) return;

            Array.Copy(memoryBlock.Slab.Array, memoryBlock.StartLocation, DestinationArray, DestinationIndex, Length);
        }

        public void CopyFrom(byte[] SourceArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            CopyFrom(SourceArray, 0, SourceArray.Length);
        }

        public void CopyFrom(byte[] SourceArray, long SourceIndex, long Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (Length > (SourceIndex + this.Length)) throw new ArgumentException("Source array length is less than buffer length");
            if (this.Length == 0) return;

            Array.Copy(SourceArray, SourceIndex, memoryBlock.Slab.Array, memoryBlock.StartLocation, Length);
        }

        public bool IsDisposed
        {
            get { return disposed; }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                try
                {
                    if (memoryBlock != null)
                    {
                        memoryBlock.Slab.Free(memoryBlock);
                    }
                }
                catch
                {
                    //Suppress exception in release mode
                    #if DEBUG                    
                        throw;
                    #endif
                }

            }
        }

        public long Length
        {
            get { return memoryBlock == null ? 0 : memoryBlock.Length; }
        }

    }

    public class BufferPool : IBufferPool
    {
        public const int MinimumSlabSize = 92160; //90 KB to force slab into LOH

        private long slabSize;
        private int initialSlabs, subsequentSlabs;
        private object sync_slabList = new object(); //synchronizes access to the array of slabs
        private object sync_newSlab = new object(); //synchronizes access to new slab creation
        private List<IMemorySlab> slabs = new List<IMemorySlab>();
        private readonly IMemorySlab firstSlab;
        private int singleSlabPool; //-1 or 0, used for faster access if only one slab is available

        public BufferPool(long SlabSize, int InitialSlabs, int SubsequentSlabs)
        {

            if (SlabSize < 1) throw new ArgumentException("SlabSize must be equal to or greater than 1");
            if (InitialSlabs < 1) throw new ArgumentException("InitialSlabs must be equal to or greater than 1");
            if (SubsequentSlabs < 1) throw new ArgumentException("SubsequentSlabs must be equal to or greater than 1");

            this.slabSize = SlabSize > MinimumSlabSize ? SlabSize : MinimumSlabSize;
            this.initialSlabs = InitialSlabs;
            this.subsequentSlabs = SubsequentSlabs;

            lock (sync_slabList)
            {
                if (slabs.Count == 0)
                {
                    if (initialSlabs > 1)
                    {
                        Interlocked.Exchange(ref singleSlabPool, 0); //false
                    }
                    else
                    {
                        Interlocked.Exchange(ref singleSlabPool, -1); //true
                    }

                    for (int i = 0; i < initialSlabs; i++ )
                    {
                        slabs.Add(new MemorySlab(slabSize, this));
                    }

                    firstSlab = slabs[0];
                }
            }
        }

        public IBuffer GetBuffer(long Length)
        {

            if (Length < 0) throw new ArgumentException("Length must be greater than 0");

            if (Length == 0) return new ManagedBuffer(firstSlab.Array); //Return an empty buffer

            IMemoryBlock allocatedBlock;
            IMemorySlab[] slabArr;

            if (singleSlabPool == -1)
            {
                //Optimization: Chances are that there'll be just one slab in a pool, so access it directly 
                //and avoid the lock statement involved while creating an array of slabs.

                //Note that even if singleSlabPool is inaccurate, this method will still work properly.
                //The optimization is effective because singleSlabPool will be accurate majority of the time.

                slabArr = new IMemorySlab[] { firstSlab };
                if (TryAllocateBlockInSlabs(Length, slabArr, out allocatedBlock))
                {
                    return new ManagedBuffer(allocatedBlock);
                }

                Interlocked.Exchange(ref singleSlabPool, 0); // Slab count will soon be incremented
            }
            else
            {

                lock (sync_slabList)
                {
                    slabArr = slabs.ToArray();
                }

                if (TryAllocateBlockInSlabs(Length, slabArr, out allocatedBlock))
                {
                    return new ManagedBuffer(allocatedBlock);
                }
            }


            lock (sync_newSlab)
            {
                //Look again for free block
                lock (sync_slabList)
                {
                    slabArr = slabs.ToArray();
                }
                
                if (TryAllocateBlockInSlabs(Length, slabArr, out allocatedBlock))
                {
                    //found it -- leave
                    return new ManagedBuffer(allocatedBlock);
                }

                //Unable to find available free space, so create new slab
                MemorySlab newSlab = new MemorySlab(slabSize, this);

                newSlab.TryAllocate(Length, out allocatedBlock);

                lock (sync_slabList)
                {
                    //Add new Slab to collection

                    slabs.Add(newSlab);

                    //Add extra slabs as requested in object properties
                    for (int i = 0; i < subsequentSlabs - 1; i++)
                    {
                        slabs.Add(new MemorySlab(slabSize, this));
                    }
                }

            }

            return new ManagedBuffer(allocatedBlock);

        }


        //Helper method that searches for free block in an array of slabs and returns the allocated block
        private static bool TryAllocateBlockInSlabs(long Length, IMemorySlab[] Slabs, out IMemoryBlock allocatedBlock)
        {
            allocatedBlock = null;
            for (int i = 0; i < Slabs.Length; i++)
            {
                if (Slabs[i].LargestFreeBlockSize >= Length)
                {
                    if (Slabs[i].TryAllocate(Length, out allocatedBlock))
                    {                        
                        return true;
                    }
                }
            }

            return false;
        }


        internal void TryFreeSlab()
        {
            lock (sync_slabList)
            {
                int emptySlabsCount = 0;
                int lastemptySlab = -1;
                for (int i = 0; i < slabs.Count; i++)
                {
                    if (slabs[i].LargestFreeBlockSize == slabSize)
                    {
                        emptySlabsCount++;
                        lastemptySlab = i;
                    }
                }

                if (emptySlabsCount > 1) //There should be at least two empty slabs before one is removed
                {
                    //remove the last empty one
                    slabs.RemoveAt(lastemptySlab);

                    if (slabs.Count == 1) Interlocked.Exchange(ref singleSlabPool, -1);
                }

            }
        }

        //Returns number of slabs
        //Strictly for testing
        internal long SlabCount
        {
            get { return slabs.Count; }
        }


        public int InitialSlabs
        {
            get { return initialSlabs; }
        }

        public int SubsequentSlabs
        {
            get { return subsequentSlabs ; }
        }

        public long SlabSize
        {
            get { return slabSize; }
        }

    }
}
