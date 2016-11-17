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
        public string Repeater { get; set; }

        public bool HasRepeater => !string.IsNullOrEmpty(Repeater);

        public string RepeaterScriptId
        {
            get
            {
                if (HasRepeater)
                {
                    return Repeater.Replace("ussd:script:", string.Empty);
                }

                return string.Empty;
            }
        }

        public List<UssdFormListChoice> Choices { get; set; }
    }
}