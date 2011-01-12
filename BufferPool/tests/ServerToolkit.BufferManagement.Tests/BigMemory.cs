using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ServerToolkit.BufferManagement.Tests
{
    /// <summary>
    /// Summary description for BigMemory
    /// </summary>
    [TestClass]
    public class BigMemory
    {
        //TODO: Large Memory tests

        /*
         * 
         * 2. Memory test:
Create buffers that are greater than the 32 bit space. Check to make sure buffers are properly allocated. Check to see that all relevant methods work smoothly. Check to see that new slabs are created without problems.

3. Slab destruction test on a massive level:
Create a whole bunch of slabs.
Free them one by one. Keep freeing to see if it waits until there are two free slabs before freeing a slab and that it doesn't go below two.

4. When you need to send a message larger than 2GB. See if ArraySegment can start at a long length greater than the 2GB limit and verify it's content.
         * 
         */

        public BigMemory()
        {
            //
            // TODO: Add constructor logic here
            //
        }

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
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestMethod1()
        {
            //
            // TODO: Add test logic here
            //
        }
    }
}
