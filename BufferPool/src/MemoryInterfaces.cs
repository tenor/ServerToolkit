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
    /// <summary>
    /// Represents a block of memory
    /// </summary>
    internal interface IMemoryBlock
    {
        /// <summary>
        /// Gets the offset in the slab where the memory block begins.
        /// </summary>
        long StartLocation { get; }

        /// <summary>
        /// Gets the offset in the slab where the memory block ends.
        /// </summary>
        /// <remarks> EndLocation = StartLocation + Length - 1</remarks>
        long EndLocation { get; }

        /// <summary>
        /// Gets the length of the memory block, in bytes.
        /// </summary>
        /// <remarks> Length = EndLocation - StartLocation + 1</remarks>
        long Length { get; }

        /// <summary>
        /// Gets the containing slab
        /// </summary>
        IMemorySlab Slab { get; }

    }

    /// <summary>
    /// Represents a memory slab, from which memory blocks are allocated
    /// </summary>
    internal interface IMemorySlab
    {
        /// <summary>
        /// Gets the size, in bytes, of the memory slab.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets the last known largest unallocated contiguous block size.
        /// </summary>
        long LargestFreeBlockSize { get; }

        /// <summary>
        /// Gets the underlying byte array that is wrapped by the memory slab
        /// </summary>
        byte[] Array { get; }

        /// <summary>
        /// Attempts to allocate a memory block of a specified length.
        /// </summary>
        /// <param name="length">Length, in bytes, of memory block</param>
        /// <param name="allocatedBlock">Allocated memory block</param>
        /// <returns>True, if memory block was allocated. False, if otherwise</returns>
        bool TryAllocate(long length, out IMemoryBlock allocatedBlock);

        /// <summary>
        /// Frees an allocated memory block.
        /// </summary>
        /// <param name="allocatedBlock">Allocated memory block to be freed</param>
        void Free(IMemoryBlock allocatedBlock);

    }
}
