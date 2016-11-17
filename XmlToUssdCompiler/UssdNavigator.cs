namespace XmlToUssdCompiler
{
    public class UssdNavigator
    {
        public UssdNavigator(string pointerStream)
        {
            if (pointerStream.StartsWith("ussd:action"))
            {
                NavType = UssdNavigatorTypes.ToAction;
                Id = pointerStream.Replace("ussd:action:", string.Empty);
            }
            if (pointerStream.StartsWith("ussd:script"))
            {
                NavType = UssdNavigatorTypes.ToScript;
                Id = pointerStream.Replace("ussd:script:", string.Empty);
            }
            else
            {
                NavType = UssdNavigatorTypes.ToScreen;
                Id = pointerStream.Replace("ussd:nav:", string.Empty);
                UssdScreen = new UssdScreen
                {
                    Id = Id
                };
            }
           
        }

        public string Id { get; set; }
        public UssdScreen UssdScreen { get; set; }
        public UssdNavigatorTypes NavType { get; set; }
    }
}