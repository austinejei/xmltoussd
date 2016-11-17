using System;
using System.Configuration;

namespace XmlToUssdCompiler
{
    public static class StringHelper
    {
        public static string ToUssdString(this string innerText, bool simulator=true)
        {
            //if doing simulation, it should use the second return, else fine.


                innerText = innerText
                    .Replace("{Request.Mobile}", ConfigurationManager.AppSettings["Ussd.Context.Mobile"])
                    .Replace("{Request.SessionId}", Guid.NewGuid().ToString("N"))
                    .Replace("{Request.Type}", "Initiation")
                    .Replace("{Request.Message}", "*1234#")
                    .Replace("{Request.ServiceCode}","*1234#")
                    ;
                return innerText.Replace(Environment.NewLine, string.Empty)
                    .Replace(@"\t", "\t")
                    //.Replace(@"\r\n", " ")
                    ;
        
        }
    }
}