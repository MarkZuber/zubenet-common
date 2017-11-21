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

using System.IO;
using System.IO.Compression;

namespace ZubeNet.Common.Concrete
{
    public sealed class GZipUtils : IGZipUtils
    {
        public void GZipCompressFile(byte[] fileBytes, string outputFilePath)
        {
            EnsureDirectoryExists(outputFilePath);
            using (var originalFileStream = new MemoryStream(fileBytes))
            {
                using (var compressedStream = File.Create(outputFilePath))
                {
                    using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal))
                    {
                        originalFileStream.CopyTo(gzipStream);
                        gzipStream.Flush();
                    }
                }
            }
        }

        public void GZipCompressFile(string filePath, string outputFilePath)
        {
            var bytes = File.ReadAllBytes(filePath);
            GZipCompressFile(bytes, outputFilePath);
        }

        public void GZipDecompressFile(string compressedFilePath, string outputFilePath)
        {
            EnsureDirectoryExists(outputFilePath);
            using (var compressedStream = File.OpenRead(compressedFilePath))
            {
                using (var decompressedStream = File.Create(outputFilePath))
                {
                    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        gzipStream.CopyTo(decompressedStream);
                        decompressedStream.Flush();
                    }
                }
            }
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}