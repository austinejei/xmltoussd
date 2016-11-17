using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using XmlToUssdCompiler;

namespace XmlToUssdTests
{
    [TestClass]
    public class LussdCompilerShouldRejectFileThat 
    {

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException),
    "File not found.")]
        public void IsNotFound()
        {
            var ussd = new UssdXml("not-found.xml");


        }

        [TestMethod]
        [ExpectedException(typeof(Exception),
    "file is empty")]
        public void IsEmpty()
        {
            var ussd = new UssdXml(@"SourceFiles\empty-file.xml");


        }

        [TestMethod]
        [ExpectedException(typeof(Exception),
    "invalid exception")]
        public void HasInvalidExtension()
        {
            var ussd = new UssdXml("invalid-extension.notxml");

        }

     
    }
}
