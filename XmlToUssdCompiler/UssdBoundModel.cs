using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdBoundModel
    {
        public UssdBoundModel()
        {
            Value = new Dictionary<string, string>();
        }
        public string Name { get; set; }
        public Dictionary<string,string> Value { get; set; }
    }
}