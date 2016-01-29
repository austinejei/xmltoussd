namespace XmlToUssdCompiler
{
    public class UssdFormListChoice
    {
        public string Text { get; set; }
        public string Selector { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return string.Format("{0}.{1}",Selector,Text);
        }
    }
}