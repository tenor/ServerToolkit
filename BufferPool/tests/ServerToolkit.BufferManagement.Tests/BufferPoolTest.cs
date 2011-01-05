using ServerToolkit.BufferManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ServerToolkit.BufferManagement.Tests
{
    
    
    /// <summary>
    ///This is a test class for BufferPoolTest and is intended
    ///to contain all BufferPoolTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BufferPoolTest
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
        ///A test for BufferPool Constructor
        ///</summary>
        [TestMethod()]
        public void BufferPoolConstructorTest()
        {
            long SlabSize = 0; // TODO: Initialize to an appropriate value
            int InitialSlabs = 0; // TODO: Initialize to an appropriate value
            int SubsequentSlabs = 0; // TODO: Initialize to an appropriate value
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            Assert.Inconclusive("TODO: Implement code to verify target");
        }

        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            long SlabSize = 0; // TODO: Initialize to an appropriate value
            int InitialSlabs = 0; // TODO: Initialize to an appropriate value
            int SubsequentSlabs = 0; // TODO: Initialize to an appropriate value
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs); // TODO: Initialize to an appropriate value
            target.Dispose();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for GetBuffer
        ///</summary>
        [TestMethod()]
        public void GetBufferTest()
        {
            //TODO: Must work with buffers of zero length

            long SlabSize = 0; // TODO: Initialize to an appropriate value
            int InitialSlabs = 0; // TODO: Initialize to an appropriate value
            int SubsequentSlabs = 0; // TODO: Initialize to an appropriate value
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs); // TODO: Initialize to an appropriate value
            long Size = 0; // TODO: Initialize to an appropriate value
            IBuffer expected = null; // TODO: Initialize to an appropriate value
            IBuffer actual;
            actual = target.GetBuffer(Size);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for TryFreeSlab
        ///</summary>
        [TestMethod()]
        public void TryFreeSlabTest()
        {
            long SlabSize = 0; // TODO: Initialize to an appropriate value
            int InitialSlabs = 0; // TODO: Initialize to an appropriate value
            int SubsequentSlabs = 0; // TODO: Initialize to an appropriate value
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs); // TODO: Initialize to an appropriate value
            target.TryFreeSlab();
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for InitialSlabs
        ///</summary>
        [TestMethod()]
        public void InitialSlabsTest()
        {
            long SlabSize = 0; // TODO: Initialize to an appropriate value
            int InitialSlabs = 0; // TODO: Initialize to an appropriate value
            int SubsequentSlabs = 0; // TODO: Initialize to an appropriate value
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs); // TODO: Initialize to an appropriate value
            int actual;
            actual = target.InitialSlabs;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for SlabSize
        ///</summary>
        [TestMethod()]
        public void SlabSizeTest()
        {
            long SlabSize = 0; // TODO: Initialize to an appropriate value
            int InitialSlabs = 0; // TODO: Initialize to an appropriate value
            int SubsequentSlabs = 0; // TODO: Initialize to an appropriate value
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs); // TODO: Initialize to an appropriate value
            long actual;
            actual = target.SlabSize;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for SubsequentSlabs
        ///</summary>
        [TestMethod()]
        public void SubsequentSlabsTest()
        {
            long SlabSize = 0; // TODO: Initialize to an appropriate value
            int InitialSlabs = 0; // TODO: Initialize to an appropriate value
            int SubsequentSlabs = 0; // TODO: Initialize to an appropriate value
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs); // TODO: Initialize to an appropriate value
            int actual;
            actual = target.SubsequentSlabs;
            Assert.Inconclusive("Verify the correctness of this test method.");
        }
    }
}
