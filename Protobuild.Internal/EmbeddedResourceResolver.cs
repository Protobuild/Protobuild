//-----------------------------------------------------------------------
// <copyright file="EmbeddedResourceResolver.cs" company="Protobuild Project">
// The MIT License (MIT)
// 
// Copyright (c) Various Authors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//     The above copyright notice and this permission notice shall be included in
//     all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//     THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Protobuild
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// An XML URL resource resolver that loads XML resources from the Protobuild assembly.
    /// </summary>
    public class EmbeddedResourceResolver : XmlUrlResolver
    {
        /// <summary>
        /// Gets the XML entity by it's URI and returns the object.
        /// </summary>
        /// <returns>The XML entity.</returns>
        /// <param name="absoluteUri">The absolute URI.</param>
        /// <param name="role">The role of the XML entity.</param>
        /// <param name="typeOfObjectToReturn">The tyep of object to return.</param>
        public override object GetEntity(
            Uri absoluteUri,
            string role,
            Type typeOfObjectToReturn)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(
                this.GetType(),
                Path.GetFileName(absoluteUri.AbsolutePath));
        }
    }
}
