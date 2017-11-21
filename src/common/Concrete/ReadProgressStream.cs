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
using System.ComponentModel;
using System.IO;

namespace ZubeNet.Common.Concrete
{
    public class ReadProgressStream : ContainerStream
    {
        private int _lastProgress;

        public ReadProgressStream(Stream stream)
            : base(stream)
        {
            if (stream.Length <= 0 || !stream.CanRead)
            {
                throw new ArgumentException("Stream length is negative or stream can't be read.");
            }
        }

        public event ProgressChangedEventHandler ProgressChanged;

        public override int Read(byte[] buffer, int offset, int count)
        {
            int amountRead = base.Read(buffer, offset, count);
            if (ProgressChanged != null)
            {
                int newProgress = (int)(Position * 100.0 / Length);
                if (newProgress > _lastProgress)
                {
                    _lastProgress = newProgress;
                    ProgressChanged(this, new ProgressChangedEventArgs(_lastProgress, null));
                }
            }
            return amountRead;
        }
    }
}