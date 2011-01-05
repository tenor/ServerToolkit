using ServerToolkit.BufferManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ServerToolkit.BufferManagement.Tests
{
    
    
    /// <summary>
    ///This is a test class for IMemorySlabTest and is intended
    ///to contain all IMemorySlabTest Unit Tests
    ///</summary>
    [TestClass()]
    public class IMemorySlabTest
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


        internal long totalLen = 200000;

        internal virtual IMemorySlab CreateIMemorySlab()
        {

            IMemorySlab target = new MemorySlab(totalLen, null);
            return target;
        }

        /// <summary>
        ///A test for Free
        ///</summary>
        [TestMethod()]
        public void FreeTest()
        {
            //TODO: Figure out a LIGHTWEIGHT way to make sure that once a memoryblock is freed. 
                    //a). It cannot be reused
                    //b). It cannot be freed again
                    //c). Mechanism should be lightweight check but throw exceptions since buffer handles this developer-side

            //TODO: Create tests that scan all allocated and free areas for overlap during free.

            IMemorySlab target = CreateIMemorySlab(); // TODO: Initialize to an appropriate value
            IMemoryBlock AllocatedBlock = null; // TODO: Initialize to an appropriate value
            target.Free(AllocatedBlock);
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for TryAllocate
        ///</summary>
        [TestMethod()]
        public void TryAllocateTest()
        {
            IMemorySlab target = CreateIMemorySlab();
            
            //allocate 1 byte
            long smallLength = 1; 
            IMemoryBlock allocatedBlock = null;
            bool result1 = target.TryAllocate(smallLength, out allocatedBlock);
            Assert.AreEqual<long>(smallLength, allocatedBlock.Length);
            Assert.AreEqual<long>(0, allocatedBlock.StartLocation);
            Assert.AreEqual<bool>(true, result1);

            //allocate rest of slab
            long restLength = totalLen - 1; 
            IMemoryBlock allocatedBlock2 = null;
            bool result2 = target.TryAllocate(restLength, out allocatedBlock2);
            Assert.AreEqual<long>(restLength, allocatedBlock2.Length);
            Assert.AreEqual<long>(1, allocatedBlock2.StartLocation);
            Assert.AreEqual<bool>(true, result2);

            //Now try to allocate another byte and expect it to fail
            IMemoryBlock allocatedBlock3 = null;
            bool result3 = target.TryAllocate(smallLength, out allocatedBlock3);
            Assert.AreEqual<bool>(false, result3);

            //Clean up
            target.Free(allocatedBlock);
            target.Free(allocatedBlock2);

        }

        /// <summary>
        ///A test for TryAllocate
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TryAllocateTest2()
        {
            IMemorySlab target = CreateIMemorySlab();

            //allocate invalid length
            long badLength = 0;
            IMemoryBlock allocatedBlock = null;
            bool result1 = target.TryAllocate(badLength, out allocatedBlock);

        }

        /// <summary>
        ///A test for TryAllocate
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TryAllocateTest3()
        {
            IMemorySlab target = CreateIMemorySlab();

            //allocate invalid length
            long badLength = -1;
            IMemoryBlock allocatedBlock = null;
            bool result1 = target.TryAllocate(badLength, out allocatedBlock);

        }



        /// <summary>
        ///A test for Array
        ///</summary>
        [TestMethod()]
        public void ArrayTest()
        {
            IMemorySlab target = CreateIMemorySlab(); 
            byte[] actual;
            actual = target.Array;
            Assert.AreEqual<long>(totalLen, actual.LongLength);
        }

        /// <summary>
        ///A test for LargestFreeBlockSize
        ///</summary>
        [TestMethod()]
        public void LargestFreeBlockSizeTest()
        {
            IMemorySlab target = CreateIMemorySlab(); 
            long actual;
            actual = target.LargestFreeBlockSize;
            Assert.AreEqual<long>(totalLen, actual);

            IMemoryBlock allocatedBlock;
            target.TryAllocate(4321, out allocatedBlock);
            actual = target.LargestFreeBlockSize;
            Assert.AreEqual<long>(totalLen - 4321, actual);

            IMemoryBlock allocatedBlock2;
            target.TryAllocate(19500, out allocatedBlock2);
            actual = target.LargestFreeBlockSize;
            Assert.AreEqual<long>(totalLen - 4321 - 19500 , actual);

            target.Free(allocatedBlock);

            Assert.AreEqual<long>((totalLen - 4321 - 19500) > 4321 ? (totalLen - 4321 - 19500) : 4321  , actual);

            target.Free(allocatedBlock2);


        }

        /// <summary>
        ///A test for TotalLength
        ///</summary>
        [TestMethod()]
        public void TotalLengthTest()
        {
            IMemorySlab target = CreateIMemorySlab(); 
            long actual;
            actual = target.TotalLength;
            Assert.AreEqual<long>(totalLen, actual);
        }
    }
}
