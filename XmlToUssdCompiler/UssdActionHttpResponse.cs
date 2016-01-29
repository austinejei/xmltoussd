namespace XmlToUssdCompiler
{
    public class UssdActionHttpResponse
    {
        public UssdActionHttpResponse()
        {
            Goto = new UssdNavigator("ussd:nav:ussd");
        }
        public UssdNavigator Goto { get; set; }
        public int Value { get; set; }
    }
}