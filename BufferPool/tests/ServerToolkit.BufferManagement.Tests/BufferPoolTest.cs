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
        [Description("Proper construction for BufferPool works")]
        public void BufferPoolConstructorTest()
        {
            long SlabSize = 2 * 1024 * 1024; //2 MB
            int InitialSlabs = 1; 
            int SubsequentSlabs = 1; 
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            Assert.AreEqual<long>(InitialSlabs, target.InitialSlabs);
            Assert.AreEqual<long>(SubsequentSlabs, target.SubsequentSlabs);
            Assert.AreEqual<long>(SlabSize, target.SlabSize);
        }

        /// <summary>
        ///A test for BufferPool Constructor
        ///</summary>
        [TestMethod()]
        [Description("Construction with SlabSize = 0 throws ArgumentException")]
        [ExpectedException(typeof(ArgumentException))]
        public void BufferPoolConstructorTest2()
        {
            long SlabSize = 0;
            int InitialSlabs = 1;
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
        }


        [TestMethod()]
        [Description("Construction with SlabSize = -1 throws ArgumentException")]
        [ExpectedException(typeof(ArgumentException))]
        public void BufferPoolConstructorTest3()
        {
            long SlabSize = -1;
            int InitialSlabs = 1;
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
        }


        /// <summary>
        ///A test for BufferPool Constructor
        ///</summary>
        [TestMethod()]
        [Description("Construction with InitialSlabs = 0 throws ArgumentException")]
        [ExpectedException(typeof(ArgumentException))]
        public void BufferPoolConstructorTest4()
        {
            long SlabSize = 1;
            int InitialSlabs = 0;
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
        }


        [TestMethod()]
        [Description("Construction with InitialSlabs = -1 throws ArgumentException")]
        [ExpectedException(typeof(ArgumentException))]
        public void BufferPoolConstructorTest5()
        {
            long SlabSize = 0;
            int InitialSlabs = -1;
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
        }



        /// <summary>
        ///A test for BufferPool Constructor
        ///</summary>
        [TestMethod()]
        [Description("Construction with SubsequentSlabs = 0 throws ArgumentException")]
        [ExpectedException(typeof(ArgumentException))]
        public void BufferPoolConstructorTest6()
        {
            long SlabSize = 1;
            int InitialSlabs = 1;
            int SubsequentSlabs = 0;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
        }


        [TestMethod()]
        [Description("Construction with SubsequentSlabs = -1 throws ArgumentException")]
        [ExpectedException(typeof(ArgumentException))]
        public void BufferPoolConstructorTest7()
        {
            long SlabSize = 1;
            int InitialSlabs = 1;
            int SubsequentSlabs = -1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
        }



        /// <summary>
        ///A test for GetBuffer
        ///</summary>
        [TestMethod()]
        [Description("GetBuffer works in different scenarios")]
        public void GetBufferTest()
        {

            long SlabSize = 2 * 1024 * 1024;
            int InitialSlabs = 1; 
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            long length = 20 * 1024; 
            IBuffer actual;
            actual = target.GetBuffer(length);
            Assert.AreEqual(length, actual.Length);


            //Confirm that zero-length buffers work
            long length2 = 0;
            actual = target.GetBuffer(length2);
            Assert.AreEqual(length2, actual.Length);

            //Get another buffer and confirm that it is contiguous from the first acquired buffer. 
            //i.e. the zero buffer "in-between" didn't cause any harm
            long length3 = 100 * 1024;
            actual = target.GetBuffer(length3);
            Assert.AreEqual(length3, actual.Length);
            Assert.IsTrue(actual.GetArraySegments()[0].Offset == length);

        }

        [TestMethod]
        [Description("GetBuffer call with length = -1 throws ArgumentException")]
        [ExpectedException(typeof(ArgumentException))]
        public void GetBufferTest2()
        {

            long SlabSize = 2 * 1024 * 1024;
            int InitialSlabs = 1; 
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            long length = -1;
            IBuffer actual;
            actual = target.GetBuffer(length);


        }

        /// <summary>
        ///A test for TryFreeSlab
        ///</summary>
        [TestMethod()]
        [Description("TryFreeSlab works as expected")]
        public void TryFreeSlabTest()
        {

            long SlabSize = BufferPool.MinimumSlabSize;
            int InitialSlabs = 1;
            int SubsequentSlabs = 3;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            Assert.AreEqual<long>(InitialSlabs, target.SlabCount);

            //GetBuffer of slab size
            IBuffer buff1 = target.GetBuffer(SlabSize);
            //confirm that slabcount is 1
            Assert.AreEqual<long>(InitialSlabs, target.SlabCount);

            //Try to free a slab and do a recount -- slabcount should remain 1
            target.TryFreeSlab();
            Assert.AreEqual<long>(InitialSlabs, target.SlabCount);

            //Get it again to force construction of 3 new slabs
            IBuffer buff2 = target.GetBuffer(SlabSize);
            Assert.AreEqual<long>(SubsequentSlabs + InitialSlabs, target.SlabCount);
            
            //Free a slab and do a recount
            target.TryFreeSlab();
            Assert.AreEqual<long>(SubsequentSlabs + InitialSlabs - 1, target.SlabCount);

            //Free a slab and do a recount -- slabcount should not budge since only one slab is free
            target.TryFreeSlab();
            Assert.AreEqual<long>(SubsequentSlabs + InitialSlabs - 1, target.SlabCount);

            //Try to free a slab and do a recount
            //Free 1st buffer
            buff1.Dispose();
            target.TryFreeSlab();
            Assert.AreEqual<long>(SubsequentSlabs + InitialSlabs - 2, target.SlabCount);

            //Try to free a slab and do a recount -- slabcount shouldn't go below two
            target.TryFreeSlab();
            Assert.AreEqual<long>(SubsequentSlabs + InitialSlabs - 2, target.SlabCount);

            //Free buffer, try to free a slab and do a recount -- slabcount should now be back to 1
            buff2.Dispose();
            target.TryFreeSlab();
            Assert.AreEqual<long>(SubsequentSlabs + InitialSlabs - 3, target.SlabCount);

        }

        /// <summary>
        ///A test for InitialSlabs
        ///</summary>
        [TestMethod()]
        [Description("InitialSlabs property matches constructor parameter")]
        public void InitialSlabsTest()
        {
            long SlabSize = 1024; //1 KB
            int InitialSlabs = 41;
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            Assert.AreEqual<long>(InitialSlabs, target.InitialSlabs);
        }

        /// <summary>
        ///A test for SlabSize
        ///</summary>
        [TestMethod()]
        [Description("SlabSize property matches constructor parameter")]
        public void SlabSizeTest()
        {

            //Test for slab of minimum slab size
            long SlabSize = 1; //1 byte
            int InitialSlabs = 1;
            int SubsequentSlabs = 1;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            Assert.AreEqual<long>(Math.Max(SlabSize, BufferPool.MinimumSlabSize),target.SlabSize);

            //Test for slab of minimum slab size + 1 byte
            SlabSize = BufferPool.MinimumSlabSize + 1;
            InitialSlabs = 1;
            SubsequentSlabs = 1;
            target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            Assert.AreEqual<long>(Math.Max(SlabSize, BufferPool.MinimumSlabSize), target.SlabSize);
        }

        /// <summary>
        ///A test for SubsequentSlabs
        ///</summary>
        [TestMethod()]
        [Description("SubsequentSlabs property matches constructor parameter and is used as expected")]
        public void SubsequentSlabsTest()
        {
            long SlabSize = BufferPool.MinimumSlabSize; //Minimum Slab size
            int InitialSlabs = 1;
            int SubsequentSlabs = 41;
            BufferPool target = new BufferPool(SlabSize, InitialSlabs, SubsequentSlabs);
            target.GetBuffer(SlabSize);
            target.GetBuffer(SlabSize);
            Assert.AreEqual<long>(SubsequentSlabs, target.SubsequentSlabs);
            Assert.AreEqual<long>(SubsequentSlabs + InitialSlabs, target.SlabCount);
        }
    }
}
