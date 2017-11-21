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

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZubeNet.Common.Concrete
{
    /// <summary>
    ///     Extracts zip files to a single flat folder. Supports auto renaming files when duplicate names exist
    /// </summary>
    public static class ZipExtractor
    {
        public static void ZipExtractToFolder(string inputZipFileName, string outputDirectoryName, bool autoRenameFiles = true)
        {
            var duplicateFileCount = new Dictionary<string, int>();

            using (var zipToOpen = new FileStream(inputZipFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    Directory.CreateDirectory(outputDirectoryName);

                    foreach (var entry in archive.Entries)
                    {
                        // Folders will have empty names
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            if (autoRenameFiles)
                            {
                                var targetPath = Path.Combine(outputDirectoryName, entry.Name);

                                // Auto rename behavior adding _1, _2 to duplicate files
                                if (File.Exists(targetPath))
                                {
                                    if (duplicateFileCount.ContainsKey(entry.Name))
                                    {
                                        duplicateFileCount[entry.Name] = duplicateFileCount[entry.Name] + 1;
                                    }
                                    else
                                    {
                                        duplicateFileCount.Add(entry.Name, 1);
                                    }
                                    var newTargetPath = new StringBuilder();
                                    newTargetPath.Append(Path.Combine(outputDirectoryName, Path.GetFileNameWithoutExtension(targetPath)));
                                    newTargetPath.Append("_");
                                    newTargetPath.Append(duplicateFileCount[entry.Name]);
                                    newTargetPath.Append(Path.GetExtension(targetPath));
                                    targetPath = newTargetPath.ToString();
                                }

                                entry.ExtractToFile(targetPath, false);
                            }
                            else
                            {
                                entry.ExtractToFile(Path.Combine(outputDirectoryName, entry.Name), true);
                            }
                        }
                    }
                }
            }
        }
    }
}