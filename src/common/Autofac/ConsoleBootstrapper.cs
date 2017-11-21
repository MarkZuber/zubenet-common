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
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using ZubeNet.Common.CommandLine;
using ZubeNet.Common.Logging;
using ZubeNet.Common.Modules;

namespace ZubeNet.Common.Autofac
{
    public class ConsoleBootstrapper
    {
        public async Task<int> RunAsync(IEnumerable<Module> modules, string[] args)
        {
            var serviceCollection = new ServiceCollection();

            // todo: get connectivity for AddLogging reference
            //serviceCollection.AddLogging(
            //    builder =>
            //    {
            //        builder.AddConsole(options => options.IncludeScopes = false);
            //        // builder.AddDebug();
            //    });

            new LogConfigBuilder().AddColoredConsoleAppender().Build();

            var containerBuilder = new ContainerBuilder();
            containerBuilder.Populate(serviceCollection);

            containerBuilder.RegisterModule(new ZubeNetCommonModule());

            foreach (var module in modules)
            {
                containerBuilder.RegisterModule(module);
            }
            containerBuilder.RegisterType<Runner>().AsSelf();

            containerBuilder.Populate(serviceCollection);
            var container = containerBuilder.Build();
            var serviceProvider = new AutofacServiceProvider(container);

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddNLog();
            loggerFactory.ConfigureNLog(LogManager.Configuration);

            var runner = serviceProvider.GetService<Runner>();
            try
            {
                int val = await runner.RunAsync(args);
                ConsoleReadLineIfDebuggerAttached();
                return val;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failure: {ex}");
                ConsoleReadLineIfDebuggerAttached();
                return CommandLineExecutor.UnexpectedFailureExitCode;
            }
        }

        private void ConsoleReadLineIfDebuggerAttached()
        {
            if (Debugger.IsAttached)
            {
                Console.Write("Press <ENTER> to continue");
                Console.ReadLine();
            }
        }

        private sealed class Runner
        {
            private readonly ICommandLineExecutor _executor;

            public Runner(ICommandLineExecutor executor)
            {
                _executor = executor;
            }

            public Task<int> RunAsync(string[] args)
            {
                return _executor.RunAsync(args);
            }
        }
    }
}