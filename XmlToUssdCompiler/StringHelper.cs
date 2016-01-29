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
                innerText = innerText.Replace("{Ussd.Context.Mobile}", ConfigurationManager.AppSettings["Ussd.Context.Mobile"]);
                return innerText.Replace(@"\r\n", Environment.NewLine).Replace(@"\t", "\t");
            }

            innerText = innerText.Replace("{Ussd.Context.Mobile}", "\" + Request.Mobile + \"");
            innerText = innerText.Replace("{Ussd.Context.SessionId}", "\" + Request.SessionId + \"");
            innerText = innerText.Replace("{Ussd.Context.Operator}", "\" + Request.Operator + \"");
            innerText = innerText.Replace("{Ussd.Context.ServiceCode}", "\" + Request.ServiceCode + \"");
            innerText = innerText.Replace("{Ussd.Context.Message}", "\" + Request.Message + \"");
            innerText = innerText.Replace("{Ussd.Context.Type}", "\" + Request.Type + \"");
            return innerText.Trim();
        }
    }
}