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


        /// <summary>
        ///A test for Buffer Constructor
        ///</summary>
        [TestMethod()]
        public void BufferConstructorTest()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock);
            Assert.Inconclusive("TODO: Implement code to verify target");
        }

        /// <summary>
        ///A test for CopyFrom
        ///</summary>
        [TestMethod()]
        public void CopyFromTest()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            byte[] SourceArray = null; // TODO: Initialize to an appropriate value
            target.CopyFrom(SourceArray);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for CopyFrom
        ///</summary>
        [TestMethod()]
        public void CopyFromTest1()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            byte[] SourceArray = null; // TODO: Initialize to an appropriate value
            long SourceIndex = 0; // TODO: Initialize to an appropriate value
            long Length = 0; // TODO: Initialize to an appropriate value
            target.CopyFrom(SourceArray, SourceIndex, Length);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for CopyTo
        ///</summary>
        [TestMethod()]
        public void CopyToTest()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            byte[] DestinationArray = null; // TODO: Initialize to an appropriate value
            long DestinationIndex = 0; // TODO: Initialize to an appropriate value
            long Length = 0; // TODO: Initialize to an appropriate value
            target.CopyTo(DestinationArray, DestinationIndex, Length);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for CopyTo
        ///</summary>
        [TestMethod()]
        public void CopyToTest1()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            byte[] DestinationArray = null; // TODO: Initialize to an appropriate value
            target.CopyTo(DestinationArray);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            target.Dispose();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        public void GetArraySegmentTest()
        {
            //TODO: Make sure zero lengths are allowed

            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            int Offset = 0; // TODO: Initialize to an appropriate value
            int Length = 0; // TODO: Initialize to an appropriate value
            ArraySegment<byte> expected = new ArraySegment<byte>(); // TODO: Initialize to an appropriate value
            ArraySegment<byte> actual;
            actual = target.GetArraySegment(Offset, Length);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        public void GetArraySegmentTest1()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            ArraySegment<byte> expected = new ArraySegment<byte>(); // TODO: Initialize to an appropriate value
            ArraySegment<byte> actual;
            actual = target.GetArraySegment();
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for GetArraySegment
        ///</summary>
        [TestMethod()]
        public void GetArraySegmentTest2()
        {
            //TODO: Make sure zero lengths are allowed

            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            int Length = 0; // TODO: Initialize to an appropriate value
            ArraySegment<byte> expected = new ArraySegment<byte>(); // TODO: Initialize to an appropriate value
            ArraySegment<byte> actual;
            actual = target.GetArraySegment(Length);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for IsDisposed
        ///</summary>
        [TestMethod()]
        public void IsDisposedTest()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            bool actual;
            actual = target.IsDisposed;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for Length
        ///</summary>
        [TestMethod()]
        public void LengthTest()
        {
            IMemoryBlock AllocatedMemoryBlock = null; // TODO: Initialize to an appropriate value
            Buffer target = new Buffer(AllocatedMemoryBlock); // TODO: Initialize to an appropriate value
            long actual;
            actual = target.Length;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }
    }
}
