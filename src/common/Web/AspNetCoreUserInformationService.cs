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
using System.Security.Claims;
using Autofac;
using Microsoft.AspNetCore.Http;

namespace ZubeNet.Common.Web
{
    /// <summary>
    ///     AspNetCore UserInformationService.
    ///     Usage of this class requires IHttpContextAccessor to be bound.
    ///     Use NinjectSetupHelper to initialize the bindings.
    /// </summary>
    public class AspNetCoreUserInformationService : IUserInformationService
    {
        private readonly Lazy<IHttpContextAccessor> _accessor;

        public AspNetCoreUserInformationService(IComponentContext componentContext)
        {
            // Note that IHttpContextAccessor cannot be constructed inline because it is bound late.
            // References:
            // 1. https://github.com/aspnet/Security/issues/322
            _accessor = new Lazy<IHttpContextAccessor>(componentContext.Resolve<IHttpContextAccessor>);
        }

        public ClaimsPrincipal CurrentPrincipal
        {
            get
            {
                var httpContext = _accessor.Value.HttpContext;
                if (httpContext == null)
                {
                    throw new InvalidOperationException("HttpContext is null");
                }
                return httpContext.User;
            }
        }

        /// <inheritdoc />
        public void Start()
        {
        }
    }
}