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

using NLog;
using NLog.Conditions;
using NLog.Config;
using NLog.Targets;

namespace ZubeNet.Common.Logging
{
    public class LogConfigBuilder
    {
        public const string DefaultConsoleLayout = @"${date:format=HH\:mm\:ss}|${level:uppercase=true}| ${logger} ${message}";
        private readonly LoggingConfiguration _loggingConfiguration = new LoggingConfiguration();

        public LogConfigBuilder AddColoredConsoleAppender(LogLevel logLevel = null, string layout = DefaultConsoleLayout)
        {
            var consoleTarget = new ColoredConsoleTarget
            {
                Layout = layout
            };

            var highlightRule = new ConsoleRowHighlightingRule
            {
                Condition = ConditionParser.ParseExpression("level == LogLevel.Debug"),
                ForegroundColor = ConsoleOutputColor.DarkGray
            };
            consoleTarget.RowHighlightingRules.Add(highlightRule);

            _loggingConfiguration.AddTarget("console", consoleTarget);
            _loggingConfiguration.LoggingRules.Add(new LoggingRule("*", logLevel ?? LogLevel.Debug, consoleTarget));

            return this;
        }

        public LogConfigBuilder AddDebugAppender(LogLevel logLevel = null, string layout = DefaultConsoleLayout)
        {
            var debugTarget = new TraceTarget
            {
                Layout = layout
            };

            _loggingConfiguration.AddTarget("debug", debugTarget);
            _loggingConfiguration.LoggingRules.Add(new LoggingRule("*", logLevel ?? LogLevel.Debug, debugTarget));

            return this;
        }

        public LogConfigBuilder AddFileAppender(string fileName = "nlog.txt", LogLevel logLevel = null)
        {
            var fileTarget = new FileTarget
            {
                FileName = fileName,
                MaxArchiveFiles = 5
            };
            _loggingConfiguration.AddTarget("file", fileTarget);
            _loggingConfiguration.LoggingRules.Add(new LoggingRule("*", logLevel ?? LogLevel.Debug, fileTarget));

            return this;
        }

        public void Build()
        {
            // setting the configuration activates it.
            LogManager.Configuration = _loggingConfiguration;
        }
    }
}