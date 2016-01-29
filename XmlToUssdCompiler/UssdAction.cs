using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdAction : UssdItem
    {
        public UssdAction()
        {
            BindTo = new UssdBoundModel();
            Http = new UssdActionHttp();
           
        }

        public string Id { get; set; }
        public UssdBoundModel BindTo { get; set; }
        public UssdActionHttp Http { get; set; }


    }
}