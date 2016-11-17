using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdScript
    {
        public string Id { get; set; }
        public string BindFrom { get; set; }
        public string BindTo { get; set; }
        
        public string Content { get; set; }
        

        public override string ToString()
        {
            return Id;
        }

        


    }
}