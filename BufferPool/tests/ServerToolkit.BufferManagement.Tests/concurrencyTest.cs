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

        //TODO: Add memory test that performs a hundred get buffers, then silmultaneously randomly disposes some and creates some
        //Then verify that the total tally is correct and the number of expected slabs is accurate.

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
                //and slabs should be 3.

                List<IBuffer> bufferList = new List<IBuffer>();

                int threadNumber = 100;
                int sizeOdd = 314567;
                int sizeEven = 314574;

                AquireBuffersConcurrently(pool, bufferList, threadNumber, sizeOdd, sizeEven);
                AssertIsContiguous(bufferList);
                //TODO:Also Assert that all buffers, both Size and GetSegments().Count are equal to the requested size.
                Assert.IsTrue(pool.SlabCount == 3, "SlabCount is " + pool.SlabCount + ". Was expecting 3");
        }

        [TestMethod]
        [Description("HundredGetBuffers times thirty")]
        public void ThreeThousandGetBuffers()
        {
            //PLAN:
            //Just like HundredGetBuffers but with 3000 threads

            /*
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(@"c:\test\myerrors.txt"));
            System.Diagnostics.Trace.AutoFlush = true;
             */
            
            try
            {
                System.Runtime.MemoryFailPoint memCheck = new System.Runtime.MemoryFailPoint(3000 + 1500); //Thread memory + pool memory
            }
            catch (InsufficientMemoryException) 
            {
                Assert.Inconclusive("There is not enough memory on the system to run this test");
            }
             
            BufferPool pool = new BufferPool(10 * 1024 * 1024, 1, 1);

            List<IBuffer> bufferList = new List<IBuffer>();

            int threadNumber = 3000;
            int sizeOdd = 524288;
            int sizeEven = 524288;

            AquireBuffersConcurrently(pool, bufferList, threadNumber, sizeOdd, sizeEven);
            AssertIsContiguous(bufferList);
            //TODO:Also Assert that all buffers, both Size and GetSegments().Count are equal to the requested size.
            Assert.IsTrue(pool.SlabCount == 150, "SlabCount is " + pool.SlabCount + ". Was expecting 150");

        }


        [TestMethod]
        [Description("Runs HundredGetBuffers 50 times. This test is necessary to sleuth out subtle bugs that will not be found by HundredGetBuffers")]
        public void LongRunningHundredGetBuffers()
        {
            /*
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(@"c:\test\myerrors.txt"));
            System.Diagnostics.Trace.AutoFlush = true;
             */

            for (int count = 0; count < 50; count++)
            {
                HundredGetBuffers();
            }
        }



        private static void AquireBuffersConcurrently(BufferPool pool, List<IBuffer> bufferList, int threadNumber, int sizeOdd, int sizeEven)
        {
            object bufferList_sync = new object();
            ManualResetEvent mre = new ManualResetEvent(false);

            for (int i = 0; i < threadNumber; i++)
            {
                Thread thread = new Thread(delegate(object number)
                {
                    try
                    {
                        var size = ((int)number % 2 == 1 ? sizeOdd : sizeEven);

                        //wait for signal
                        mre.WaitOne();

                        IBuffer buff = pool.GetBuffer(size);

                        //Add to Queue
                        lock (bufferList_sync)
                        {
                            bufferList.Add(buff);
                        }
                    }
                    catch(Exception e) 
                    {

                        System.Diagnostics.Trace.WriteLine("=================\r\nThread " + number + ", " + Thread.CurrentThread.ManagedThreadId + "\r\n\r\n"
                            + e.GetType() + "\r\n" + e.Message + "\r\n\r\n" + e.StackTrace);

                        throw;
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



        //verify that all buffers referencing the same array are contiguous i.e no overlap or gap.
        private static void AssertIsContiguous(List<IBuffer> bufferList)
        {
            var segmentSlabGroups = bufferList.SelectMany( b => b.GetSegments()).GroupBy(o => o.Array);

            foreach (var grp in segmentSlabGroups)
            {
                var orderedList = grp.OrderBy(o => o.Offset).ToList();
                Assert.AreEqual(0, orderedList[0].Offset);
                for (int i = 1; i < orderedList.Count; i++)
                {
                    Assert.IsTrue(
                        orderedList[i].Offset == orderedList[i - 1].Offset + orderedList[i - 1].Count
                        );
                }
            }
        }
    }
}
