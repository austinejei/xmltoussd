using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdScreen
    {
        public UssdScreen()
        {
            UssdItems = new List<UssdItem>();
        }

        public string Id { get; set; }
        public bool Main { get; set; }

        public override string ToString()
        {
            return Id;
        }

        public List<UssdItem> UssdItems { get; set; }

     
    }
}