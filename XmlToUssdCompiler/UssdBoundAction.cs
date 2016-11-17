using System.Collections.Generic;

namespace XmlToUssdCompiler
{
    public class UssdBoundAction
    {
        public UssdBoundAction(string actionText)
        {
            Value = new Dictionary<string, dynamic>();

            if (!string.IsNullOrEmpty(actionText))
            {
                if (actionText.StartsWith("ussd:action"))
                {
                    NavType = UssdNavigatorTypes.ToAction;
                    Name = actionText.Replace("ussd:action:", string.Empty);
                }
                if (actionText.StartsWith("ussd:script"))
                {
                    NavType = UssdNavigatorTypes.ToScript;
                    Name = actionText.Replace("ussd:script:", string.Empty);
                }
                else
                {
                    NavType = UssdNavigatorTypes.ToScreen;
                    Name = actionText.Replace("ussd:nav:", string.Empty);
                    //UssdScreen = new UssdScreen
                    //{
                    //    Id = Name
                    //};
                }
            }
          
        }
        public string Name { get; }

        public UssdNavigatorTypes NavType { get;}
       // public dynamic Value { get; set; }
        public Dictionary<string, dynamic> Value { get; set; }
    }
}