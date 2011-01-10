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


        internal long totalSize = 200000;

        internal virtual IMemorySlab CreateIMemorySlab()
        {

            IMemorySlab target = new MemorySlab(totalSize, null);
            return target;
        }

        internal virtual IMemorySlab CreateInvalidMemorySlab1()
        {
            IMemorySlab target = new MemorySlab(-1, null);
            return target;
        }

        internal virtual IMemorySlab CreateInvalidMemorySlab2()
        {
            IMemorySlab target = new MemorySlab(0, null);
            return target;
        }


        /// <summary>
        ///A test for Free
        ///</summary>
        [TestMethod()]
        [Description("Free method works as expected in different scenarios")]
        public void FreeTest()
        {
            IMemorySlab target = CreateIMemorySlab();
            IMemoryBlock block1, block2, block3, block4, block5, block6;
            target.TryAllocate(10,out block1);
            target.TryAllocate(10, out block2);
            target.TryAllocate(10, out block3);
            target.TryAllocate(10, out block4);
            target.TryAllocate(10, out block5);
            target.TryAllocate(target.LargestFreeBlockSize, out block6); 
            //entire slab is now used up

            //Free block1
            target.Free(block1);
            //reassign block1 to be 4 bytes;
            target.TryAllocate(4, out block1);
            //Free space should be 6 bytes now;
            Assert.AreEqual<long>(6, target.LargestFreeBlockSize);
            //Free Block2
            target.Free(block2);
            //Free space should now be 16 bytes;
            Assert.AreEqual<long>(16, target.LargestFreeBlockSize);
            //Free Block5
            target.Free(block5);
            //Free Block4
            target.Free(block4);
            //Largest free space should now be 20 bytes;
            Assert.AreEqual<long>(20, target.LargestFreeBlockSize);
            //Free Block3
            target.Free(block3);
            //Largest free space should now be 20 + 10 + 16 = 46 bytes
            Assert.AreEqual<long>(46, target.LargestFreeBlockSize);
            //Free Block6
            target.Free(block6);
            //Largest free block should be slab size - block1 size
            Assert.AreEqual<long>(target.Size - block1.Length, target.LargestFreeBlockSize);
            //Free Block1
            target.Free(block1);
            //Largest free block should now be slab size
            Assert.AreEqual<long>(target.Size, target.LargestFreeBlockSize);
        }

        /// <summary>
        ///A test for TryAllocate
        ///</summary>
        [TestMethod()]
        [Description("TryAllocate behaves as expected in different scenarios")]
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
            long restLength = totalSize - 1; 
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
        [Description("Allocation of zero length block throws exception")]
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
        [Description("Allocation of negative length block throws exception")]
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
        [Description("Array length matches Size parameter in constructor")]
        public void ArrayTest()
        {
            IMemorySlab target = CreateIMemorySlab(); 
            byte[] actual;
            actual = target.Array;
            Assert.AreEqual<long>(totalSize, actual.LongLength);
        }

        /// <summary>
        ///A test for LargestFreeBlockSize
        ///</summary>
        [TestMethod()]
        [Description("LargestFreeBlockSize returns the expected value in different scenarios")]
        public void LargestFreeBlockSizeTest()
        {
            IMemorySlab target = CreateIMemorySlab(); 
            long actual;

            //LargestFreeBlockSize should be initially the total size of the slab
            actual = target.LargestFreeBlockSize;
            Assert.AreEqual<long>(totalSize, actual);

            //Allocate a small block, LargestFreeBlockSize should be the difference from the total slab size
            IMemoryBlock allocatedBlock;
            target.TryAllocate(4321, out allocatedBlock);
            actual = target.LargestFreeBlockSize;
            Assert.AreEqual<long>(totalSize - 4321, actual);

            //Allocate a bigger block, LargestFreeBlockSize should decrease by that block size
            IMemoryBlock allocatedBlock2;
            target.TryAllocate(19500, out allocatedBlock2);
            actual = target.LargestFreeBlockSize;
            Assert.AreEqual<long>(totalSize - 4321 - 19500 , actual);

            //Free the original small allocation
            target.Free(allocatedBlock);

            //LargestFreeBlockSize should be the greater of it's previous and the freed block
            Assert.AreEqual<long>(Math.Max( 4321 , (totalSize - 4321 - 19500))  , actual);

            target.Free(allocatedBlock2);


        }

        /// <summary>
        ///A test for Size
        ///</summary>
        [TestMethod()]
        [Description("Size property matches constructor parameter")]
        public void SizeTest()
        {
            IMemorySlab target = CreateIMemorySlab(); 
            long actual;
            actual = target.Size;
            Assert.AreEqual<long>(totalSize, actual);
        }


        [TestMethod]
        [Description("Construction with a Length parameter of -1 throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void BadConstructionTest1()
        {
            IMemorySlab target = CreateInvalidMemorySlab1(); 
        }

        [TestMethod]
        [Description("Construction with a Length parameter of 0 throws exception")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void BadConstructionTest2()
        {
            IMemorySlab target = CreateInvalidMemorySlab2();
        }

    }
}
