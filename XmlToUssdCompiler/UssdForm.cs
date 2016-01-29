using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlToUssdCompiler
{
    public class UssdForm : UssdItem
    {
        public UssdForm()
        {
            BindTo = new UssdBoundModel();
            Inputs = new List<UssdFormInput>();
            OnSubmit = new UssdNavigator("ussd:nav:ussd");
        }
        public UssdBoundModel BindTo { get; set; }
        public string Title { get; set; }
        public List<UssdFormInput> Inputs { get; set; }
        public UssdNavigator OnSubmit { get; set; }

        
    }
}