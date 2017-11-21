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
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ZubeNet.Common
{
    public static class AzureStorageUtils
    {
        public static T GetById<T>(CloudTable cloudTable, string partitionKey, string rowKey)
            where T : TableEntity
        {
            var op = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var result = cloudTable.ExecuteAsync(op).ConfigureAwait(false).GetAwaiter().GetResult();
            return (T)result.Result;
        }

        public static async Task<T> GetByIdAsync<T>(CloudTable cloudTable, string partitionKey, string rowKey)
            where T : TableEntity
        {
            var op = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var tableResult = await cloudTable.ExecuteAsync(op);
            return (T)tableResult.Result;
        }

        /// <summary>
        ///     Might throw StorageException with PreconditionFailed.
        ///     Consider using with RetryOnPreconditionFailed().
        /// </summary>
        public static void DeleteById<T>(CloudTable cloudTable, string partitionKey, string rowKey)
            where T : TableEntity
        {
            var entity = GetById<T>(cloudTable, partitionKey, rowKey);
            if (entity != null)
            {
                var op = TableOperation.Delete(entity);
                cloudTable.ExecuteAsync(op).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        ///     Might throw StorageException with PreconditionFailed.
        ///     Consider using with RetryOnPreconditionFailed().
        /// </summary>
        public static async Task DeleteByIdAsync<T>(CloudTable cloudTable, string partitionKey, string rowKey)
            where T : TableEntity
        {
            var entity = await GetByIdAsync<T>(cloudTable, partitionKey, rowKey);
            if (entity != null)
            {
                var op = TableOperation.Delete(entity);
                await cloudTable.ExecuteAsync(op);
            }
        }

        public static CloudTable GetCloudTableReference(string storageConnectionString, string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var cloudTable = tableClient.GetTableReference(tableName);
            cloudTable.CreateIfNotExistsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return cloudTable;
        }

        public static async Task RetryOnPreconditionFailedAsync(Func<Task> asyncFunc, int maxTries = 5)
        {
            await RetryOnPreconditionFailedAsync(
                async () =>
                {
                    await asyncFunc();
                    return true;
                },
                maxTries);
        }

        /// <summary>
        ///     Precondition failure can happen when the underlying data changes.
        /// </summary>
        /// <remarks>
        ///     See https://azure.microsoft.com/en-us/blog/managing-concurrency-in-microsoft-azure-storage-2/
        /// </remarks>
        public static async Task<T> RetryOnPreconditionFailedAsync<T>(Func<Task<T>> asyncFunc, int maxTries = 5)
        {
            var tryCount = 0;
            var aggregatedExceptions = new List<StorageException>();

            do
            {
                tryCount++;
                try
                {
                    return await asyncFunc();
                }
                catch (StorageException storeEx)
                {
                    if (!storeEx.IsHttpStatusCode(HttpStatusCode.PreconditionFailed))
                    {
                        throw;
                    }
                    aggregatedExceptions.Add(storeEx);
                }
                if (tryCount >= maxTries)
                {
                    throw new AggregateException(aggregatedExceptions);
                }
                await Task.Delay(100);
            }
            while (true);
        }
    }
}