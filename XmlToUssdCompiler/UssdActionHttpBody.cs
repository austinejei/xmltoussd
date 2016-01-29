using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdActionHttpBody
    {
        public UssdActionHttpBody()
        {
            Params = new List<UssdActionHttpBodyParam>();
            BindFrom = new UssdBoundModel();
        }
        public List<UssdActionHttpBodyParam> Params { get; set; }
        public UssdBoundModel BindFrom { get; set; }
    }
}