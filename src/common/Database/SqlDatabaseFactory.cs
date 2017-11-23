﻿// MIT License
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

using Autofac;

namespace ZubeNet.Common.Database
{
    public class SqlDatabaseFactory : ISqlDatabaseFactory
    {
        private readonly IComponentContext _componentContext;

        public SqlDatabaseFactory(IComponentContext componentContext)
        {
            _componentContext = componentContext;
        }

        public ISqlDatabase CreateSqlDatabase(string connectionString)
        {
            return _componentContext.Resolve<ISqlDatabase>(new NamedParameter(nameof(connectionString), connectionString));
        }

        public ISqlDatabase CreateSqlDatabase(string connectionString, int sqlTimeout)
        {
            return _componentContext.Resolve<ISqlDatabase>(new NamedParameter(nameof(connectionString), connectionString), new NamedParameter(nameof(sqlTimeout), sqlTimeout));
        }
    }
}