namespace XmlToUssdCompiler
{
    public class UssdMenuOption
    {
        public UssdMenuOption()
        {
            OnSelect = new UssdNavigator("ussd:nav:ussd");
        }
        public string Text { get; set; }
        public string Value { get; set; }
        public UssdNavigator OnSelect { get; set; }
    }
}