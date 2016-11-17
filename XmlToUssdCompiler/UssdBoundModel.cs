using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdBoundModel
    {
        public UssdBoundModel()
        {
            Value = new Dictionary<string, dynamic>();
        }
        public string Name { get; set; }
        public Dictionary<string, dynamic> Value { get; set; }
    }
}