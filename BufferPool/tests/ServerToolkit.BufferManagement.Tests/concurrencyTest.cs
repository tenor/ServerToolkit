using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace ServerToolkit.BufferManagement.Tests
{
    [TestClass]
    public class concurrencyTest
    {
        [TestMethod]
        [Description("Verifies that silmultaneous access to a buffer pool is synchronized")] 
        public void HundredGetBuffers()
        {
            BufferPool pool = new BufferPool(10 * 1024 * 1024, 1, 1);

            //PLAN:
            //Create 100 threads. Odd numbered threads will request buffer sizes of 314567 bytes
            //whereas even numbered threads will request buffer sizes of 314574 bytes
            //for a total allocation of 31457100 bytes.
            //If everything goes well there will be no overlap in allocated buffers
            //and there'll be no free space greater than 314574 (the lower number) on total slabs - 1
            //and slabs should be 4

            List<IBuffer> bufferList = new List<IBuffer>();

            int threadNumber = 100;
            int sizeOdd = 314567;
            int sizeEven = 314574;

            AquireBuffersConcurrently(pool, bufferList, threadNumber, sizeOdd, sizeEven);
            AssertIsContiguous(bufferList);
            Assert.IsTrue(pool.SlabCount == 3 || pool.SlabCount == 4, "SlabCount is " + pool.SlabCount + ". Was expecting 3 or 4");
        }

        [TestMethod]
        [Description("Verifies that silmultaneous access to a buffer pool is synchronized and will fit exactly in slabs")]
        public void HundredGetBuffersExactFit()
        {
            BufferPool pool = new BufferPool(10 * 1024 * 1024, 1, 1);

            //PLAN:
            //Same as HundredGetBuffersExactFit, except that slab size have been selected such that 20 buffers will fit in a slab.
            //Therefore there must be only 5 slabs after test is complete.

            List<IBuffer> bufferList = new List<IBuffer>();

            int threadNumber = 100;
            int sizeOdd = 524288;
            int sizeEven = 524288;

            AquireBuffersConcurrently(pool, bufferList, threadNumber, sizeOdd, sizeEven);

            AssertIsContiguous(bufferList);
            Assert.AreEqual(5, pool.SlabCount);
        }



        private static void AquireBuffersConcurrently(BufferPool pool, List<IBuffer> bufferList, int threadNumber, int sizeOdd, int sizeEven)
        {
            object bufferList_sync = new object();
            ManualResetEvent mre = new ManualResetEvent(false);

            for (int i = 0; i < threadNumber; i++)
            {
                Thread thread = new Thread(delegate(object number)
                {
                    var size = ((int)number % 2 == 1 ? sizeOdd : sizeEven);

                    //wait for signal
                    mre.WaitOne();

                    //Aquire buffer
                    IBuffer buff = pool.GetBuffer(size);

                    //Add to Queue
                    lock (bufferList_sync)
                    {
                        bufferList.Add(buff);
                    }
                });

                thread.IsBackground = true;
                thread.Start(i);
            }

            Thread.Sleep(500); //ensure all threads are waiting for signal

            mre.Set(); //signal event

            //Wait till all threads are done
            while (true)
            {
                lock (bufferList)
                {
                    if (bufferList.Count == threadNumber) break;
                    Thread.Sleep(500);
                }
            }
        }



        //verify that all buffers referencing the same array are contiguous i.e no overlap
        private static void AssertIsContiguous(List<IBuffer> bufferList)
        {

            var orderedGroups = bufferList.GroupBy(o => o.GetArraySegment().Array);

            foreach (var grp in orderedGroups)
            {
                var orderedList = grp.OrderBy(o => o.GetArraySegment().Offset).ToList();
                Assert.AreEqual(orderedList[0].GetArraySegment().Offset, 0);
                for (int i = 1; i < orderedList.Count; i++)
                {
                    Assert.IsTrue(
                        orderedList[i].GetArraySegment().Offset == orderedList[i - 1].GetArraySegment().Offset + orderedList[i - 1].GetArraySegment().Count
                        );
                }
            }
        }
    }
}
