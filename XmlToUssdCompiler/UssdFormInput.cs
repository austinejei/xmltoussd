namespace XmlToUssdCompiler
{
    public class UssdFormInput
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Display { get; set; }
        public int? MaxLength { get; set; }
        public bool Required { get; set; }
        public bool IsList { get; set; }
        public string ListId { get; set; }
    }
}