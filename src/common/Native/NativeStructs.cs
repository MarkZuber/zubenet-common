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
using System.Runtime.InteropServices;
using System.Threading;

namespace ZubeNet.Common.Native
{
    internal static class NativeStructs
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        // ReSharper disable once InconsistentNaming
        internal struct FILE_IN_CABINET_INFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string NameInCabinet;

            public uint FileSize;
            public uint Win32Error;
            public ushort DosDate;
            public ushort DosTime;
            public ushort DosAttribs;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeConstants.MAX_PATH)]
            public string FullTargetName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        // ReSharper disable once InconsistentNaming
        internal struct FILEPATHS
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Target;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string Source;

            public uint Win32Error;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        // ReSharper disable once InconsistentNaming
        internal struct NETRESOURCE
        {
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
#pragma warning disable SA1305 // Field names must not use Hungarian notation

            public int dwScope;
            public ResourceType dwType;
            public int dwDisplayType;
            public int dwUsage;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpLocalName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpRemoteName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpComment;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpProvider;

#pragma warning restore SA1305 // Field names must not use Hungarian notation
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct UNICODE_STRING : IDisposable
        {
            public ushort Length;
            public ushort MaximumLength;
            private IntPtr _buffer;

            public UNICODE_STRING(string s)
            {
                Length = (ushort)(s.Length * 2);
                MaximumLength = (ushort)(Length + 2);
                _buffer = Marshal.StringToHGlobalUni(s);
            }

            public UNICODE_STRING(int length)
            {
                Length = (ushort)(length * 2);
                MaximumLength = (ushort)(length + 2);
                _buffer = Marshal.AllocHGlobal(Length + 2);
            }

            public void Dispose()
            {
                IntPtr buffer = Interlocked.Exchange(ref _buffer, IntPtr.Zero);

                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            public override string ToString()
            {
                return Marshal.PtrToStringUni(_buffer);
            }
        }

        internal enum ResourceType
        {
            // ReSharper disable once InconsistentNaming
            RESOURCETYPE_DISK = 1,
        }
    }
}