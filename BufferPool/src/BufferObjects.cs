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

namespace ServerToolkit.BufferManagement
{
    public class Buffer : IBuffer
    {
        protected bool disposed;
        internal IMemoryBlock memoryBlock;

        internal Buffer(IMemoryBlock AllocatedMemoryBlock)
        {
            memoryBlock = AllocatedMemoryBlock;
            disposed = false;
        }

        //NOTE: This overload cannot return segments larger than int.MaxValue;
        public virtual ArraySegment<byte> GetArraySegment()
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            if (memoryBlock.Length <= int.MaxValue)
            {
                return GetArraySegment(0, (int)memoryBlock.Length);
            }
            else
            {
                return GetArraySegment(0, int.MaxValue);
            }
            
        }

        public virtual ArraySegment<byte> GetArraySegment(int Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            return GetArraySegment(0, Length);
        }

        public ArraySegment<byte> GetArraySegment(int Offset, int Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (Length > memoryBlock.Length)
            {
                throw new ArgumentOutOfRangeException("Length");
            }
            if (Offset > memoryBlock.Length)
            {
                throw new ArgumentOutOfRangeException("Offset");
            }

            return new ArraySegment<byte>(memoryBlock.Slab.Array, Offset, Length);
        }


        public void CopyTo(byte[] DestinationArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            CopyTo(DestinationArray, 0, memoryBlock.Length);
        }

        public void CopyTo(byte[] DestinationArray, long DestinationIndex, long Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (Length > memoryBlock.Length) throw new ArgumentOutOfRangeException("Length"); 

            Array.Copy(memoryBlock.Slab.Array, 0, DestinationArray, DestinationIndex, Length);
        }

        public void CopyFrom(byte[] SourceArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            CopyFrom(SourceArray, 0, SourceArray.Length);
        }

        public void CopyFrom(byte[] SourceArray, long SourceIndex, long Length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (Length > (SourceIndex + memoryBlock.Length)) throw new ArgumentOutOfRangeException("Length");

            Array.Copy(SourceArray, SourceIndex, memoryBlock.Slab.Array, 0, Length);
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
                    memoryBlock.Slab.Free(memoryBlock);
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
            get { return memoryBlock.Length; }
        }



    }

    public class BufferPool : IBufferPool
    {
        public const int MinimumSlabSize = 92160; //90 KB to force slab into LOH

        private long slabSize;
        private int initialSlabs, subsequentSlabs;
        private object sync = new object();
        private bool disposed;
        private List<IMemorySlab> slabs = new List<IMemorySlab>();

        public BufferPool(long SlabSize, int InitialSlabs, int SubsequentSlabs)
        {
            if (InitialSlabs < 1) throw new ArgumentOutOfRangeException("InitialSlabs");
            if (SubsequentSlabs < 1) throw new ArgumentOutOfRangeException("SubsequentSlabs");

            this.slabSize = SlabSize > MinimumSlabSize ? SlabSize : MinimumSlabSize;
            this.initialSlabs = InitialSlabs;
            this.subsequentSlabs = SubsequentSlabs;

            lock (sync)
            {
                if (slabs.Count == 0)
                {
                    for (int i = 0; i < initialSlabs; i++ )
                    {
                        slabs.Add(new MemorySlab(slabSize, this));
                    }
                }
            }
        }

        public IBuffer GetBuffer(long Size)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            IMemorySlab[] slabArr;
            lock (sync)
            {
                slabArr = slabs.ToArray();
            }

            IMemoryBlock allocatedBlock;
            for (int i = 0; i < slabArr.Length; i++)
            {
                if (slabArr[i].LargestFreeBlockSize >= Size)
                {                    
                    if (slabArr[i].TryAllocate(Size, out allocatedBlock))
                    {
                        return new Buffer(allocatedBlock);
                    }
                }
            }

            //Unable to find available free space, so create new slab
            MemorySlab newSlab = new MemorySlab(slabSize, this);

            newSlab.TryAllocate(Size, out allocatedBlock);

            //Add new Slab to collection
            lock (sync)
            {
                slabs.Add(newSlab);

                //Add extra slabs as requested in object properties
                for (int i = 0; i < subsequentSlabs - 1; i++)
                {
                    slabs.Add(new MemorySlab(slabSize, this));
                }
            }

            return new Buffer(allocatedBlock);

        }

        internal void TryFreeSlab()
        {
            lock (sync)
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
                }
            }
        }


        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                lock (sync)
                {
                    slabs.Clear();
                }
            }
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
