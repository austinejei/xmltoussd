namespace XmlToUssdCompiler.ScriptModels
{
    public class LussdRequestContext
    {
        public string Mobile { get; set; }
        public string SessionId { get; set; }
        public string ServiceCode { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string Operator { get; set; }

        public LussdRequestTypes RequestType
        {
            get
            {
                switch (Type.ToLower())
                {
                    case "initiation":
                        return LussdRequestTypes.Initiation;
                    case "response":
                        return LussdRequestTypes.Response;
                    case "release":
                        return LussdRequestTypes.Release;
                    default:
                        return LussdRequestTypes.Timeout;
                }
            }
        }

        public string TrimmedMessage
        {
            get { return Message.Trim(); }
        }
    }

    public enum LussdRequestTypes
    {
        Initiation,
        Response,
        Release,
        Timeout,
    }
}