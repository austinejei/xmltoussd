using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdActionHttp
    {
        public UssdActionHttp()
        {
            Method = new UssdActionHttpMethod();
            Headers = new List<UssdActionHttpHeader>();
            Responses = new List<UssdActionHttpResponse>();
            Body = new UssdActionHttpBody();
            Cycle = UssdActionCycle.Once;
        }
        public string Url { get; set; }
        public UssdActionHttpMethod Method { get; set; }

        public List<UssdActionHttpHeader> Headers { get; set; }
        public UssdActionCycle Cycle { get; set; }
        public List<UssdActionHttpResponse> Responses { get; set; }
        public UssdActionHttpBody Body { get; set; }
    }
}