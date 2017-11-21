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

using System.Security.Cryptography.X509Certificates;

namespace ZubeNet.Common
{
    /// <summary>
    ///     Utility methods for interacting with certificates.
    /// </summary>
    public interface ICertUtils
    {
        /// <summary>
        ///     Gets a certificate from the given store by its thumbprint.
        /// </summary>
        /// <param name="storeName">The store name.</param>
        /// <param name="storeLocation">The store location.</param>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <param name="validOnly">A value indicating whether only valid certificates are to be returned.</param>
        /// <returns>The certificate, if it exists.</returns>
        /// <exception cref="System.IO.FileNotFoundException">
        ///     The certificate does not exist.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     More than one certificate matched the criteria.
        /// </exception>
        X509Certificate2 GetByThumbprint(StoreName storeName, StoreLocation storeLocation, string thumbprint, bool validOnly);

        /// <summary>
        ///     Gets a certificate from the given store by the given criteria.
        /// </summary>
        /// <param name="storeName">The store name.</param>
        /// <param name="storeLocation">The store location.</param>
        /// <param name="findType">The method to use to find the certificate.</param>
        /// <param name="findValue">The search criteria value.</param>
        /// <param name="validOnly">A value indicating whether only valid certificates are to be returned.</param>
        /// <returns>The certificate, if it exists.</returns>
        /// <exception cref="System.IO.FileNotFoundException">
        ///     The certificate does not exist.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        ///     More than one certificate matched the criteria.
        /// </exception>
        X509Certificate2 GetCertificate(StoreName storeName, StoreLocation storeLocation, X509FindType findType, object findValue, bool validOnly);
    }
}