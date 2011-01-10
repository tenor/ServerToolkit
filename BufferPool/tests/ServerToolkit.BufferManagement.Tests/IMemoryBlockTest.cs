using ServerToolkit.BufferManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ServerToolkit.BufferManagement.Tests
{
    
    
    /// <summary>
    ///This is a test class for IMemoryBlockTest and is intended
    ///to contain all IMemoryBlockTest Unit Tests
    ///</summary>
    [TestClass()]
    public class IMemoryBlockTest
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


        internal long startLoc = 23;
        internal long length = 100567;
        internal IMemorySlab slab = new MemorySlab(200000, null);

        internal virtual IMemoryBlock CreateIMemoryBlock()
        {
            IMemoryBlock target = new MemoryBlock(startLoc, length, slab);
            return target;
        }


        internal virtual IMemoryBlock CreateInvalidIMemoryBlock_BadStartLoc()
        {
            IMemoryBlock target = new MemoryBlock(-1, length, slab);
            return target;
        }

        internal virtual IMemoryBlock CreateInvalidMemoryBlock_BadLength2()
        {
            IMemoryBlock target = new MemoryBlock(startLoc, 0, slab);
            return target;
        }

        internal virtual IMemoryBlock CreateInvalidMemoryBlock_BadLength()
        {
            IMemoryBlock target = new MemoryBlock(startLoc, -1, slab);
            return target;
        }

        internal virtual IMemoryBlock CreateInvalidMemoryBlock_NullSlab()
        {
            IMemoryBlock target = new MemoryBlock(startLoc, length, null);
            return target;
        }

        /// <summary>
        /// Test for invalid construction 1
        /// </summary>
        [TestMethod]
        [Description("Construction with negative StartLocation parameter throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void BadConstructionTest1()
        {
            CreateInvalidIMemoryBlock_BadStartLoc();
        }


        /// <summary>
        /// Test for invalid construction 2
        /// </summary>
        [TestMethod]
        [Description("Construction with Length parameter of -1 throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void BadConstructionTest2()
        {
            CreateInvalidMemoryBlock_BadLength();
        }


        /// <summary>
        /// Test for invalid construction 2
        /// </summary>
        [TestMethod]
        [Description("Construction with Length parameter of 0 throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void BadConstructionTest3()
        {
            CreateInvalidMemoryBlock_BadLength2();
        }

        /// <summary>
        /// Test for invalid construction 2
        /// </summary>
        [TestMethod]
        [Description("Construction with null Slab parameter throws exception")]
        [ExpectedException(typeof(ArgumentNullException))]
        public void BadConstructionTest4()
        {
            CreateInvalidMemoryBlock_NullSlab();
        }


        /// <summary>
        ///A test for EndLocation
        ///</summary>
        [TestMethod()]
        [Description("EndLocation property is accurately calculated")]
        public void EndLocationTest()
        {
            //Correct Property
            IMemoryBlock target = CreateIMemoryBlock();
            long actual;
            actual = target.EndLocation;
            Assert.AreEqual<long>(startLoc + length - 1, actual);

        }

        /// <summary>
        ///A test for Length
        ///</summary>
        [TestMethod()]
        [Description("Length property matches constructor parameter")]
        public void LengthTest()
        {
            IMemoryBlock target = CreateIMemoryBlock(); 
            long actual;
            actual = target.Length;
            Assert.AreEqual<long>(length, actual);
        }

        /// <summary>
        ///A test for Slab
        ///</summary>
        [TestMethod()]
        [Description("Slab property matches constructor parameter")]
        public void SlabTest()
        {
            IMemoryBlock target = CreateIMemoryBlock(); 
            IMemorySlab actual;
            actual = target.Slab;
            Assert.AreEqual<IMemorySlab>(slab, actual);
        }

        /// <summary>
        ///A test for StartLocation
        ///</summary>
        [TestMethod()]
        [Description("StartLocation property matches constructor parameter")]
        public void StartLocationTest()
        {
            IMemoryBlock target = CreateIMemoryBlock();
            long actual;
            actual = target.StartLocation;
            Assert.AreEqual<long>(startLoc, actual);
        }
    }
}
