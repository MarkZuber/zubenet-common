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

using System.Diagnostics.Contracts;
using System.Security.Principal;

namespace ZubeNet.Common
{
    /// <summary>
    ///     Contains methods for interacting with users.
    /// </summary>
    public static class UserUtils
    {
        /// <summary>
        ///     Checks that the code is executing in an elevated context.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the caller is running elevated; <c>false</c> otherwise.
        /// </returns>
        public static bool IsCallerElevated()
        {
            using (var user = WindowsIdentity.GetCurrent())
            {
                Contract.Assert(user != null);
                return user.IsElevated();
            }
        }

        /// <summary>
        ///     Checks that the given user is elevated.
        /// </summary>
        /// <param name="identity">
        ///     The identity to check.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the <paramref name="identity" /> is running elevated; <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="identity" /> is <c>null</c>.
        /// </exception>
        public static bool IsElevated(this WindowsIdentity identity)
        {
            ValidationUtils.CheckArgumentNotNull(identity, nameof(identity));

            var principal = new WindowsPrincipal(identity);
            return principal.IsElevated();
        }

        /// <summary>
        ///     Checks that the given user is elevated.
        /// </summary>
        /// <param name="principal">
        ///     The principal to check.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the <paramref name="principal" /> is running elevated; <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        ///     <paramref name="principal" /> is <c>null</c>.
        /// </exception>
        public static bool IsElevated(this WindowsPrincipal principal)
        {
            ValidationUtils.CheckArgumentNotNull(principal, nameof(principal));

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}