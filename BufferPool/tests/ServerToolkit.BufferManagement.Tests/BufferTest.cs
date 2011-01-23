using ServerToolkit.BufferManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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

        private static ManagedBuffer GetNewBuffer(IMemorySlab Slab)
        {

            IMemoryBlock allocatedMemoryBlock;
            Slab.TryAllocate(blockSize, out allocatedMemoryBlock);

            ManagedBuffer target = new ManagedBuffer(allocatedMemoryBlock);
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
        [Description("Construction with null AllocatedMemoryBlock throws exception")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BufferConstructorTest()
        {
            IMemoryBlock nullSlab = null;
            ManagedBuffer target = new ManagedBuffer(nullSlab);
        }

        /// <summary>
        ///A test for CopyFrom
        ///</summary>
        [TestMethod()]
        [Description("CopyFrom() copies full source array")]
        public void CopyFromTest()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target1, target2;

            {
                ManagedBuffer target = GetNewBuffer(slab);
                target1 = target;
                byte[] SourceArray = GetRandomizedByteArray(blockSize);
                target.CopyFrom(SourceArray);
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

                //Source Array is smaller than ManagedBuffer Size
                long blockSizeLess = blockSize - 100;
                SourceArray = GetRandomizedByteArray(blockSizeLess);
                target.CopyFrom(SourceArray);
                copyOfDestination = new byte[blockSizeLess];
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

            }

            //Repeat test with a new buffer to confirm that offsets within slab arrays are accurately tracked
            //and to make sure there is no off by 1 error

            {
                ManagedBuffer target = GetNewBuffer(slab);
                target2 = target;
                byte[] SourceArray = GetRandomizedByteArray(blockSize);
                target.CopyFrom(SourceArray);
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

                //Source Array is smaller than ManagedBuffer Size
                long blockSizeLess = blockSize - 1;
                SourceArray = GetRandomizedByteArray(blockSizeLess);
                target.CopyFrom(SourceArray);
                copyOfDestination = new byte[blockSizeLess];
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
                Assert.IsTrue(ArraysMatch(SourceArray, copyOfDestination));

            }

            target2.Dispose();
            target1.Dispose();

        }

        /// <summary>
        ///A test for CopyFrom
        ///</summary>
        [TestMethod()]
        [Description("CopyFrom() throws exception when source is larger than ManagedBuffer")]
        [ExpectedException(typeof(ArgumentException))]
        public void CopyFromTest2()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);

            ManagedBuffer target = GetNewBuffer(slab);
            //Source Array is larger than buffer size
            long blockSizeMore = blockSize + 1;
            byte[] SourceArray = GetRandomizedByteArray(blockSizeMore);

            target.CopyFrom(SourceArray);

        }


        /// <summary>
        ///A test for CopyFrom
        ///</summary>
        [TestMethod()]
        [Description("Source arrays copied into the middle of a buffer are copied accurately")]
        public void CopyFromTest3()
        {

            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);

            byte[] SourceArray = GetRandomizedByteArray(blockSize);
            target.CopyFrom(SourceArray,1,blockSize - 2);
            byte[] copyOfDestination = new byte[blockSize - 2];
            byte[] copyOfSource = new byte[blockSize - 2];
            Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfDestination, 0, copyOfDestination.LongLength);
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
                target.CopyFrom(GetRandomizedByteArray(blockSize));
                byte[] DestArray = new byte[blockSize];
                target.CopyTo(DestArray);
                byte[] copyOfSource = new byte[blockSize];
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
                Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

                //Destination Array is larger than ManagedBuffer Size
                target.CopyFrom(GetRandomizedByteArray(blockSize));
                DestArray = new byte[blockSize + 100];
                target.CopyTo(DestArray);
                copyOfSource = new byte[blockSize];
                copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
                Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

            }

            //Repeat test with a new buffer to confirm that offsets within slab arrays are accurately tracked
            //and to make sure there is no off by 1 error

            {
                ManagedBuffer target = GetNewBuffer(slab);
                target2 = target;
                target.CopyFrom(GetRandomizedByteArray(blockSize));
                byte[] DestArray = new byte[blockSize];
                target.CopyTo(DestArray);
                byte[] copyOfSource = new byte[blockSize];
                byte[] copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
                Assert.IsTrue(ArraysMatch(copyOfSource, copyOfDestination));

                //Destination Array is larger than ManagedBuffer Size
                target.CopyFrom(GetRandomizedByteArray(blockSize));
                DestArray = new byte[blockSize + 1];
                target.CopyTo(DestArray);
                copyOfSource = new byte[blockSize];
                copyOfDestination = new byte[blockSize];
                Array.Copy(DestArray, 0, copyOfDestination, 0, copyOfDestination.LongLength);
                Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
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

            target.CopyFrom(GetRandomizedByteArray(blockSize));
            byte[] DestArray = new byte[blockSize];
            target.CopyTo(DestArray, 1, blockSize - 2);
            byte[] copyOfSource = new byte[blockSize - 2];
            byte[] copyOfDestination = new byte[blockSize - 2];
            Array.Copy(DestArray, 1, copyOfDestination, 0, copyOfDestination.LongLength);
            Array.Copy(target.GetArraySegments()[0].Array, target.GetArraySegments()[0].Offset, copyOfSource, 0, copyOfSource.LongLength);
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

            //Call Dispose again, should cause any exceptions to be thrown
            target.Dispose();

            //Try to work with disposed object. should throw exceptions
            {
                bool exceptionThrown = false;

                try
                {
                    target.CopyFrom(new byte[] { 0, 1, 2 });
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
                    target.GetArraySegments();
                }
                catch (ObjectDisposedException)
                {
                    exceptionThrown = true;
                }

                Assert.IsTrue(exceptionThrown);
            }


        }

        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment(int,int) is accurate")]
        public void GetArraySegmentTest()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Offset = 0;
            int Length = (int)(blockSize - 1);
            ArraySegment<byte> actual;
            actual = target.GetArraySegments(Offset, Length)[0];
            Assert.AreEqual<long>(actual.Offset, Offset + target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Length);
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array);

            //Test for full blocksize
            Offset = 0;
            Length = (int)blockSize;
            actual = target.GetArraySegments(Offset, Length)[0];
            Assert.AreEqual<long>(actual.Offset, Offset + target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Length);
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array); 


            //Test for offset of 1
            Offset = 1;
            Length = (int)(blockSize - 1);
            actual = target.GetArraySegments(Offset, Length)[0];
            Assert.AreEqual<long>(actual.Offset, Offset + target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Length);
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array); 


        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment(int,int) for zero lengths is accurate")]
        public void GetArraySegmentTest2()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Offset = 0;
            int Length = 0;
            ArraySegment<byte> actual;
            actual = target.GetArraySegments(Offset, Length)[0];
            Assert.AreEqual<long>(actual.Offset, Offset + target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Length);
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array); 

        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("GetArraySegment(int,int) with -1 offset parameter throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetArraySegmentTest3()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Offset = -1;
            int Length = 0;
            ArraySegment<byte> actual;
            actual = target.GetArraySegments(Offset, Length)[0];
        }

        [TestMethod()]
        [Description("GetArraySegment(int,int) fwith -1 length parameter throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetArraySegmentTest4()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Offset = 0;
            int Length = -1;
            ArraySegment<byte> actual;
            actual = target.GetArraySegments(Offset, Length)[0];
        }

        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment(int) is accurate")]
        public void GetArraySegmentTest5()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Length = (int)(blockSize - 1);
            ArraySegment<byte> actual;
            actual = target.GetArraySegments(Length)[0];
            Assert.AreEqual<long>(actual.Offset, target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Length);
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array); 

            //Test again for full blocksize
            Length = (int)(blockSize - 1);
            actual = target.GetArraySegments(Length)[0];
            Assert.AreEqual<long>(actual.Offset, target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Length);
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array); 

        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment(int) for zero lengths is accurate")]
        public void GetArraySegmentTest6()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Length = 0;
            ArraySegment<byte> actual;
            actual = target.GetArraySegments(Length)[0];
            Assert.AreEqual<long>(actual.Offset, 0 + target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Length);
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array); 

        }


        [TestMethod()]
        [Description("GetArraySegment(int,int) with -1 length parameter throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetArraySegmentTest7()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            int Length = -1;
            ArraySegment<byte> actual;
            actual = target.GetArraySegments(Length)[0];
        }


        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        [Description("ArraySegment returned by GetArraySegment() is accurate")]
        public void GetArraySegmentTest8()
        {
            MemorySlab slab = new MemorySlab(blockSize * 3, null);
            ManagedBuffer target = GetNewBuffer(slab);
            ArraySegment<byte> actual;
            actual = target.GetArraySegments()[0];
            Assert.AreEqual<long>(actual.Offset, target.memoryBlock.StartLocation);
            Assert.AreEqual<long>(actual.Count, Math.Min(target.Size, int.MaxValue ));
            Assert.AreEqual<byte[]>(actual.Array, target.memoryBlock.Slab.Array);

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
