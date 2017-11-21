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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ZubeNet.Common.Extensions;

namespace ZubeNet.Common.Rest
{
    /// <summary>
    ///     This is needed so that the Impl can have DI capabilities since the filter is registered at the same time as we're
    ///     setting up the DI container.
    /// </summary>
    public sealed class RequestDurationFilter : TypeFilterAttribute
    {
        public RequestDurationFilter()
            : base(typeof(RequestDurationFilterImpl))
        {
        }
    }

    /// <summary>
    ///     Filter that records how long requests take.
    /// </summary>
    public sealed class RequestDurationFilterImpl : ActionFilterAttribute
    {
        // private readonly IMetric _durationMetric;

        //public RequestDurationFilterImpl(IApiServiceMetrics metrics)
        //{
        //    _durationMetric = metrics.RequestDurationInMs;
        //}
        /// <summary>
        ///     Initializes a new instance of the <see cref="RequestDurationFilter" /> class.
        /// </summary>
        /// <inheritdoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Items["stopWatch"] = Stopwatch.StartNew();
        }

        /// <inheritdoc />
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var watch = (Stopwatch)context.HttpContext.Items["stopWatch"];
            watch.Stop();

            var apiVersion = context.HttpContext.GetApiVersion();
            var userId = context.HttpContext.User.FindFirst("appid")?.Value ?? "unknown";
            //_durationMetric.LogValue(
            //    watch.ElapsedMilliseconds,
            //    apiVersion,
            //    context.Controller.GetType().Name.Split('.').Last(),
            //    context.ActionDescriptor.DisplayName,
            //    userId);
        }
    }
}