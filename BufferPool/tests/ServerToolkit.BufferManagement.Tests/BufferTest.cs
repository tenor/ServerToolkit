using ServerToolkit.BufferManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace ServerToolkit.BufferManagement.Tests
{
    
    
    /// <summary>
    ///This is a test class for BufferTest and is intended
    ///to contain all BufferTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BufferTest
    {

        static long blockSize = 20000;

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion



        private static byte[] GetRandomizedByteArray(long Length)
        {
            byte[] SourceArray = new byte[Length];
            new Random().NextBytes(SourceArray);
            return SourceArray;
        }

        //Gets a single slab buffer
        private static ManagedBuffer GetNewBuffer(IMemorySlab Slab)
        {

            IMemoryBlock allocatedMemoryBlock;
            Slab.TryAllocate(blockSize, out allocatedMemoryBlock);

            ManagedBuffer target = new ManagedBuffer(new IMemoryBlock[] { allocatedMemoryBlock });
            return target;
        }

        //Gets a multi slab buffer
        private static ManagedBuffer GetNewBuffer(IMemorySlab[] Slabs)
        {
            List<IMemoryBlock> blockList = new List<IMemoryBlock>();
            IMemoryBlock allocatedMemoryBlock;

            foreach (var slab in Slabs)
            {
                slab.TryAllocate(blockSize, out allocatedMemoryBlock);
                blockList.Add(allocatedMemoryBlock);
            }

            ManagedBuffer target = new ManagedBuffer(blockList);
            return target;
        }        

        private bool ArraysMatch(Array array1, Array array2)
        {
            if (array1.LongLength != array2.LongLength)
            {
                return false;
            }

            for (long i = 0; i < array1.LongLength; i++)
            {
                if (!array1.GetValue(i).Equals(array2.GetValue(i)))
                {
                    return false;
                }
            }
            return true;
        }



        /// <summary>
        ///A test for ManagedBuffer Constructor
        ///</summary>
        [TestMethod()]
        [Description("Construction with null AllocatedMemoryBlocks throws exception")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BufferConstructorTest()
        {
            IMemoryBlock[] nullBlocks = null;
            ManagedBuffer target = new ManagedBuffer(nullBlocks);
        }

        /// <summary>
        ///A test for ManagedBuffer Constructor
        ///</summary>
        [TestMethod()]
        [Description("Construction with empty AllocatedMemoryBlocks throws exception")]
        [ExpectedException(typeof(ArgumentException))]
        public void BufferConstructorTest2()
        {
            IMemoryBlock[] emptyBlocks = new IMemoryBlock[0];
            ManagedBuffer target = new ManagedBuffer(emptyBlocks);
        }

        /// <summary>
        ///A test for FillWith
        ///</summary>
        [TestMethod()]
        [Description("FillWith() copies full source array")]
        public void FillWithTest()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target1, target2;

            {
                ManagedBuffer target = GetNewBuffer(slab);
                target1 = target;
                byte[] SourceArray = GetRandomizedByteArray(blockSize);
                target.FillWith(SourceArray);
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

                //Source Array is smaller than ManagedBuffer Size
                long blockSizeLess = blockSize - 100;
                SourceArray = GetRandomizedByteArray(blockSizeLess);
                target.FillWith(SourceArray);
                copyOfDestination = new byte[blockSizeLess];
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

            }

            //Repeat test with a new buffer to confirm that offsets within slab arrays are accurately tracked
            //and to make sure there is no off by 1 error

            {
                ManagedBuffer target = GetNewBuffer(slab);
                target2 = target;
                byte[] SourceArray = GetRandomizedByteArray(blockSize);
                target.FillWith(SourceArray);
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

                //Source Array is smaller than ManagedBuffer Size
                long blockSizeLess = blockSize - 1;
                SourceArray = GetRandomizedByteArray(blockSizeLess);
                target.FillWith(SourceArray);
                copyOfDestination = new byte[blockSizeLess];
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

            }

            target2.Dispose();
            target1.Dispose();

        }

        /// <summary>
        ///A test for FillWith
        ///</summary>
        [TestMethod()]
        [Description("FillWith() throws exception when source is larger than ManagedBuffer")]
        [ExpectedException(typeof(ArgumentException))]
        public void CopyFromTest2()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);

            ManagedBuffer target = GetNewBuffer(slab);
            //Source Array is larger than buffer size
            long blockSizeMore = blockSize + 1;
            byte[] SourceArray = GetRandomizedByteArray(blockSizeMore);

            target.FillWith(SourceArray);

        }


        /// <summary>
        ///A test for FillWith
        ///</summary>
        [TestMethod()]
        [Description("Source arrays copied into the middle of a buffer are copied accurately")]
        public void FillWithTest3()
        {

            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);

            byte[] SourceArray = GetRandomizedByteArray(blockSize);
            target.FillWith(SourceArray, 1, blockSize - 2);
            byte[] copyOfDestination = new byte[blockSize - 2];
            byte[] copyOfSource = new byte[blockSize - 2];
            Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
            Array.Copy(SourceArray, 1, copyOfSource, 0, copyOfSource.LongLength);
            Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

        }

        /// <summary>
        ///A test for CopyTo
        ///</summary>
        [TestMethod()]
        [Description("CopyTo() copies full buffer")]
        public void CopyToTest()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target1, target2;

            {
                ManagedBuffer target = GetNewBuffer(slab);
                target1 = target;
                target.FillWith(GetRandomizedByteArray(blockSize));
                byte[] DestArray = new byte[blockSize];
                target.CopyTo(DestArray);
                byte[] copyOfSource = new byte[blockSize];
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
                Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

                //Destination Array is larger than ManagedBuffer Size
                target.FillWith(GetRandomizedByteArray(blockSize));
                DestArray = new byte[blockSize + 100];
                target.CopyTo(DestArray);
                copyOfSource = new byte[blockSize];
                copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
                Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

            }

            //Repeat test with a new buffer to confirm that offsets within slab arrays are accurately tracked
            //and to make sure there is no off by 1 error

            {
                ManagedBuffer target = GetNewBuffer(slab);
                target2 = target;
                target.FillWith(GetRandomizedByteArray(blockSize));
                byte[] DestArray = new byte[blockSize];
                target.CopyTo(DestArray);
                byte[] copyOfSource = new byte[blockSize];
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
                Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

                //Destination Array is larger than ManagedBuffer Size
                target.FillWith(GetRandomizedByteArray(blockSize));
                DestArray = new byte[blockSize + 1];
                target.CopyTo(DestArray);
                copyOfSource = new byte[blockSize];
                copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
                Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

            }

            target2.Dispose();
            target1.Dispose();


        }

        /// <summary>
        ///A test for CopyTo
        ///</summary>
        [TestMethod()]
        [Description("CopyTo() throws exception when destination is smaller than ManagedBuffer")]
        [ExpectedException(typeof(ArgumentException))]
        public void CopyToTest2()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);

            ManagedBuffer target = GetNewBuffer(slab);
            //Destination array is smaller than buffer size

            byte[] DestArray = new byte[blockSize - 1];
            target.CopyTo(DestArray);
        }

        /// <summary>
        ///A test for CopyTo
        ///</summary>
        [TestMethod()]
        [Description("Buffers copied into the middle of a destination array are copied accurately")]
        public void CopyToTest3()
        {

            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);

            target.FillWith(GetRandomizedByteArray(blockSize));
            byte[] DestArray = new byte[blockSize];
            target.CopyTo(DestArray, 1, blockSize - 2);
            byte[] copyOfSource = new byte[blockSize - 2];
            byte[] copyOfDestination = new byte[blockSize - 2];
            Array.Copy(DestArray, 1, copyOfDestination, 0, copyOfDestination.LongLength);
            Array.Copy(target.GetSegments()[0].Array, target.GetSegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
            Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

        }

        /// <summary>
        ///Dispose in different scenarios
        ///</summary>
        [TestMethod()]
        [Description("Dispose works safely more than once, buffer throws ObjectDisposedException if used in disposed state")]
        public void DisposeTest()
        {

            MemorySlab slab = new MemorySlab(blockSize * 3, null);

            ManagedBuffer target = GetNewBuffer(slab);
            target.Dispose();

            //Call Dispose again, shouldn't cause any exceptions to be thrown
            target.Dispose();

            //Try to work with disposed object. should throw exceptions
            {
                bool exceptionThrown = false;

                try
                {
                    target.FillWith(new byte[] { 0, 1, 2 });
                }
                catch (ObjectDisposedException)
                {
                    exceptionThrown = true;
                }

                Assert.IsTrue(exceptionThrown);
            }

            {
                bool exceptionThrown = false;

                try
                {
                    target.CopyTo(new byte[] { 0, 1, 2 });
                }
                catch (ObjectDisposedException)
                {
                    exceptionThrown = true;
                }

                Assert.IsTrue(exceptionThrown);
            }

            {
                bool exceptionThrown = false;

                try
                {
                    target.GetSegments();
                }
                catch (ObjectDisposedException)
                {
                    exceptionThrown = true;
                }

                Assert.IsTrue(exceptionThrown);
            }


        }

        /// <summary>
        ///A test for GetSegments
        ///</summary>
        [TestMethod()]
        [Description("ArraySegments returned by GetArraySegment(int,int) is accurate")]
        public void GetSegmentsTest()
        {
            //Single Slab tests:

            {
                MemorySlab slab = new MemorySlab(blockSize * 3, null);
                ManagedBuffer target = GetNewBuffer(slab);
                int Offset = 0;
                int Length = (int)(blockSize - 1);
                ArraySegment<byte> actual;
                actual = target.GetSegments(Offset, Length)[0];
                Assert.AreEqual<long>(actual.Offset, Offset + target.MemoryBlocks[0].StartLocation);
                Assert.AreEqual<long>(actual.Count, Length);
                Assert.AreEqual<byte[]>(actual.Array, target.MemoryBlocks[0].Slab.Array);

                //Test for full blocksize
                Offset = 0;
                Length = (int)blockSize;
                actual = target.GetSegments(Offset, Length)[0];
                Assert.AreEqual<long>(actual.Offset, Offset + target.MemoryBlocks[0].StartLocation);
                Assert.AreEqual<long>(actual.Count, Length);
                Assert.AreEqual<byte[]>(actual.Array, target.MemoryBlocks[0].Slab.Array);


                //Test for offset of 1
                Offset = 1;
                Length = (int)(blockSize - 1);
                actual = target.GetSegments(Offset, Length)[0];
                Assert.AreEqual<long>(actual.Offset, Offset + target.MemoryBlocks[0].StartLocation);
                Assert.AreEqual<long>(actual.Count, Length);
                Assert.AreEqual<byte[]>(actual.Array, target.MemoryBlocks[0].Slab.Array);
            }

            //Multi-Slab tests:

            {
                MemorySlab[] slabs = { new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null) };
                ManagedBuffer target = GetNewBuffer(slabs);
                int Offset = 0;
                int Length = (int)(blockSize - 1);
                var actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array,actual[0].Array);

                //Test for full blocksize
                Offset = 0;
                Length = (int)blockSize;
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);


                //Test for offset of 1
                Offset = 1;
                Length = (int)(blockSize - 1);
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);

                //Test that offset of 1 plus blocksize flows into another segment
                Offset = 1;
                Length = (int)(blockSize);
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(2, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual(1, actual[1].Count); 
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);

                //Test that offset of 1 plus (blocksize * 2) - 2 stays in two segments
                Offset = 1;
                Length = (int)(blockSize * 2 - 2);
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(2, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual(blockSize - 1, actual[1].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);

                //Test that offset of 1 plus (blocksize * 2) - 1 stays in two segments
                Offset = 1;
                Length = (int)(blockSize * 2 - 1);
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(2, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual(blockSize, actual[1].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);

                //Test that offset of 0 plus (blocksize * 2) stays in two segments
                Offset = 0;
                Length = (int)(blockSize * 2);
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(2, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual(blockSize, actual[1].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);


                //Test that offset of 1 plus (blocksize * 2) flows into the third segment
                Offset = 1;
                Length = (int)(blockSize * 2);
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(3, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[2].StartLocation, actual[2].Offset);
                Assert.AreEqual(1, actual[2].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count + actual[2].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[2].Slab.Array, actual[2].Array);


            }

        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment(int,int) for zero lengths is accurate")]
        public void GetSegmentsTest2()
        {

            //Single Slab test
            {
                MemorySlab slab = new MemorySlab(blockSize * 3, null);
                ManagedBuffer target = GetNewBuffer(slab);
                int Offset = 0;
                int Length = 0;
                var actual = target.GetSegments(Offset, Length);
                Assert.AreEqual<int>(1, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);

                //Test again at a different offset
                Offset = 10;
                Length = 0;
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual<int>(1, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
            }

            //Multi slab test:
            {
                MemorySlab[] slabs = { new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null) };
                ManagedBuffer target = GetNewBuffer(slabs);
                int Offset = 0;
                int Length = 0;
                var actual = target.GetSegments(Offset, Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);

                //Test again at a different offset
                Offset = 10;
                Length = 0;
                actual = target.GetSegments(Offset, Length);
                Assert.AreEqual<int>(1, actual.Count);
                Assert.AreEqual<long>(Offset + target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
            }
        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("GetArraySegment(int,int) with -1 offset parameter throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetSegmentsTest3()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Offset = -1;
            int Length = 0;
            ArraySegment<byte> actual;
            actual = target.GetSegments(Offset, Length)[0];
        }

        [TestMethod()]
        [Description("GetArraySegment(int,int) fwith -1 length parameter throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetSegmentsTest4()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Offset = 0;
            int Length = -1;
            ArraySegment<byte> actual;
            actual = target.GetSegments(Offset, Length)[0];
        }

        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment(int) is accurate")]
        public void GetSegmentsTest5()
        {

            //Single slab test:
            {
                MemorySlab slab = new MemorySlab(blockSize * 3, null);
                ManagedBuffer target = GetNewBuffer(slab);
                int Length = (int)(blockSize - 1);
                var actual = target.GetSegments(Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);

                //Test again for full blocksize
                Length = (int)(blockSize);
                actual = target.GetSegments(Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(actual[0].Offset, target.MemoryBlocks[0].StartLocation);
                Assert.AreEqual<long>(actual[0].Count, Length);
                Assert.AreEqual<byte[]>(actual[0].Array, target.MemoryBlocks[0].Slab.Array);
            }

            //Multi slab test:
            {
                MemorySlab[] slabs = { new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null) };
                ManagedBuffer target = GetNewBuffer(slabs);
                int Length = (int)(blockSize - 1);
                var actual = target.GetSegments(Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);

                //Test for full blocksize
                Length = (int)blockSize;
                actual = target.GetSegments(Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);

                //Test that blocksize plus 1 flows into another segment
                Length = (int)(blockSize + 1);
                actual = target.GetSegments(Length);
                Assert.AreEqual(2, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual(1, actual[1].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);

                //Test that (blocksize * 2) - 1 stays in two segments
                Length = (int)(blockSize * 2 - 1);
                actual = target.GetSegments(Length);
                Assert.AreEqual(2, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual(blockSize - 1, actual[1].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);

                //Test that (blocksize * 2) stays in two segments
                Length = (int)(blockSize * 2);
                actual = target.GetSegments(Length);
                Assert.AreEqual(2, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual(blockSize, actual[1].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);

                //Test that (blocksize * 2) + 1 flows into the third segment
                Length = (int)(blockSize * 2 + 1);
                actual = target.GetSegments(Length);
                Assert.AreEqual(3, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[2].StartLocation, actual[2].Offset);
                Assert.AreEqual(1, actual[2].Count);
                Assert.AreEqual<long>(Length, actual[0].Count + actual[1].Count + actual[2].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[2].Slab.Array, actual[2].Array);


            }

        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment(int) for zero lengths is accurate")]
        public void GetSegmentsTest6()
        {
            //Single slab test:
            {
                MemorySlab slab = new MemorySlab(blockSize * 3, null);
                ManagedBuffer target = GetNewBuffer(slab);
                int Length = 0;
                var actual = target.GetSegments(Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
            }

            //Multi slab test:
            {
                MemorySlab[] slabs = { new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null) };
                ManagedBuffer target = GetNewBuffer(slabs);
                int Length = 0;
                var actual = target.GetSegments(Length);
                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(Length, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
            }

        }


        [TestMethod()]
        [Description("GetArraySegment(int,int) with -1 length parameter throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetSegmentsTest7()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Length = -1;
            ArraySegment<byte> actual;
            actual = target.GetSegments(Length)[0];
        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment() is accurate")]
        public void GetSegmentsTest8()
        {

            //Single slab test:
            {
                MemorySlab slab = new MemorySlab(blockSize * 3, null);
                ManagedBuffer target = GetNewBuffer(slab);
                var actual = target.GetSegments();
                Assert.AreEqual<int>(1, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.Size, actual[0].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
            }

            //Multi slab test:
            {
                MemorySlab[] slabs = { new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null), new MemorySlab(blockSize * 2, null) };
                ManagedBuffer target = GetNewBuffer(slabs);
                var actual = target.GetSegments();
                Assert.AreEqual(3, actual.Count);
                Assert.AreEqual<long>(target.MemoryBlocks[0].StartLocation, actual[0].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[1].StartLocation, actual[1].Offset);
                Assert.AreEqual<long>(target.MemoryBlocks[2].StartLocation, actual[2].Offset);
                Assert.AreEqual<long>(target.Size, actual[0].Count + actual[1].Count + actual[2].Count);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[0].Slab.Array, actual[0].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[1].Slab.Array, actual[1].Array);
                Assert.AreEqual<byte[]>(target.MemoryBlocks[2].Slab.Array, actual[2].Array);

            }
        }


        /// <summary>
        ///A test for IsDisposed
        ///</summary>
        [TestMethod()]
        [Description("IsDisposed returns expected value")]
        public void IsDisposedTest()
        {

            MemorySlab slab = new MemorySlab(blockSize * 3, null);

            ManagedBuffer target = GetNewBuffer(slab);
            Assert.IsFalse(target.IsDisposed);
            target.Dispose();
            Assert.IsTrue(target.IsDisposed);


        }

        /// <summary>
        ///A test for Length
        ///</summary>
        [TestMethod()]
        [Description("Length matches constructor parameter")]
        public void LengthTest()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);

            ManagedBuffer target = GetNewBuffer(slab);
            Assert.AreEqual<long>(blockSize, target.Size);
            target.Dispose();
        }
    }
}
