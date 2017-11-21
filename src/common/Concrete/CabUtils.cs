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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using ZubeNet.Common.Native;

namespace ZubeNet.Common.Concrete
{
    public sealed class CabUtils : ICabUtils
    {
        public void ExtractAll(string cabPath, string targetDirectory)
        {
            new CabUtilsHandler(cabPath, targetDirectory).ExtractAll();
        }

        private class CabUtilsHandler
        {
            private readonly string _cabPath;
            private readonly Dictionary<string, int> _duplicateFileCount = new Dictionary<string, int>();
            private readonly string _targetDirectory;
            private Exception _callbackException;

            public CabUtilsHandler(string cabPath, string targetDirectory)
            {
                if (!File.Exists(cabPath))
                {
                    throw new FileNotFoundException("CAB not found", cabPath);
                }

                _cabPath = cabPath;
                _targetDirectory = targetDirectory;
            }

            public void ExtractAll()
            {
                Directory.CreateDirectory(_targetDirectory);
                NativeDelegates.PSP_FILE_CALLBACK setupApiCallback = SetupApiCallback;
                bool result = NativeMethods.SetupIterateCabinetW(_cabPath, 0, setupApiCallback, IntPtr.Zero);

                if (_callbackException != null)
                {
                    throw new Win32Exception("Extraction Failed", _callbackException);
                }

                if (!result)
                {
                    int error = Marshal.GetHRForLastWin32Error();
                    throw Marshal.GetExceptionForHR(error);
                }

                GC.KeepAlive(setupApiCallback);
            }

            private uint SetupApiCallback(IntPtr context, uint notification, IntPtr param1, IntPtr param2)
            {
                try
                {
                    Debug.Assert(_callbackException == null, "callbackException must be null here");

                    uint retValue = NativeConstants.NO_ERROR;
                    switch (notification)
                    {
                    case NativeConstants.SPFILENOTIFY_FILEINCABINET:
                        retValue = OnFileFound(param1);
                        break;
                    case NativeConstants.SPFILENOTIFY_FILEEXTRACTED:
                        retValue = OnFileExtractComplete(param1);
                        break;
                    case NativeConstants.SPFILENOTIFY_NEEDNEWCABINET:
                        retValue = NativeConstants.NO_ERROR;
                        break;
                    }

                    return retValue;
                }
                catch (Exception ex)
                {
                    _callbackException = ex;
                    return NativeConstants.FILEOP_ABORT;
                }
            }

            private uint OnFileFound(IntPtr param1)
            {
                var fileInCabinetInfo = (NativeStructs.FILE_IN_CABINET_INFO)Marshal.PtrToStructure(param1, typeof(NativeStructs.FILE_IN_CABINET_INFO));
                string fileNameInCab = Path.GetFileName(fileInCabinetInfo.NameInCabinet);
                if (fileNameInCab != null)
                {
                    string targetPath = Path.Combine(_targetDirectory, fileNameInCab);

                    // Auto rename behavior adding _1, _2 to duplicate files
                    if (File.Exists(targetPath))
                    {
                        if (_duplicateFileCount.ContainsKey(fileNameInCab))
                        {
                            _duplicateFileCount[fileNameInCab] = _duplicateFileCount[fileNameInCab] + 1;
                        }
                        else
                        {
                            _duplicateFileCount.Add(fileNameInCab, 1);
                        }
                        targetPath = Path.Combine(
                            _targetDirectory,
                            Path.GetFileNameWithoutExtension(targetPath) + "_" + _duplicateFileCount[fileNameInCab] + Path.GetExtension(targetPath));
                    }

                    fileInCabinetInfo.FullTargetName = targetPath;
                }

                Marshal.StructureToPtr(fileInCabinetInfo, param1, true);
                return NativeConstants.FILEOP_DOIT;
            }

            private uint OnFileExtractComplete(IntPtr param1)
            {
                var filePaths = (NativeStructs.FILEPATHS)Marshal.PtrToStructure(param1, typeof(NativeStructs.FILEPATHS));
                return filePaths.Win32Error;
            }
        }
    }
}