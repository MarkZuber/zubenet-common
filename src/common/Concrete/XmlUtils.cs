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
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace ZubeNet.Common.Concrete
{
    /// <summary>
    ///     A collection of XML utilities.
    /// </summary>
    public class XmlUtils : IXmlUtils
    {
        /// <summary>
        ///     Default XML root node name for a DataSet.
        /// </summary>
        private const string DefaultRootNodeName = "root";

        /// <summary>
        ///     Transforms a DataSet via XSLT.
        /// </summary>
        /// <param name="ds">DataSet of results.</param>
        /// <param name="xsltFileName">XSLT file name.</param>
        /// <param name="xsltArgs">XSLT arguments.</param>
        /// <param name="rootNodeName">XML root node (optional, defaults to 'root').</param>
        /// <returns>XSLT output.</returns>
        public string TransformDataSet(DataSet ds, string xsltFileName, XsltArgumentList xsltArgs, string rootNodeName)
        {
            // if we weren't given a root node name, we have to call it something, so default/'root'
            if (string.IsNullOrEmpty(rootNodeName))
            {
                rootNodeName = DefaultRootNodeName;
            }

            // create a return writer buffer for holding HTML
            using (var xsltOutput = new StringWriter(CultureInfo.InvariantCulture))
            {
                ds.DataSetName = rootNodeName;

                using (var xmlSr = new StringReader(ds.GetXml()))
                {
                    XPathDocument xpathDoc = new XPathDocument(xmlSr);

                    // setup our Xsl compiled transform
                    XslCompiledTransform xsl = new XslCompiledTransform(false);

                    // allow document()
                    XsltSettings xsltSettings = new XsltSettings(true, true);
                    xsl.Load(xsltFileName, xsltSettings, new XmlUrlResolver());

                    // setup xslt args if null
                    if (xsltArgs == null)
                    {
                        xsltArgs = new XsltArgumentList();
                    }

                    // do the transform
                    xsl.Transform(xpathDoc, xsltArgs, xsltOutput);

                    // return HTML
                    return xsltOutput.ToString();
                }
            }
        }

        /// <summary>
        ///     Transforms a DataSet via XSLT.
        /// </summary>
        /// <param name="ds">DataSet of results.</param>
        /// <param name="xsltFileName">XSLT file name.</param>
        /// <returns>XSLT output.</returns>
        public string TransformDataSet(DataSet ds, string xsltFileName)
        {
            return TransformDataSet(ds, xsltFileName, null, string.Empty);
        }

        /// <summary>
        ///     Transforms a DataSet via XSLT.
        /// </summary>
        /// <param name="ds">DataSet of results.</param>
        /// <param name="xsltFileName">XSLT file name.</param>
        /// <param name="xsltArgs">XSLT arguments.</param>
        /// <returns>XSLT output.</returns>
        public string TransformDataSet(DataSet ds, string xsltFileName, XsltArgumentList xsltArgs)
        {
            return TransformDataSet(ds, xsltFileName, xsltArgs, string.Empty);
        }

        /// <summary>
        ///     Transforms a DataSet via XSLT.
        /// </summary>
        /// <param name="ds">DataSet of results.</param>
        /// <param name="xsltFileName">XSLT file name.</param>
        /// <param name="rootNodeName">XML root node (optional, defaults to 'root').</param>
        /// <returns>XSLT output.</returns>
        public string TransformDataSet(DataSet ds, string xsltFileName, string rootNodeName)
        {
            return TransformDataSet(ds, xsltFileName, null, rootNodeName);
        }

        /// <summary>
        ///     Determines whether an element exists via XPath.
        /// </summary>
        /// <param name="doc">XML XDocument.</param>
        /// <param name="elementName">Element name.</param>
        /// <returns>True if the element exists.</returns>
        public bool XPathExists(XDocument doc, string elementName)
        {
            return doc != null && XPathExists(doc.Root, elementName);
        }

        /// <summary>
        ///     Determines whether an element exists via XPath relative to node
        /// </summary>
        /// <param name="node">Node to check XPath from</param>
        /// <param name="elementName">Element name.</param>
        /// <returns>True if the element exists.</returns>
        public bool XPathExists(XElement node, string elementName)
        {
            return node?.XPathSelectElement(elementName) != null;
        }

        /// <summary>
        ///     Determines whether an element and its attribute exists via XPath.
        /// </summary>
        /// <param name="doc">XML XDocument.</param>
        /// <param name="elementName">Element name.</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <returns>True if the element and its attribute exist.</returns>
        public bool XPathExists(XDocument doc, string elementName, string attributeName)
        {
            if (doc == null || !XPathExists(doc.Root, elementName))
            {
                return false;
            }

            return doc.Root?.XPathSelectElement(elementName).Attribute(attributeName) != null;
        }

        /// <summary>
        ///     Determines whether an element and its attribute exists via XPath relative to a node.
        /// </summary>
        /// <param name="node">Node to check XPath from</param>
        /// <param name="elementName">Element name.</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <returns>True if the element and its attribute exist.</returns>
        public bool XPathExists(XElement node, string elementName, string attributeName)
        {
            if (!XPathExists(node, elementName))
            {
                return false;
            }

            return node.XPathSelectElement(elementName).Attribute(attributeName) != null;
        }

        /// <summary>
        ///     Determines whether an element and its attribute exists via XPath relative to a node.
        /// </summary>
        /// <param name="node">Node to check XPath from</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <returns>True if the element and its attribute exist.</returns>
        public bool AttributeExists(XElement node, string attributeName)
        {
            return node?.Attribute(attributeName) != null;
        }

        /// <summary>
        ///     Parse an XML attribute from an XDocument via xpath.
        /// </summary>
        /// <typeparam name="T">Type of value to return.</typeparam>
        /// <param name="doc">XML XDocument.</param>
        /// <param name="elementName">Element name.</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>
        ///     Value of attribute converted to requested type.  If the XDocument is invalid, the element does not exist or the
        ///     attribute is missing,
        ///     the default value is returned.
        /// </returns>
        public T XPathParseAttribute<T>(XDocument doc, string elementName, string attributeName, T defaultValue)
        {
            // return default value if XPath doesn't exist
            return XPathExists(doc, elementName, attributeName) ? ParseAttributeFromNode(doc.Root?.XPathSelectElement(elementName), attributeName, defaultValue) : defaultValue;
        }

        /// <summary>
        ///     Parse an Attribute from an XmlNode
        /// </summary>
        /// <typeparam name="T">Type of value to return.</typeparam>
        /// <param name="node">XML Node to parse from</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>
        ///     Value of attribute converted to requested type.  If the XDocument is invalid, the element does not exist or the
        ///     attribute is missing,
        ///     the default value is returned.
        /// </returns>
        public T ParseAttributeFromNode<T>(XElement node, string attributeName, T defaultValue)
        {
            try
            {
                if (!AttributeExists(node, attributeName))
                {
                    return defaultValue;
                }
                // get string representation of value
                string value = node.Attribute(attributeName).Value;
                if (string.IsNullOrEmpty(value))
                {
                    return defaultValue;
                }

                // convert to target type
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), value);
                }
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (XmlException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        ///     Parse an inner text from an XmlNode via xpath relative to node.
        /// </summary>
        /// <typeparam name="T">Type of value to return.</typeparam>
        /// <param name="node">XML Node to parse from</param>
        /// <param name="elementName">Element name.</param>
        /// <param name="defaultValue">Default value.</param>
        /// <returns>
        ///     Value of attribute converted to requested type.  If the XDocument is invalid, the element does not exist or the
        ///     attribute is missing,
        ///     the default value is returned.
        /// </returns>
        public T XPathParseInnerTextFromNode<T>(XElement node, string elementName, T defaultValue)
        {
            try
            {
                if (!XPathExists(node, elementName))
                {
                    return defaultValue;
                }

                // get string representation of value
                string value = node.XPathSelectElement(elementName).Value;
                if (string.IsNullOrEmpty(value))
                {
                    return defaultValue;
                }

                // convert to target type
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), value);
                }
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (XmlException)
            {
                return defaultValue;
            }
        }

        public void BeautifyXml(string xmlFilePath)
        {
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);

            using (var xtw = new XmlTextWriter(xmlFilePath, Encoding.UTF8))
            {
                xtw.Formatting = Formatting.Indented;
                doc.Save(xtw);
            }
        }

        public string BeautifyXmlString(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            using (var buffer = new StringWriter())
            {
                using (var xtw = new XmlTextWriter(buffer))
                {
                    xtw.Formatting = Formatting.Indented;
                    doc.WriteTo(xtw);
                    xtw.Flush();
                }

                buffer.Flush();
                return buffer.ToString();
            }
        }

        public string CanonicalizeXml(string xml)
        {
            var document = new XmlDocument
            {
                PreserveWhitespace = false
            };

            document.LoadXml(xml);
            var nodes = document.SelectNodes("/descendant-or-self::node() | //@* | //namespace::*");
            Debug.Assert(nodes != null, "nodes != null");

            //
            // Canonicalize the document.
            // <node /> expands to <node></node>, attributes are placed into
            // alphabetical order, etc.
            //
            var transform = new XmlDsigExcC14NTransform();
            transform.LoadInput(nodes);
            using (var stream = (MemoryStream)transform.GetOutput(typeof(Stream)))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}