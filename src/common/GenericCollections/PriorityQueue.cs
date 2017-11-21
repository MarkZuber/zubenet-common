// MIT License
// 
// Copyright (c) 2017 Mark Zuber
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZubeNet.Common.GenericCollections
{
    /// <summary>
    ///     .NET doesn't have a PQ, so we wrote a minimalistic min-heap.
    ///     Inspired by C5's priority queue interface.
    ///     This is a min-heap using the provided ordering.
    /// </summary>
    public class PriorityQueue<T> : IPriorityQueue<T>
    {
        internal const int InvalidatedIndex = -1;
        private readonly List<PriorityQueueItem<T>> _heap;

        // Default initial capacity is 4, which is the initial capacity for List when first item is added.
        public PriorityQueue(int initialCapacity = 4)
            : this(Comparer<T>.Default, initialCapacity)
        {
        }

        public PriorityQueue(Comparer<T> comparer, int initialCapacity = 4)
        {
            Comparer = comparer;
            _heap = new List<PriorityQueueItem<T>>(initialCapacity);
        }

        public Comparer<T> Comparer { get; private set; }

        public int Count => _heap.Count;

        public IPriorityQueueItem<T> Poll()
        {
            if (_heap.Count == 0)
            {
                return null;
            }
            var handle = _heap[0];
            Remove(0);
            return handle;
        }

        /// <summary>
        ///     Add an item to the priority queue.
        /// </summary>
        /// <param name="item">
        ///     Accepts null as an item
        /// </param>
        public IPriorityQueueItem<T> Add(T item)
        {
            var handle = new PriorityQueueItem<T>(item);
            // Add to last element of array, then percolate up
            _heap.Add(handle);
            handle.Index = _heap.Count - 1;
            PercolateUp(_heap.Count - 1);
            return handle;
        }

        /// <summary>
        ///     Retrieves head of the queue.
        /// </summary>
        /// <returns>
        ///     null if queue is empty.
        /// </returns>
        public IPriorityQueueItem<T> Peek()
        {
            if (_heap.Count == 0)
            {
                return null;
            }
            return _heap[0];
        }

        public void Clear()
        {
            _heap.Clear();
        }

        public void Remove(IPriorityQueueItem<T> item)
        {
            // Check
            if (item.Index < 0 || item.Index >= _heap.Count)
            {
                throw new ArgumentException("handle does not exist in heap.", "item");
            }
            if (item.Item is ValueType)
            {
                if (!_heap[item.Index].Item.Equals(item.Item))
                {
                    throw new ArgumentException(string.Format("handle has unexpected item: heap {0} vs handle {1}.", _heap[item.Index].Item, item.Item));
                }
            }
            else
            {
                if (!ReferenceEquals(_heap[item.Index].Item, item.Item))
                {
                    throw new ArgumentException(string.Format("handle has unexpected item: heap {0} vs handle {1}.", _heap[item.Index].Item, item.Item));
                }
            }

            Remove(item.Index);
        }

        public IEnumerator<IPriorityQueueItem<T>> GetEnumerator()
        {
            return _heap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (this as IEnumerable<IPriorityQueueItem<T>>).GetEnumerator();
        }

        private T GetItem(int index)
        {
            return _heap[index].Item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ParentIndex(int index)
        {
            return (index - 1) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int LeftIndex(int index)
        {
            return (2 * index) + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int RightIndex(int index)
        {
            return (2 * index) + 2;
        }

        private void PercolateUp(int index)
        {
            while (index > 0)
            {
                int parent = ParentIndex(index);
                if (Comparer.Compare(GetItem(parent), GetItem(index)) <= 0)
                {
                    // Parent is smaller than child, so don't continue.
                    break;
                }
                Swap(index, parent);
                index = parent;
            }
        }

        private void PercolateDown(int index)
        {
            // -1 to include index
            while (index < _heap.Count - 1)
            {
                int left = LeftIndex(index);
                int right = RightIndex(index);

                int pick = left;
                // Get the min of the two
                if (right < _heap.Count && Comparer.Compare(GetItem(pick), GetItem(right)) > 0)
                {
                    pick = right;
                }
                // i.e. If child is smaller
                if (pick < _heap.Count && Comparer.Compare(GetItem(pick), GetItem(index)) < 0)
                {
                    Swap(pick, index);
                    index = pick;
                }
                else
                {
                    break;
                }
            }
        }

        private void Remove(int index)
        {
            // Move last item to index, and then percolate down
            int lastIndex = _heap.Count - 1;
            Swap(index, lastIndex);
            _heap[lastIndex].Index = InvalidatedIndex; // Invalidate Index
            _heap.RemoveAt(lastIndex); // Remove the recently swapped index-item
            if (index < lastIndex)
            {
                PercolateDown(index);
            }
        }

        private void Swap(int i, int j)
        {
            var tmp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = tmp;

            _heap[j].Index = j;
            _heap[i].Index = i;
        }

        public override string ToString()
        {
            return _heap.ToString();
        }
    }
}