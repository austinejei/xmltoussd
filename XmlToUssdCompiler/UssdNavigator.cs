namespace XmlToUssdCompiler
{
    public class UssdNavigator
    {
        public UssdNavigator(string pointerStream)
        {
            if (pointerStream.Contains("ussd:action"))
            {
                NavType = UssdNavigatorTypes.ToAction;
                Id = pointerStream.Replace("ussd:action:", string.Empty);
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