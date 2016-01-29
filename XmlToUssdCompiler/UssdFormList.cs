using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdFormList
    {
        public UssdFormList()
        {
            Choices = new List<UssdFormListChoice>();
        }
        public string Id { get; set; }
        public List<UssdFormListChoice> Choices { get; set; }
    }
}