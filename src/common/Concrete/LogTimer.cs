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
using System.Diagnostics;
using NLog;

namespace ZubeNet.Common.Concrete
{
    /// <summary>
    ///     A disposable timer for auto-timing function blocks.
    ///     Meant to be used within a using statement such that when the scope exits, it will log the time.
    ///     Since it's within a using statement, and thus you can't conditionally invoke it syntactically, if you want to be
    ///     able to control logging with a setting, you still need to invoke this object but you can send in the
    ///     "skipLogging" flag to control whether the class actually performs any work.
    ///     Example:
    ///     using (var logTimer = _logTimerFactory.CreateLogTimer("My Timing Description"))
    ///     {
    ///     CallSomeFunctionToBeTimed();
    ///     }
    /// </summary>
    public sealed class LogTimer : ILogTimer
    {
        private readonly Logger _log;
        private readonly Stopwatch _stopwatch;

        public LogTimer(Logger logger, string format, params object[] args)
            : this(logger, false, format, args)
        {
        }

        public LogTimer(Logger logger, bool skipLogging, string format, params object[] args)
        {
            _log = logger;
            Description = string.Format(format, args);
            SkipLogging = skipLogging;

            if (!SkipLogging)
            {
                _stopwatch = Stopwatch.StartNew();
                _log.Debug("LogTimer: {0} has started", Description);
            }
        }

        public string Description { get; }
        public bool SkipLogging { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LogTimer()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!SkipLogging)
                {
                    _stopwatch.Stop();
                    _log.Debug("LogTimer: {0}: {1}", (object)Description, (object)_stopwatch.ElapsedMilliseconds);
                }
            }
        }
    }
}