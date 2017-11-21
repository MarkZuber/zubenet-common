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
using System.IO;
using System.IO.Compression;

namespace ZubeNet.Common.Concrete
{
    public sealed class ZipUtils : IZipUtils
    {
        public void ExtractAll(string inputZipFilename, string outputDirectoryName)
        {
            ZipFile.ExtractToDirectory(inputZipFilename, outputDirectoryName);
        }

        public int CreateZipFile(string zipFilePath, string sourcePath, Action<ZipProgress> zipProgressAction)
        {
            var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
            int totalFiles = files.Length;
            int currentFileIdx = 0;

            using (var zipOutputStream = new FileStream(zipFilePath, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipOutputStream, ZipArchiveMode.Create))
                {
                    foreach (string file in files)
                    {
                        string blobName = file.Substring(sourcePath.Length);

                        if (blobName.StartsWith(@"\", StringComparison.InvariantCultureIgnoreCase))
                        {
                            blobName = blobName.Substring(1);
                        }

                        blobName = blobName.Replace(@"\", "/");

                        currentFileIdx++;
                        zipProgressAction?.Invoke(new ZipProgress(blobName, currentFileIdx, totalFiles));

                        var entry = archive.CreateEntry(blobName);
                        using (var entryStream = entry.Open())
                        {
                            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                            {
                                byte[] buffer = new byte[0x10000];
                                int n;
                                while ((n = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    entryStream.Write(buffer, 0, n);
                                }
                            }
                        }
                    }
                }
            }

            return totalFiles;
        }
    }
}