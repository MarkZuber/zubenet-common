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
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace ZubeNet.Common.CommandLine
{
    public sealed class CommandLineExecutor : ICommandLineExecutor
    {
        public const int SuccessExitCode = 0;
        public const int UsageFailureExitCode = 1;
        public const int UnexpectedFailureExitCode = 2;

        private readonly IEnumerable<IProgramCommand> _programCommands;

        public CommandLineExecutor(IEnumerable<IProgramCommand> programCommands)
        {
            _programCommands = programCommands;
        }

        public Task<int> RunAsync(string[] commandLineArgs)
        {
            var result = Parser.Default.ParseArguments(commandLineArgs, _programCommands.Select(x => x.OptionsType).ToArray());
            return result.MapResult<object, Task<int>>(RunProgramCommandAsync, errs => Task.FromResult(1));
        }

        private async Task<int> RunProgramCommandAsync(object options)
        {
            var programCommand = _programCommands.FirstOrDefault(x => x.OptionsType == options.GetType());

            try
            {
                return await programCommand.RunAsync(options);
            }
            catch (AggregateException e)
            {
                return HandleAggregateException(e);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"FAILURE: {e}");
                return UnexpectedFailureExitCode;
            }
        }

        private static int HandleAggregateException(AggregateException e)
        {
            Exception exceptionToHandle = e;
            if (e.InnerExceptions.Count == 1)
            {
                exceptionToHandle = e.InnerException;
            }

            if (exceptionToHandle is CommandLineExecutorSilentException)
            {
                var casted = exceptionToHandle as CommandLineExecutorSilentException;
                if (casted.ExitCode != 0)
                {
                    Console.Error.WriteLine($"A non-zero return code has been encountered: {casted.ExitCode}");
                }

                return casted.ExitCode;
            }

            if (exceptionToHandle is UsageException)
            {
                Console.Error.WriteLine($"Error: {e}");
                // todo: Console.Error.WriteLine(HelpText.AutoBuild(programCommand));
                return UsageFailureExitCode;
            }

            if (exceptionToHandle is ProcessRunException)
            {
                var casted = exceptionToHandle as ProcessRunException;
                Console.Error.WriteLine();
                Console.Error.WriteLine($"FAILURE: A Process ({casted.FileName}) has failed with exit code: {casted.ProcessExitCode}");
                Console.Error.WriteLine($"Arguments: {casted.Arguments}");
                Console.Error.WriteLine($"StdOut: {casted.ProcessStandardOutput}");
                Console.Error.WriteLine($"StdErr: {casted.ProcessStandardError}");
                Console.Error.WriteLine();
                return UnexpectedFailureExitCode;
            }

            if (exceptionToHandle is AggregateException)
            {
                var casted = exceptionToHandle as AggregateException;
                Console.Error.WriteLine("Aggregate Exception caught:");
                foreach (var ex in casted.InnerExceptions)
                {
                    Console.Error.WriteLine($"* Error: {ex.Message}");
                    Console.Error.WriteLine($"  Stack: {ex.StackTrace}");
                    Console.Error.WriteLine("---------");
                }

                return UnexpectedFailureExitCode;
            }

            Console.Error.WriteLine($"FAILURE: {exceptionToHandle}");
            return UnexpectedFailureExitCode;
        }
    }
}