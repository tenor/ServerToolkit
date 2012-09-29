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

    /// <summary>
    /// Represents a buffer for asynchronous socket operations and defines methods to copy data into and out of the buffer
    /// </summary>
    public interface IBuffer : IDisposable
    {
        /// <summary>
        /// Gets the total size of the buffer, in bytes.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets a value indicating whether the buffer is disposed.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Gets the number of segments in the buffer.
        /// </summary>
        int SegmentCount { get; }

        /// <summary>
        /// Gets buffer segments that can be passed on to an asynchronous socket operation.
        /// </summary>
        /// <returns>A list of ArraySegments(of Byte) containing buffer segments.</returns>
        IList<ArraySegment<byte>> GetSegments();

        /// <summary>
        /// Gets buffer segments that can be passed on to an asynchronous socket operation.
        /// </summary>
        /// <param name="length">Total length of segments.</param>
        /// <returns>A list of ArraySegments(of Byte) containing buffer segments.</returns>
        IList<ArraySegment<byte>> GetSegments(long length);

        /// <summary>
        /// Gets buffer segments that can be passed on to an asynchronous socket operation.
        /// </summary>
        /// <param name="offset">Offset in the buffer where segments start.</param>
        /// <param name="length">Total length of segments.</param>
        /// <returns>A list of ArraySegments(of Byte) containing buffer segments.</returns>
        IList<ArraySegment<byte>> GetSegments(long offset, long length);

        /// <summary>
        /// Copies data from the buffer into a byte array.
        /// </summary>
        /// <param name="destinationArray">The one-dimensional byte array which receives the data.</param>
        /// <remarks>The size of the buffer must be less than or equal to the destinationArray length.</remarks>
        void CopyTo(byte[] destinationArray);

        /// <summary>
        /// Copies data from the buffer to a byte array
        /// </summary>
        /// <param name="destinationArray">The one-dimensional byte array which receives the data.</param>
        /// <param name="destinationIndex">The index in the destinationArray at which storing begins.</param>
        /// <param name="length">The number of bytes to copy.</param>
        void CopyTo(byte[] destinationArray, long destinationIndex, long length);

        /// <summary>
        /// Copies data from a byte array to the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <remarks>The length of the sourceArray must be less than or equal to the buffer size.</remarks>
        void FillWith(byte[] sourceArray);

        /// <summary>
        /// Copies data from a byte array to the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <param name="sourceIndex">The index in the sourceArray at which copying begins.</param>
        /// <param name="length">The number of bytes to copy.</param>
        void FillWith(byte[] sourceArray, long sourceIndex, long length);

        /// <summary>
        /// Copies data from a byte array to the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <remarks>The length of the sourceArray must be less than or equal to the buffer size.</remarks>
        [Obsolete("Use the FillWith method instead -- this method will be removed in a later version",true)]
        void CopyFrom(byte[] sourceArray);

        /// <summary>
        /// Copies data from a byte array to the buffer.
        /// </summary>
        /// <param name="sourceArray">The one-dimensional byte array that contains the data.</param>
        /// <param name="sourceIndex">The index in the sourceArray at which copying begins.</param>
        /// <param name="length">The number of bytes to copy.</param>
        [Obsolete("Use the FillWith method instead -- this method will be removed in a later version", true)]
        void CopyFrom(byte[] sourceArray, long sourceIndex, long length);
    }

    /// <summary>
    /// Represents a pool of IBuffer objects 
    /// </summary>
    internal interface IBufferPool
    {
        /// <summary>
        /// Gets the initial number of slabs created
        /// </summary>
        int InitialSlabs { get; }

        /// <summary>
        /// Gets the additional number of slabs to be created at a time
        /// </summary>
        int SubsequentSlabs { get; }

        /// <summary>
        /// Gets the slab size, in bytes
        /// </summary>
        long SlabSize { get; }

        /// <summary>
        /// Creates a buffer of the specified size
        /// </summary>
        /// <param name="size">Buffer size, in bytes</param>
        /// <returns>IBuffer object of requested size</returns>
        IBuffer GetBuffer(long size);

        /// <summary>
        /// Creates a buffer of the specified size, filled with the contents of a specified byte array
        /// </summary>
        /// <param name="size">Buffer size, in bytes</param>
        /// <param name="filledWith">Byte array to copy to buffer</param>
        /// <returns>IBuffer object of requested size</returns>
        IBuffer GetBuffer(long size, byte[] filledWith);

    }
}
