using Fpl.Api.Controllers;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Fpl.Test
{
    public class Tests
    {
        private IEnumerable<int> _array;

        [SetUp]
        public void Setup()
        {
            _array = Enumerable.Range(0, 50);
        }

        [Test]
        public void GetPermutations()
        {
            var allCombinations1 = Backpack.GetPermutations(_array, 3).ToList();
            Assert.True(allCombinations1.Any());
        }
        [Test]
        public void GetCombinations()
        {
            var allCombinations2 = Backpack.Combine(_array,3).ToList();
            Assert.True(allCombinations2.Any());
        }
        [Test]
        public void CompareLength()
        {
            var generate1 = Backpack.GetPermutations(_array, 5).Count();
            var generate2 = Backpack.Combine(_array, 5).Count();

            Assert.AreEqual(generate1, generate2);
        }
    }
}