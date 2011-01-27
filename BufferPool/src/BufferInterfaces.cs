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

    public interface IBuffer : IDisposable
    {
        long Size { get; }
        bool IsDisposed { get; }
        int SegmentCount { get; }
        IList<ArraySegment<byte>> GetSegments();
        IList<ArraySegment<byte>> GetSegments(long length);
        IList<ArraySegment<byte>> GetSegments(long offset, long length);
        void CopyTo(byte[] destinationArray);
        void CopyTo(byte[] destinationArray, long destinationIndex, long length);
        void CopyFrom(byte[] sourceArray);
        void CopyFrom(byte[] sourceArray, long sourceIndex, long length);
    }

    public interface IBufferPool
    {
        int InitialSlabs { get; }
        int SubsequentSlabs { get; }
        long SlabSize { get; }
        IBuffer GetBuffer(long size);

    }
}
