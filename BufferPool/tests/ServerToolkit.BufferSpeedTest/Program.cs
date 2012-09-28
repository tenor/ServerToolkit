using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerToolkit.BufferManagement;
using System.Threading;
using System.Diagnostics;

namespace ServerToolkit.BufferPoolSpeedTest
{
    class Program
    {
        static BufferPool pool = new BufferPool(1024 * 1024, 1, 1);

        const int bufferCount = 20;
        const int operationsPerThread = 1000000;
        const int minBufferSize = 27000;
        const int maxBufferSize = 30000;
        const int NoOfThreads = 4;
        const int loops = 10;

        static ManualResetEvent startEvent;
        static ManualResetEvent[] doneEvents;

        static Random rand = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine("Running test...");

            TimeSpan totalTestTime = new TimeSpan();

            for (int p = 0; p < loops; p++)
            {
                startEvent = new ManualResetEvent(false);
                doneEvents = new ManualResetEvent[NoOfThreads];

                for (int i = 0; i < NoOfThreads; i++)
                {
                    var done = doneEvents[i] = new ManualResetEvent(false);
                    Thread t = new Thread(() =>
                    {
                        RunTest(done);
                    });

                    t.IsBackground = true;
                    t.Start();

                }

                Thread.Sleep(500); //Wait for all threads to be ready

                Stopwatch timer = new Stopwatch();
                timer.Start(); //start timer

                startEvent.Set(); //Signal start 

                WaitHandle.WaitAll(doneEvents); //Wait for all done signals

                timer.Stop(); //stop timer

                totalTestTime += timer.Elapsed; //add elapsed time to total time

            }

            

            Console.WriteLine("\r\nSpeed test complete. {0} loops over {1:N0} buffer operations across {2} threads took {3}\r\n", loops, operationsPerThread * NoOfThreads, NoOfThreads, totalTestTime);
        }

        static void RunTest(ManualResetEvent done)
        {
            //Wait for signal
            startEvent.WaitOne();

            IBuffer[] buffers = new IBuffer[bufferCount];

            //Grab new buffers
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = pool.GetBuffer(minBufferSize + rand.Next(101) * ((maxBufferSize - minBufferSize) / 100));
            }

            //Randomly allocate and deallocate buffers
            int selected;
            for (int i = 0; i < operationsPerThread; i++)
            {
                selected = rand.Next(bufferCount);

                if (buffers[selected].IsDisposed)
                {
                    buffers[selected] = pool.GetBuffer(minBufferSize + rand.Next(101) * ((maxBufferSize - minBufferSize) / 100));
                }
                else
                {
                    buffers[selected].Dispose();
                }
            }

            //Dispose all buffers
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i].Dispose();
            }

            //Signal you're done
            done.Set();

        }
    }
}
