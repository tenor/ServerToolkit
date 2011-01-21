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
    public interface IBuffer : IDisposable
    {
        IList<ArraySegment<byte>> GetArraySegments();
        IList<ArraySegment<byte>> GetArraySegments(int Length);
        IList<ArraySegment<byte>> GetArraySegments(int Offset, int Length);
        void CopyTo(byte[] DestinationArray);
        void CopyTo(byte[] DestinationArray, long DestinationIndex, long Length);
        void CopyFrom(byte[] SourceArray);
        void CopyFrom(byte[] SourceArray, long SourceIndex, long Length);
        long Length { get; }
        bool IsDisposed { get; }
    }

    public interface IBufferPool
    {
        IBuffer GetBuffer(long Size);
        int InitialSlabs { get; }
        int SubsequentSlabs { get; }
        long SlabSize { get; }

    }
}
