using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlToUssdCompiler
{
    public class UssdText : UssdItem
    {
        public UssdText()
        {
            BindFrom = new UssdBoundModel();
            BindAction = new UssdBoundAction();
        }
        public string Text { get; set; }
        public UssdBoundModel BindFrom { get; set; }
        public UssdBoundAction BindAction { get; set; }

        public UssdText(string text)
        {
            Text = text;
            BindFrom = new UssdBoundModel();
            BindAction = new UssdBoundAction();
        }

    }
}