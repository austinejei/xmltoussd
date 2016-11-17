using System.Collections.Generic;
using System.Text;

namespace XmlToUssdCompiler
{
    public class UssdMenu : UssdItem
    {
        public UssdMenu()
        {
            BindFrom = new UssdBoundModel();
            Options = new List<UssdMenuOption>();
            BindAction = new UssdBoundAction("");
        }
        public UssdBoundModel BindFrom { get; set; }
        public string Title { get; set; }
        public List<UssdMenuOption> Options { get; set; }
        public UssdBoundAction BindAction { get; set; }

       
    }
}