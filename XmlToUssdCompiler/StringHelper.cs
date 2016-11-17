using System;
using System.Configuration;

namespace XmlToUssdCompiler
{
    public static class StringHelper
    {
        public static string ToUssdString(this string innerText, bool simulator=true)
        {
            //if doing simulation, it should use the second return, else fine.


            if (simulator)
            {
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

          
            innerText = innerText.Replace(Environment.NewLine, string.Empty).Replace(@"\t", "\t");
            //since we're parsing each expression, we shall have Request object in the expression

            //innerText = innerText.Replace("{Request.Mobile}", "\" + Request.Mobile + \"");
            //innerText = innerText.Replace("{Request.SessionId}", "\" + Request.SessionId + \"");
            //innerText = innerText.Replace("{Request.Operator}", "\" + Request.Operator + \"");
            //innerText = innerText.Replace("{Request.ServiceCode}", "\" + Request.ServiceCode + \"");
            //innerText = innerText.Replace("{Request.Message}", "\" + Request.Message + \"");
            //innerText = innerText.Replace("{Request.Type}", "\" + Request.Type + \"");
            return innerText.Trim();
        }
    }
}