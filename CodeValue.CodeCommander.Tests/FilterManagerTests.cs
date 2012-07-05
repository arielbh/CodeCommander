using System;
using System.Reactive;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeValue.CodeCommander.Interfaces;
using CodeValue.CodeCommander.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeValue.CodeCommander.Tests
{
    [TestClass]
    public class FilterManagerTests
    {
        [TestMethod]
        public void AddFilter_WithEmptyFilter_ItemIsAdded()
        {
            bool wasAdded = false;
            var filter = new EmptyFilter();

            FilterManager filterManager = new FilterManager();
            filterManager.ItemsAdded.Subscribe(Observer.Create<IFilter>(_ => wasAdded = true));

            filterManager.AddFilter(filter);

            Assert.IsTrue(wasAdded);
        }

        [TestMethod]
        public void AddFilter_WithEmptyFilter_AddedItemIsTheSame()
        {
            bool areSame = false;
            var filter = new EmptyFilter();

            FilterManager filterManager = new FilterManager();
            filterManager.ItemsAdded.Subscribe(Observer.Create<IFilter>(f => areSame = filter == f));

            filterManager.AddFilter(filter);

            Assert.IsTrue(areSame);
        }

        [TestMethod]
        public void RemoveFilter_WithEmptyFilter_ItemIsRemoved()
        {
            bool wasRemoved = false;
            var filter = new EmptyFilter();

            FilterManager filterManager = new FilterManager();
            filterManager.AddFilter(filter);

            filterManager.ItemsRemoved.Subscribe(Observer.Create<IFilter>(_ => wasRemoved = true));

            filterManager.RemoveFilter(filter);

            Assert.IsTrue(wasRemoved);
        }


        [TestMethod]
        public void RemoveFilter_WithEmptyFilter_RemovedItemIsTheSame()
        {
            bool areSame = false;

            var filter = new EmptyFilter();

            FilterManager filterManager = new FilterManager();
            filterManager.AddFilter(filter);

            filterManager.ItemsRemoved.Subscribe(Observer.Create<IFilter>(f => areSame = filter == f));

            filterManager.RemoveFilter(filter);

            Assert.IsTrue(areSame);
        }

        [TestMethod]
        public void ItemsChanged_WithEmptyFilter_ChangedItemsIsCalledForAddAndRemove()
        {
            int timesCalled = 0;
            var filter = new EmptyFilter();

            FilterManager filterManager = new FilterManager();
            filterManager.ItemsChanged.Subscribe(Observer.Create<IFilter>(_ => ++timesCalled));

            filterManager.AddFilter(filter);
            filterManager.RemoveFilter(filter);

            Assert.AreEqual(2, timesCalled);
        }

        [TestMethod]
        public void Process_CommandIsNotPending_ReturnFalse()
        {
            var command = new TestCommand(CommandState.New);

            FilterManager filterManager = new FilterManager();
            var result = filterManager.Process(command);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Process_CommandIsPendingWithNoFilters_ReturnTrue()
        {
            var command = new TestCommand(CommandState.Pending);

            FilterManager filterManager = new FilterManager();
            var result = filterManager.Process(command);
            Assert.IsTrue(result);
        }


        [TestMethod]
        public void Process_CommandIsPendingWitWithOnePassingFilter_CommandNotFiltered()
        {
            var command = new TestCommand(CommandState.Pending);

            FilterManager filterManager = new FilterManager();
            var filter = new GenericFilter(c => true);
            filterManager.AddFilter(filter);
            var result = filterManager.Process(command);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void Process_CommandIsPendingWithOneNotPassingFilter_CommandIsFiltered()
        {
            var command = new TestCommand(CommandState.Pending);

            FilterManager filterManager = new FilterManager();
            var filter = new GenericFilter(c => false);
            filterManager.AddFilter(filter);

            var result = filterManager.Process(command);
            Assert.IsFalse(result);
        }
        /// <summary>
        /// We are calling the FilterManager.Process in another thread so we simulate filters being added during the Process itself.
        /// </summary>
        [TestMethod]
        public void Process_NonPassingFilterAddedDuringCommandProcessing_CommandIsFiltered()
        {
            var command = new TestCommand(CommandState.Pending);

            FilterManager filterManager = new FilterManager();
            ManualResetEventSlim evt = new ManualResetEventSlim(false);
            var filter = new GenericFilter(c =>
                {
                    evt.Wait();
                    return true;
                });

            filterManager.AddFilter(filter);

            Task<bool> result = Task.Factory.StartNew(() => filterManager.Process(command));

            filterManager.AddFilter(new GenericFilter(c => false));
            evt.Set();

            Assert.IsFalse(result.Result);
        }


        public class EmptyFilter : IFilter
        {
            public double Order
            {
                get { return 0; }
            }

            public string Name
            {
                get { return string.Empty; }
            }

            public bool Process(ICommandBase command)
            {
                throw new NotImplementedException();
            }
        }
    }
}