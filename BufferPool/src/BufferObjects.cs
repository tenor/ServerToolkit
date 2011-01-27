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
    /// Represents an allocated buffer that is managed within a buffer pool.
    /// </summary>
    public class ManagedBuffer : IBuffer
    {

        internal IMemoryBlock memoryBlock;
        protected bool disposed = false;
        byte[] slabArray;

        internal ManagedBuffer(IMemoryBlock allocatedMemoryBlock)
        {
            if (allocatedMemoryBlock == null) throw new ArgumentNullException("AllocatedMemoryBlock");
            memoryBlock = allocatedMemoryBlock;
            slabArray = null;
        }


        //Used for Creating an empty (zero-length) buffer
        internal ManagedBuffer(byte[] slabArray)
        {
            if (slabArray == null) throw new ArgumentNullException("SlabArray");
            memoryBlock = null;
            this.slabArray = slabArray;
        }


        public bool IsDisposed
        {
            get { return disposed; }
        }

        public long Size
        {
            get { return memoryBlock == null ? 0 : memoryBlock.Length; }
        }

        public int SegmentCount
        {
            get { return 1; /*Always 1 for now */ }
        }


        //NOTE: This overload cannot return segments larger than int.MaxValue;
        //TODO: MULTI_ARRAY_SEGMENTS: NOTE: This method should be able to accept length > int.MaxValue after implementing multi-array-segments
        public virtual IList<ArraySegment<byte>> GetSegments()
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            //TODO: MULTI_ARRAY_SEGMENTS: NOTE: This int.MaxValue should be removed after implementing multi-array-segments
            if (this.Size <= int.MaxValue)
            {
                return GetSegments(0, (int)this.Size);
            }
            else
            {
                return GetSegments(0, int.MaxValue);
            }
            
        }

        public virtual IList<ArraySegment<byte>> GetSegments(long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            return GetSegments(0, length);
        }

        public IList<ArraySegment<byte>> GetSegments(long offset, long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (length > this.Size || length < 0)
            {
                throw new ArgumentOutOfRangeException("Length");
            }

            if (offset > this.Size || offset < 0)
            {
                throw new ArgumentOutOfRangeException("Offset");
            }

            IList<ArraySegment<byte>> result = new List<ArraySegment<byte>>();
            if (this.Size == 0)
            {
                result.Add(new ArraySegment<byte>(slabArray, 0, 0));
                return result;
            }
            else
            {
                //TODO: MULTI_ARRAY_SEGMENTS: NOTE: This exception should not take place after implementing multi-array-segments
                // and a limit to SlabSize (MaximumSlabSize) is in place, which would probably be int.MaxValue;
                if (offset + memoryBlock.StartLocation > int.MaxValue)
                {
                    throw new InvalidOperationException("ArraySegment location exceeds int.MaxValue");
                }

                result.Add(new ArraySegment<byte>(memoryBlock.Slab.Array, (int)(offset + memoryBlock.StartLocation), (int)length));
                return result;
            }
        }


        public void CopyTo(byte[] destinationArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            CopyTo(destinationArray, 0, this.Size);
        }

        public void CopyTo(byte[] destinationArray, long destinationIndex, long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (destinationArray == null) throw new ArgumentNullException("destinationArray");
            if (length > this.Size) throw new ArgumentException("Buffer length is greater than Destination array length");
            if (this.Size == 0) return;

            Array.Copy(memoryBlock.Slab.Array, memoryBlock.StartLocation, destinationArray, destinationIndex, length);
        }

        public void CopyFrom(byte[] sourceArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            CopyFrom(sourceArray, 0, sourceArray.Length);
        }

        public void CopyFrom(byte[] sourceArray, long sourceIndex, long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (sourceArray == null) throw new ArgumentNullException("sourceArray");            
            if (length > (sourceIndex + this.Size)) throw new ArgumentException("Source array length is less than buffer length");
            if (this.Size == 0) return;

            Array.Copy(sourceArray, sourceIndex, memoryBlock.Slab.Array, memoryBlock.StartLocation, length);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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
        }

    }

    /// <summary>
    /// Represents a buffer pool implementation
    /// </summary>
    public class BufferPool : IBufferPool
    {
        public const int MinimumSlabSize = 92160; //90 KB to force slab into LOH
        private readonly IMemorySlab firstSlab;

        private long slabSize;
        private int initialSlabs, subsequentSlabs;
        private object sync_slabList = new object(); //synchronizes access to the array of slabs
        private object sync_newSlab = new object(); //synchronizes access to new slab creation
        private List<IMemorySlab> slabs = new List<IMemorySlab>();
        private int singleSlabPool; //-1 or 0, used for faster access if only one slab is available

        public BufferPool(long slabSize, int initialSlabs, int subsequentSlabs)
        {

            if (slabSize < 1) throw new ArgumentException("SlabSize must be equal to or greater than 1");
            if (initialSlabs < 1) throw new ArgumentException("InitialSlabs must be equal to or greater than 1");
            if (subsequentSlabs < 1) throw new ArgumentException("SubsequentSlabs must be equal to or greater than 1");

            this.slabSize = slabSize > MinimumSlabSize ? slabSize : MinimumSlabSize;
            this.initialSlabs = initialSlabs;
            this.subsequentSlabs = subsequentSlabs;

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

                    for (int i = 0; i < initialSlabs; i++)
                    {
                        slabs.Add(new MemorySlab(slabSize, this));
                    }

                    firstSlab = slabs[0];
                }
            }
        }


        public int InitialSlabs
        {
            get { return initialSlabs; }
        }

        public int SubsequentSlabs
        {
            get { return subsequentSlabs; }
        }

        public long SlabSize
        {
            get { return slabSize; }
        }

        //Returns number of slabs
        //Strictly for testing
        internal long SlabCount
        {
            get { return slabs.Count; }
        }

        public IBuffer GetBuffer(long size)
        {

            if (size < 0) throw new ArgumentException("Length must be greater than 0");

            if (size == 0) return new ManagedBuffer(firstSlab.Array); //Return an empty buffer

            IMemoryBlock allocatedBlock;
            IMemorySlab[] slabArr;

            if (singleSlabPool == -1)
            {
                //Optimization: Chances are that there'll be just one slab in a pool, so access it directly 
                //and avoid the lock statement involved while creating an array of slabs.

                //Note that even if singleSlabPool is inaccurate, this method will still work properly.
                //The optimization is effective because singleSlabPool will be accurate majority of the time.

                slabArr = new IMemorySlab[] { firstSlab };
                if (TryAllocateBlockInSlabs(size, slabArr, out allocatedBlock))
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

                if (TryAllocateBlockInSlabs(size, slabArr, out allocatedBlock))
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
                
                if (TryAllocateBlockInSlabs(size, slabArr, out allocatedBlock))
                {
                    //found it -- leave
                    return new ManagedBuffer(allocatedBlock);
                }

                //Unable to find available free space, so create new slab
                MemorySlab newSlab = new MemorySlab(slabSize, this);

                newSlab.TryAllocate(size, out allocatedBlock);

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

                if (emptySlabsCount > InitialSlabs) //There should be at least 1+initial slabs empty slabs before one is removed
                {
                    //remove the last empty one
                    slabs.RemoveAt(lastemptySlab);

                    if (slabs.Count == 1) Interlocked.Exchange(ref singleSlabPool, -1);
                }

            }
        }

        //Helper method that searches for free block in an array of slabs and returns the allocated block
        private static bool TryAllocateBlockInSlabs(long length, IMemorySlab[] slabs, out IMemoryBlock allocatedBlock)
        {
            allocatedBlock = null;
            for (int i = 0; i < slabs.Length; i++)
            {
                if (slabs[i].LargestFreeBlockSize >= length)
                {
                    if (slabs[i].TryAllocate(length, out allocatedBlock))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


    }
}
