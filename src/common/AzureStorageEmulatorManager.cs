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

using System.Diagnostics;
using System.Linq;

namespace ZubeNet.Common
{
    public static class AzureStorageEmulatorManager
    {
        private const string WindowsAzureStorageEmulatorPath = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe";

        private const string Win7ProcessName = "WAStorageEmulator";
        private const string Win8ProcessName = "WASTOR~1";

        private static readonly ProcessStartInfo StartStorageEmulatorValue = new ProcessStartInfo
        {
            FileName = WindowsAzureStorageEmulatorPath,
            Arguments = "start",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        private static readonly ProcessStartInfo ClearStorageEmulatorValue = new ProcessStartInfo
        {
            FileName = WindowsAzureStorageEmulatorPath,
            Arguments = "clear all",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        private static readonly ProcessStartInfo StopStorageEmulatorValue = new ProcessStartInfo
        {
            FileName = WindowsAzureStorageEmulatorPath,
            Arguments = "stop",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        private static Process GetProcess()
        {
            return Process.GetProcessesByName(Win7ProcessName).FirstOrDefault() ?? Process.GetProcessesByName(Win8ProcessName).FirstOrDefault();
        }

        public static bool IsProcessStarted()
        {
            return GetProcess() != null;
        }

        public static void StartStorageEmulator()
        {
            if (!IsProcessStarted())
            {
                using (var process = Process.Start(StartStorageEmulatorValue))
                {
                    process.WaitForExit();
                }
            }
        }

        public static void ClearStorageEmulator()
        {
            using (var process = Process.Start(ClearStorageEmulatorValue))
            {
                process.WaitForExit();
            }
        }

        public static void StopStorageEmulator()
        {
            using (var process = Process.Start(StopStorageEmulatorValue))
            {
                process.WaitForExit();
            }
        }
    }
}