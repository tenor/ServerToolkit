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
    /// Represents an efficiently allocated buffer for asynchronous read/write operations.
    /// </summary>
    public sealed class ManagedBuffer : IBuffer
    {
        private bool disposed = false;

        IList<IMemoryBlock> memoryBlocks;
        byte[] slabArray;
        readonly long size;

        /// <summary>
        /// Initializes a new instance of the ManagedBuffer class, specifying the memory block that the ManagedBuffer reads and writes to.
        /// </summary>
        /// <param name="allocatedMemoryBlocks">Underlying allocated memory block</param>
        internal ManagedBuffer(IList<IMemoryBlock> allocatedMemoryBlocks)
        {
            if (allocatedMemoryBlocks == null) throw new ArgumentNullException("allocatedMemoryBlocks");
            if (allocatedMemoryBlocks.Count == 0) throw new ArgumentException("allocatedMemoryBlocks cannot be empty"); 
            memoryBlocks = allocatedMemoryBlocks;
            size = 0;
            for (int i = 0; i < allocatedMemoryBlocks.Count; i++)
            {
                size += allocatedMemoryBlocks[i].Length;
            }
            slabArray = null;
        }


        /// <summary>
        /// Initializes a new instance of the ManagedBuffer class, specifying the slab to be associated with the ManagedBuffer.
        /// This constructor creates an empty (zero-length) buffer.
        /// </summary>
        /// <param name="slab">The Memory Slab to be associated with the ManagedBuffer</param>
        internal ManagedBuffer(IMemorySlab slab)
        {
            if (slab == null) throw new ArgumentNullException("slab");
            memoryBlocks = null;
            this.slabArray = slab.Array;
            size = 0;
        }


        /// <summary>
        /// Gets a value indicating whether the buffer is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return disposed; }
        }

        /// <summary>
        /// Gets the total size of the buffer, in bytes.
        /// </summary>        
        public long Size
        {
            get { return size; }
        }

        /// <summary>
        /// Gets the number of segments in the buffer.
        /// </summary>
        public int SegmentCount
        {
            get { return memoryBlocks == null ? 1 : memoryBlocks.Count; }
        }

        /// <summary>
        /// Gets the underlying memory block(s)
        /// </summary>
        /// <remarks>This property is provided for testing purposes</remarks>
        internal IList<IMemoryBlock> MemoryBlocks
        {
            get { return memoryBlocks ?? new List<IMemoryBlock>(); }
        } 


        //NOTE: This overload cannot return segments larger than int.MaxValue;
        //TODO: MULTI_ARRAY_SEGMENTS: NOTE: This method should be able to accept length > int.MaxValue after implementing multi-array-segments

        /// <summary>
        /// Gets buffer segments that can be passed on to an asynchronous socket operation.
        /// </summary>
        /// <returns>A list of ArraySegments(of Byte) containing buffer segments.</returns>
        public IList<ArraySegment<byte>> GetSegments()
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            return GetSegments(0, this.Size);            
        }

        /// <summary>
        /// Gets buffer segments that can be passed on to an asynchronous socket operation.
        /// </summary>
        /// <param name="length">Total length of segments.</param>
        /// <returns>A list of ArraySegments(of Byte) containing buffer segments.</returns>
        public IList<ArraySegment<byte>> GetSegments(long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            return GetSegments(0, length);
        }

        /// <summary>
        /// Gets buffer segments that can be passed on to an asynchronous socket operation.
        /// </summary>
        /// <param name="offset">Offset in the buffer where segments start.</param>
        /// <param name="length">Total length of segments.</param>
        /// <returns>A list of ArraySegments(of Byte) containing buffer segments.</returns>
        public IList<ArraySegment<byte>> GetSegments(long offset, long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (length > this.Size || length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            if ((offset >= this.Size && this.Size != 0) || offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            //TODO: Check if offset + length > this.Size

            IList<ArraySegment<byte>> result = new List<ArraySegment<byte>>();
            if (this.Size == 0)
            {
                result.Add(new ArraySegment<byte>(slabArray, 0, 0));
                return result;
            }
            else
            {
                //Identify which memory block contains the index to offset and the block's inner offset to sought offset                
                int startBlockIndex;
                long startBlockOffSet;
                FindBlockWithOffset(offset, out startBlockIndex, out startBlockOffSet);

                //Get first segment
                long totalLength = 0;
                {
                    IMemoryBlock startBlock = memoryBlocks[startBlockIndex];
                    if (startBlock.Length >= (startBlockOffSet + length))
                    {
                        //Block can hold entire desired length
                        result.Add(new ArraySegment<byte>(startBlock.Slab.Array, (int)(startBlockOffSet + startBlock.StartLocation), (int)length));
                        return result;
                    }
                    else
                    {
                        //Block can only hold part of desired length
                        result.Add(new ArraySegment<byte>(startBlock.Slab.Array, (int)(startBlockOffSet + startBlock.StartLocation), (int)(startBlock.Length - startBlockOffSet)));
                        totalLength += (startBlock.Length - startBlockOffSet);
                    }
                }

                //Get next set of segments
                IMemoryBlock block;
                for (int i = startBlockIndex + 1; i < memoryBlocks.Count; i++)
                {
                    block = memoryBlocks[i];
                    if (block.Length >= (length - totalLength))
                    {
                        //Block can hold the remainder of desired length
                        result.Add(new ArraySegment<byte>(block.Slab.Array, (int)(block.StartLocation), (int)(length - totalLength)));
                        return result;
                    }
                    else
                    {
                        //Block can only hold only part of the remainder of desired length
                        result.Add(new ArraySegment<byte>(block.Slab.Array, (int)(block.StartLocation), (int)block.Length));
                        totalLength += block.Length;
                    }

                }

                System.Diagnostics.Debug.Assert(true, "Execution should never reach this point, the returns above should be responsible for returning result");
                return result;
            }
        }

        /// <summary>
        /// Copies data from the buffer to a byte array.
        /// </summary>
        /// <param name="destinationArray">The one-dimensional byte array which receives the data.</param>
        /// <remarks>The size of the buffer must be less than or equal to the destinationArray length.</remarks>
        public void CopyTo(byte[] destinationArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            CopyTo(destinationArray, 0, this.Size);
        }


        //TODO: Write a version of CopyTo with a sourceIndex: CopyTo(long index, byte[] array, long arrayIndex, long length)
        //TODO: Rename the parameters of CopyTo (see List.CopyTo as a guideline)

        /// <summary>
        /// Copies data from the buffer to a byte array
        /// </summary>
        /// <param name="destinationArray">The one-dimensional byte array which receives the data.</param>
        /// <param name="destinationIndex">The index in the destinationArray at which storing begins.</param>
        /// <param name="length">The number of bytes to copy.</param>
        public void CopyTo(byte[] destinationArray, long destinationIndex, long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (destinationArray == null) throw new ArgumentNullException("destinationArray");
            if (length > this.Size) throw new ArgumentException("length is larger than buffer size");
            if (destinationIndex + length > destinationArray.Length) throw new ArgumentException("destinationIndex + length is greater than length of destinationArray");
            if (this.Size == 0) return;

            long bytesCopied = 0;
            IMemoryBlock block;
            for (int i = 0; i < memoryBlocks.Count; i++)
            {
                block = memoryBlocks[i];
                if (block.Length >= (length - bytesCopied))
                {
                    //This block can copy out the remainder of desired length
                    Array.Copy(block.Slab.Array, block.StartLocation, destinationArray, destinationIndex + bytesCopied, length - bytesCopied);
                    return;
                }
                else
                {
                    //This block can only copy out part of desired length
                    Array.Copy(block.Slab.Array, block.StartLocation, destinationArray, destinationIndex + bytesCopied, block.Length);
                    bytesCopied += block.Length;
                }
            }

            System.Diagnostics.Debug.Assert(true, "Execution should never reach this point, the returns above should be responsible for exiting the method");
        }

        /// <summary>
        /// Copies data from a byte array into the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <remarks>The length of the sourceArray must be less than or equal to the buffer size.</remarks>
        [Obsolete("Use the FillWith method instead -- this method will be removed in a later version", true)]
        public void CopyFrom(byte[] sourceArray)
        {
            FillWith(sourceArray);
        }

        /// <summary>
        /// Copies data from a byte array into the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <param name="sourceIndex">The index in the sourceArray at which copying begins.</param>
        /// <param name="length">The number of bytes to copy.</param>
        [Obsolete("Use the FillWith method instead -- this method will be removed in a later version", true)]
        public void CopyFrom(byte[] sourceArray, long sourceIndex, long length)
        {
            FillWith(sourceArray, sourceIndex, length);
        }

        /// <summary>
        /// Copies data from a byte array into the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <remarks>The length of the sourceArray must be less than or equal to the buffer size.</remarks>
        public void FillWith(byte[] sourceArray)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());

            FillWith(sourceArray, 0, sourceArray.Length);
        }

        //TODO: Write a version of FIllWith with a destinationIndex: FillWith(long index, byte[] array, long arrayIndex, long length)
        //TODO: Rename the parameters of FillWith (see List.CopyTo as a guideline)

        /// <summary>
        /// Copies data from a byte array into the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <param name="sourceIndex">The index in the sourceArray at which copying begins.</param>
        /// <param name="length">The number of bytes to copy.</param>
        public void FillWith(byte[] sourceArray, long sourceIndex, long length)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            if (sourceArray == null) throw new ArgumentNullException("sourceArray");
            if (length > (this.Size - sourceIndex)) throw new ArgumentException("length will not fit in the buffer");
            if (this.Size == 0) return;

            //NOTE: try not to keep this method as simple as possible, it's can be called from IBuffer.GetBuffer
            //and we do not want new unexpected exceptions been thrown.

            long bytesCopied = 0;
            IMemoryBlock block;
            for (int i = 0; i < memoryBlocks.Count; i++)
            {
                block = memoryBlocks[i];
                if (block.Length >= (length - bytesCopied))
                {
                    //This block can be filled with the remainder of desired length
                    Array.Copy(sourceArray, sourceIndex + bytesCopied, block.Slab.Array, block.StartLocation, length - bytesCopied);
                    return;
                }
                else
                {
                    //This block can be filled with only part of desired length
                    Array.Copy(sourceArray, sourceIndex + bytesCopied, block.Slab.Array, block.StartLocation, block.Length);
                    bytesCopied += block.Length;
                }
            }

            System.Diagnostics.Debug.Assert(true, "Execution should never reach this point, the returns above should be responsible for exiting the method");

        }

        /// <summary>
        /// Releases resources used by the buffer.
        /// </summary>
        /// <remarks>This method frees the memory blocks used by the buffer.</remarks>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases resources used by the buffer.
        /// </summary>
        /// <param name="disposing">True, to indicate you want to release all resources. False to release only native resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    disposed = true;

                    if (memoryBlocks != null)
                    {
                        for (int i = 0; i < memoryBlocks.Count; i++)
                        {
                            try
                            {
                                memoryBlocks[i].Slab.Free(memoryBlocks[i]);
                            }
                            catch
                            {
                                //Suppress exception in release mode
                                #if DEBUG
                                    throw;
                                #endif
                            }
                        }

                        //Remove all references to memory blocks and their underlying slabs/arrays
                        memoryBlocks = null;
                    }

                    if (slabArray != null) slabArray = null;
                }
            }
        }

        //This helper method identifies the block that holds an 'outer' offset and the inner offset within that block
        private void FindBlockWithOffset(long offset, out int blockIndex, out long blockOffSet)
        {
            long totalScannedLength = 0;
            blockIndex = 0;
            blockOffSet = 0;
            for (int i = 0; i < memoryBlocks.Count; i++)
            {
                if (offset < totalScannedLength + memoryBlocks[i].Length)
                {
                    //Found block;
                    blockIndex = i;
                    //Calculate start offset within block
                    blockOffSet = offset - totalScannedLength;
                    break;
                }
                totalScannedLength += memoryBlocks[i].Length;
            }
        }

    }

    /// <summary>
    /// Provides a pool of buffers that can be used to efficiently allocate memory for asynchronous socket operations
    /// </summary>
    public sealed class BufferPool : IBufferPool
    {
        /// <summary>
        /// The minimum size of a slab.
        /// </summary>
        public const int MinimumSlabSize = 92160; //90 KB to force slab into LOH

        // The MaximumSlabSize restriction is in place because of ArraySegment's (T[], int, int) constructor.
        // If a slab exceeds the MaximumSlabSize, an array segment cannot access data beyond the int.MaxValue location

        /// <summary>
        /// The maximum size of a slab.
        /// </summary>
        public const long MaximumSlabSize = ((long)int.MaxValue) + 1; 

        private readonly IMemorySlab firstSlab;
        private readonly long slabSize;
        private readonly int initialSlabs, subsequentSlabs;
        private readonly object syncSlabList = new object(); //synchronizes access to the array of slabs
        private readonly object syncNewSlab = new object(); //synchronizes access to new slab creation
        private readonly List<IMemorySlab> slabs = new List<IMemorySlab>();
        private int singleSlabPool; //-1 or 0, used for faster access if only one slab is available

        private const int MAX_SEGMENTS_PER_BUFFER = 16; //Maximum number of segments in a buffer.

        /// <summary>
        /// Initializes a new instance of the BufferPool class
        /// </summary>
        /// <param name="slabSize">Length, in bytes, of a slab in the BufferPool</param>
        /// <param name="initialSlabs">Number of slabs to create initially</param>
        /// <param name="subsequentSlabs">Number of additional slabs to create at a time</param>
        public BufferPool(long slabSize, int initialSlabs, int subsequentSlabs)
        {

            if (slabSize < 1) throw new ArgumentException("slabSize must be equal to or greater than 1");
            if (initialSlabs < 1) throw new ArgumentException("initialSlabs must be equal to or greater than 1");
            if (subsequentSlabs < 1) throw new ArgumentException("subsequentSlabs must be equal to or greater than 1");
            if (slabSize > MaximumSlabSize) throw new ArgumentException("slabSize cannot be larger BufferPool.MaximumSlabSize");

            this.slabSize = slabSize > MinimumSlabSize ? slabSize : MinimumSlabSize;
            this.initialSlabs = initialSlabs;
            this.subsequentSlabs = subsequentSlabs;

            // lock is unnecessary in this instance constructor
            //lock (syncSlabList)
            //{
                if (slabs.Count == 0)
                {
                    SetSingleSlabPool(initialSlabs == 1); //Assume for optimization reasons that it's a single slab pool if the number of initial slabs is 1

                    for (int i = 0; i < initialSlabs; i++)
                    {
                        slabs.Add(new MemorySlab(slabSize, this));
                    }

                    firstSlab = slabs[0];
                }
            //}
        }

        //TODO: Look for a better name for this property

        /// <summary>
        /// Gets the initial number of slabs created
        /// </summary>
        public int InitialSlabs
        {
            get { return initialSlabs; }
        }

        //TODO: Look for a better name for this property. SlabIncrements is probably a good one.

        /// <summary>
        /// Gets the additional number of slabs to be created at a time
        /// </summary>
        public int SubsequentSlabs
        {
            get { return subsequentSlabs; }
        }

        /// <summary>
        /// Gets the slab size, in bytes
        /// </summary>
        public long SlabSize
        {
            get { return slabSize; }
        }

        /// <summary>
        /// Gets the number of slabs in the buffer pool
        /// </summary>
        /// <remarks>This property is provided for testing purposes</remarks>
        internal long SlabCount
        {
            get { return slabs.Count; }
        }


        //Pair of Get/Set methods for the optimization singleSlabPool field. This property is accessed instead of the field
        //to prevent the compiler from performing optimizations that may render the field unreliable
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool GetSingleSlabPool()
        {
            return singleSlabPool == -1 ? true : false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetSingleSlabPool(bool value)
        {
            //TODO: Is this Interlocked.Exchange here necessary?, singleSlabPool is a 32-bit value:
            Interlocked.Exchange(ref singleSlabPool, value == true ? -1 : 0);
        }


        /// <summary>
        /// Creates a buffer of the specified size
        /// </summary>
        /// <param name="size">Buffer size, in bytes</param>
        /// <returns>IBuffer object of requested size</returns>        
        public IBuffer GetBuffer(long size)
        {
            return GetBuffer(size, null);
        }

        /// <summary>
        /// Creates a buffer of the specified size, filled with the contents of a specified byte array
        /// </summary>
        /// <param name="size">Buffer size, in bytes</param>
        /// <param name="filledWith">Byte array to copy to buffer</param>
        /// <returns>IBuffer object of requested size</returns>
        public IBuffer GetBuffer(long size, byte[] filledWith)
        {
            if (size < 0) throw new ArgumentException("size must be greater than 0");

            //TODO: If size is larger than 16 * SlabSize (or 16 * MaxNumberOfSegments) then throw exception saying you can't have a buffer greater than 16 times Slab size

            //Make sure filledWith can fit into the requested buffer, so that we do not allocate a buffer and then
            //an exception is thrown (when IBuffer.FillWith() is called) before the buffer is returned.
            if (filledWith != null)
            {
                if (filledWith.LongLength > size) throw new ArgumentException("Length of filledWith array cannot be larger than desired buffer size");
                if (filledWith.LongLength == 0) filledWith = null;

                //TODO: Write test that will test that IBuffer.FillWith() doesn't throw an exception (and that buffers aren't allocated) in this method
            }

            if (size == 0)
            {
                //Return an empty buffer
                return new ManagedBuffer(firstSlab);
            }

            List<IMemoryBlock> allocatedBlocks = new List<IMemoryBlock>();

            //TODO: Consider the performance penalty involved in making the try-catch below a constrained region
            try
            {
                IMemorySlab[] slabArr;
                long allocdLengthTally = 0;

                if (GetSingleSlabPool())
                {
                    //Optimization: Chances are that there'll be just one slab in a pool, so access it directly 
                    //and avoid the lock statement involved while creating an array of slabs.

                    //Note that even if singleSlabPool is inaccurate, this method will still work properly.
                    //The optimization is effective because singleSlabPool will be accurate majority of the time.

                    slabArr = new IMemorySlab[] { firstSlab };
                    allocdLengthTally = TryAllocateBlocksInSlabs(size, MAX_SEGMENTS_PER_BUFFER, slabArr, ref allocatedBlocks);

                    if (allocdLengthTally == size)
                    {
                        //We got the entire length we are looking for, so leave
                        return GetFilledBuffer(allocatedBlocks, filledWith);
                    }

                    SetSingleSlabPool(false); // Slab count will soon be incremented
                }
                else
                {
                    lock (syncSlabList)
                    {
                        slabArr = slabs.ToArray();
                    }

                    allocdLengthTally = TryAllocateBlocksInSlabs(size, MAX_SEGMENTS_PER_BUFFER, slabArr, ref allocatedBlocks);

                    if (allocdLengthTally == size)
                    {
                        //We got the entire length we are looking for, so leave
                        return GetFilledBuffer(allocatedBlocks, filledWith);
                    }
                }


                //Try to create new slab
                lock (syncNewSlab)
                {
                    //Look again for free block
                    lock (syncSlabList)
                    {
                        slabArr = slabs.ToArray();
                    }

                    allocdLengthTally += TryAllocateBlocksInSlabs(size - allocdLengthTally, MAX_SEGMENTS_PER_BUFFER - allocatedBlocks.Count, slabArr, ref allocatedBlocks);

                    if (allocdLengthTally == size)
                    {
                        //found it -- leave
                        return GetFilledBuffer(allocatedBlocks, filledWith);
                    }

                    List<IMemorySlab> newSlabList = new List<IMemorySlab>();
                    do
                    {
                        //Unable to find available free space, so create new slab
                        MemorySlab newSlab = new MemorySlab(slabSize, this);

                        IMemoryBlock allocdBlk;
                        if (slabSize > size - allocdLengthTally)
                        {
                            //Allocate remnant
                            newSlab.TryAllocate(size - allocdLengthTally, out allocdBlk);
                        }
                        else
                        {
                            //Allocate entire slab
                            newSlab.TryAllocate(slabSize, out allocdBlk);
                        }

                        newSlabList.Add(newSlab);
                        allocatedBlocks.Add(allocdBlk);
                        allocdLengthTally += allocdBlk.Length;
                    }
                    while (allocdLengthTally < size);

                    lock (syncSlabList)
                    {
                        //Add new slabs to collection
                        slabs.AddRange(newSlabList);

                        //Add extra slabs as requested in object properties
                        for (int i = 0; i < subsequentSlabs - 1; i++)
                        {
                            slabs.Add(new MemorySlab(slabSize, this));
                        }
                    }

                }

                return GetFilledBuffer(allocatedBlocks, filledWith);
            }
            catch
            {
                //OOMs, Thread abort exceptions and other ugly things can happen so roll back any allocated blocks.
                //This will prevent a limbo situation where those blocks are allocated but caller is unaware and can't deallocate them.

                //NOTE: This try-catch block should not be within a lock as it calls MemorySlab.Free which takes locks,
                //and in turn calls BufferPool.TryFreeSlabs which takes other locks and can lead to a dead-lock/race condition.

                //TODO: Write rollback test.

                for (int b = 0; b < allocatedBlocks.Count; b++)
                {
                    allocatedBlocks[b].Slab.Free(allocatedBlocks[b]);
                }

                throw;
            }

        }

        /// <summary>
        /// Searches for empty slabs and frees one if there are more than InitialSlabs number of slabs.
        /// </summary>
        internal void TryFreeSlabs()
        {
            //NOTE: This method can free a slab just before it gets allocated but that's alright because eventually
            //buffers in the 'stray' slab will be disposed and all references to the slab will no longer exist

            lock (syncSlabList)
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
                    //TODO: MULTI-SLAB: Consider freeing all free slabs that exceed the initial slabs count
                    //'cos a buffer can span several slabs and can actually free multiple slabs instantly.

                    //remove the last empty one
                    slabs.RemoveAt(lastemptySlab);

                    if (slabs.Count == 1) SetSingleSlabPool(true);
                }

            }
        }

        /// <summary>
        /// Helper method that searches for free blocks in an array of slabs and returns allocated blocks
        /// </summary>
        /// <param name="totalLength">Requested total length of all memory blocks</param>
        /// <param name="maxBlocks">Maximum number of memory blocks to allocate</param>
        /// <param name="slabs">Array of slabs to search</param>
        /// <param name="allocatedBlocks">List of allocated memory block</param>
        /// <returns>True if memory block was successfully allocated. False, if otherwise</returns>
        private static long TryAllocateBlocksInSlabs(long totalLength, int maxBlocks, IMemorySlab[] slabs, ref List<IMemoryBlock> allocatedBlocks)
        {
            allocatedBlocks = new List<IMemoryBlock>();

            long minBlockSize;
            long allocatedSizeTally = 0;

            long largest;
            long reqLength;
            IMemoryBlock allocdBlock;
            int allocdCount = 0;
            //TODO: Figure out how to do this math without involving floating point arithmetic
            minBlockSize = (long)Math.Ceiling(totalLength / (float)maxBlocks);
            do
            {
                allocdBlock = null;
                for (int i = 0; i < slabs.Length; i++)
                {
                    largest = slabs[i].LargestFreeBlockSize;
                    if (largest >= minBlockSize)
                    {
                        //Figure out what length to request for
                        reqLength = slabs[i].TryAllocate(minBlockSize, totalLength - allocatedSizeTally, out allocdBlock);

                        if (reqLength > 0)
                        {
                            allocatedBlocks.Add(allocdBlock);
                            allocatedSizeTally += reqLength;
                            allocdCount++;
                            if (allocatedSizeTally == totalLength) return allocatedSizeTally;

                            //Calculate the new minimum block size
                            //TODO: Figure out how to do this math without involving floating point arithmetic
                            minBlockSize = (long)Math.Ceiling((totalLength - allocatedSizeTally) / (float)(maxBlocks - allocdCount));

                            //Scan again from start because there is a chance the smaller minimum block size exists in previously skipped slabs
                            break;
                        }
                    }

                }
            } while (allocdBlock != null);

            return allocatedSizeTally;
        }

        /// <summary>
        /// Helper method that gets a filled buffer constructed from a list of memory blocks
        /// </summary>
        /// <param name="allocatedBlocks">The memoryblocks that comprise the buffer</param>
        /// <param name="filledWith">If not null, the contents the memory block will be filled with</param>
        /// <returns>An optionally filled IBuffer object</returns>
        private static IBuffer GetFilledBuffer(IList<IMemoryBlock> allocatedBlocks, byte[] filledWith)
        {
            IBuffer newBuffer = new ManagedBuffer(allocatedBlocks);
            if (filledWith != null) newBuffer.FillWith(filledWith);
            return newBuffer;
        }
    }
}
