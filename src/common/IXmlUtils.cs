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

using System.Data;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace ZubeNet.Common
{
    public interface IXmlUtils
    {
        bool AttributeExists(XElement node, string attributeName);
        void BeautifyXml(string xmlFilePath);
        string BeautifyXmlString(string xml);
        T ParseAttributeFromNode<T>(XElement node, string attributeName, T defaultValue);
        string TransformDataSet(DataSet ds, string xsltFileName);
        string TransformDataSet(DataSet ds, string xsltFileName, string rootNodeName);
        string TransformDataSet(DataSet ds, string xsltFileName, XsltArgumentList xsltArgs);
        string TransformDataSet(DataSet ds, string xsltFileName, XsltArgumentList xsltArgs, string rootNodeName);
        bool XPathExists(XDocument doc, string elementName);
        bool XPathExists(XDocument doc, string elementName, string attributeName);
        bool XPathExists(XElement node, string elementName);
        bool XPathExists(XElement node, string elementName, string attributeName);
        T XPathParseAttribute<T>(XDocument doc, string elementName, string attributeName, T defaultValue);
        T XPathParseInnerTextFromNode<T>(XElement node, string elementName, T defaultValue);
        string CanonicalizeXml(string xml);
    }
}