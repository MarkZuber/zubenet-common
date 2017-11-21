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

namespace ZubeNet.Common.Native
{
    internal static class NativeConstants
    {
        internal const int MAX_PATH = 260;
        internal const uint FILEOP_ABORT = 0; // Abort cabinet processing.
        internal const uint FILEOP_DOIT = 1; // Extract the current file.
        internal const uint FILEOP_SKIP = 2; // Skip the current file.
        internal const uint SPFILENOTIFY_FILEINCABINET = 0x00000011; // The file has been extracted from the cabinet.
        internal const uint SPFILENOTIFY_NEEDNEWCABINET = 0x00000012; // File is encountered in the cabinet.

        internal const uint SPFILENOTIFY_FILEEXTRACTED = 0x00000013;
        // The current file is continued in the next cabinet.

        internal const uint NO_ERROR = 0;
    }
}