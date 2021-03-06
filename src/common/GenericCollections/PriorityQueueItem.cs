﻿// MIT License
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

using System.Collections.Generic;

namespace ZubeNet.Common.GenericCollections
{
    public class PriorityQueueItem<T> : IPriorityQueueItem<T>
    {
        public PriorityQueueItem(T item)
        {
            Item = item;
            Index = -1; // Uninitialized
        }

        public T Item { get; }

        public int Index { get; set; }

        public override string ToString()
        {
            return $"[{Index}]: Item {Item}";
        }

        protected bool Equals(PriorityQueueItem<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Item, other.Item);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != typeof(PriorityQueueItem<T>))
            {
                return false;
            }
            return Equals((PriorityQueueItem<T>)obj);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(Item);
        }

        public static bool operator ==(PriorityQueueItem<T> left, PriorityQueueItem<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PriorityQueueItem<T> left, PriorityQueueItem<T> right)
        {
            return !Equals(left, right);
        }
    }
}