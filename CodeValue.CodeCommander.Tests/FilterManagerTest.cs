using System.Linq;
using CodeValue.CodeCommander;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CodeValue.CodeCommander.Interfaces;
using Telerik.JustMock;
using Telerik.JustMock.Helpers;

namespace CodeValue.CodeCommander.Tests
{
    
    
    /// <summary>
    ///This is a test class for FilterManagerTest and is intended
    ///to contain all FilterManagerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class FilterManagerTest
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
        ///A test for AddFilter
        ///</summary>
        [TestMethod()]
        public void AddFilter_AddingAFilterForEmptyManager_FilterManagerHasOneFilter()
        {
            // Arrange
            FilterManager target = new FilterManager(); 
            var filter = Mock.Create<IFilter>();
            // Act
            target.AddFilter(filter);
            // Assert
            Assert.IsTrue(target.CurrentFilters.Contains(filter));
        }

        [TestMethod()]
        public void AddFilter_AddingAFilterWhenFilterIsAlreadyAdded_FilterManagerHasOneFilter()
        {
            // Arrange
            FilterManager target = new FilterManager();
            var filter = Mock.Create<IFilter>();
            // Act
            target.AddFilter(filter);
            target.AddFilter(filter);
            // Assert
            Assert.IsTrue(target.CurrentFilters.Length == 1);
        }

        [TestMethod()]
        public void RemoveFilter_RemovingAFilterWhenFilterIsEmpty_FilterManagerRemainEmptyFilters()
        {
            // Arrange
            FilterManager target = new FilterManager();
            var filter = Mock.Create<IFilter>();
            // Act
            target.RemoveFilter(filter);
            // Assert
            Assert.IsTrue(target.CurrentFilters.Length == 0);
        }


        [TestMethod()]
        public void RemoveFilter_RemovingAFilterWhenFilterExist_FilterManagerRemainEmptyFilters()
        {
            // Arrange
            FilterManager target = new FilterManager();
            var filter = Mock.Create<IFilter>();
            target.AddFilter(filter);

            // Act
            
            target.RemoveFilter(filter);
            // Assert
            Assert.IsTrue(target.CurrentFilters.Length == 0);
        }

        [TestMethod()]
        public void RemoveFilter_RemovingAFilterWhenOtherFilterExist_FilterManagerShouldContainThatFilters()
        {
            // Arrange
            FilterManager target = new FilterManager();
            var filter = Mock.Create<IFilter>();
            var filter2 = Mock.Create<IFilter>();
            target.AddFilter(filter);
            target.AddFilter(filter2);

            // Act

            target.RemoveFilter(filter);
            // Assert
            Assert.IsTrue(target.CurrentFilters.Contains(filter2));
            Assert.IsFalse(target.CurrentFilters.Contains(filter));
        }

        [TestMethod()]
        public void Process_CommandIsPending_ShouldCallProcessFilters()
        {
            // Arrange
            FilterManager target = new FilterManager();
            bool expected = false;
            //Mock.Arrange(() => target.ProcessFilters(null))                       
            var command = Mock.Create<CommandBase>();
            Mock.Arrange(() => command.CurrentState).Returns(CommandState.Pending);

            // Act
            
            var result = target.Process(command);

            // Assert
            Assert.IsTrue(result);
        }


    }
}
