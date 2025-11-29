using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using System;

namespace Electra.DataStructures.Tests
{
    public class BinaryHeapTests
    {
        [Fact]
        public void MinHeap_Extract_Returns_Minimum()
        {
            // Arrange
            var heap = new BinaryHeap<int>(HeapType.MinHeap);
            heap.Insert(5);
            heap.Insert(3);
            heap.Insert(8);
            heap.Insert(1);

            // Act
            var min = heap.Extract();

            // Assert
            min.Should().Be(1);
            heap.Peek().Should().Be(3);
        }

        [Fact]
        public void MaxHeap_Extract_Returns_Maximum()
        {
            // Arrange
            var heap = new BinaryHeap<int>(HeapType.MaxHeap);
            heap.Insert(5);
            heap.Insert(3);
            heap.Insert(8);
            heap.Insert(1);

            // Act
            var max = heap.Extract();

            // Assert
            max.Should().Be(8);
            heap.Peek().Should().Be(5);
        }
    }
}
