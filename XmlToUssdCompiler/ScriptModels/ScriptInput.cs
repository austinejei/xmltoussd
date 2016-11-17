namespace XmlToUssdCompiler.ScriptModels
{
    public class ScriptInput
    {
        public ScriptInput(LussdRequestContext request,dynamic bindFrom)
        {
            Request = request;
            BindFrom =bindFrom;
    
        }
        public LussdRequestContext Request { get; set; }
        public dynamic BindFrom { get; set; }

   
    }
}