using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XmlToUssdCompiler.ScriptModels;

namespace XmlToUssdCompiler
{
    public class UssdXml
    {
        public string Filename { get; set; }

        public UssdXml(string filename)
        {
            Filename = filename;

            ReadFile();
        }

        private XmlDocument _doc;
        private void ReadFile()
        {
           var fileInfo = new FileInfo(Filename);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"file, {Filename}, not found on disk.");
            }

            if (!".xml".Equals(fileInfo.Extension,StringComparison.CurrentCultureIgnoreCase))
            {
                throw new Exception("only *.xml files allowed");
            }

            if (fileInfo.Length<=0)
            {
                throw new Exception("file is empty");
            }
            _doc= new XmlDocument();
            
            _doc.Load(Filename);
        }


        public List<UssdScreen> GetScreens()
        {
            return _screens;
        }

        public Dictionary<string, dynamic> GetBoundModels()
        {
            return _boundModels;
        }

        public List<UssdFormList> GetFormLists()
        {
            return _formLists;
        }

        public List<UssdForm> GetForms()
        {
            return _forms;
        }

        public List<UssdMenu> GetMenus()
        {
            return _menus;
        }

        public List<UssdAction> GetActions()
        {
            return _actions;
        }

        public List<UssdScript> GetScripts()
        {
            return _scripts;
        }

        List<UssdScreen> _screens;
        List<UssdScript> _scripts;
        List<UssdAction> _actions;
        List<UssdFormList> _formLists;
        List<UssdForm> _forms;
        List<UssdMenu> _menus;
        Dictionary<string,dynamic> _boundModels;
        public void Parse(bool simulator=true)
        {
            var rootNode = _doc.SelectNodes("ussd");
           
            _screens = new List<UssdScreen>();
            _scripts = new List<UssdScript>();
            _actions = new List<UssdAction>();
            _formLists = new List<UssdFormList>();
            _forms = new List<UssdForm>();
            _menus = new List<UssdMenu>();
            _boundModels = new  Dictionary<string, dynamic>();


            if (rootNode==null)
            {
                throw new Exception("no \"ussd\" element found");
            }

            //gather scripts
            foreach (XmlNode node in rootNode)
            {
                foreach (XmlNode ussdElement in node.ChildNodes)
                {
                    if (ussdElement.Name == "script")
                    {
                        if (ussdElement.Attributes.GetNamedItem("id") == null)
                        {
                            throw new Exception("script attribute \"id\" is required");
                        }

                       
                        var scriptId = ussdElement.Attributes["id"].InnerText;

                        Console.WriteLine("gotten script {0}", scriptId);
                        if (_screens.Any(s => s.Id == scriptId))
                        {
                            throw new Exception($"script id already exists at {scriptId}");
                        }
                        
                        var script = new UssdScript
                        {
                            Id = scriptId,
                            Content = ussdElement.InnerText,
                            BindFrom = ussdElement.Attributes.GetNamedItem("bindfrom") != null?ussdElement.Attributes["bindfrom"].InnerText:"",
                            BindTo= ussdElement.Attributes.GetNamedItem("bindto") != null?ussdElement.Attributes["bindto"].InnerText:"",
                            
                        };

                        _scripts.Add(script);
                        Console.WriteLine("added script {0}",scriptId);
                    }
                }
            }


            foreach (XmlNode node in rootNode)
            {
                foreach (XmlNode ussdElement in node.ChildNodes)
                {

                    #region screens

                    if (ussdElement.Name=="screen")
                    {
                        if (ussdElement.Attributes["id"] == null)
                        {
                            throw new Exception("screen id is required");
                        }
                        var screenId = ussdElement.Attributes["id"].InnerText;

                        Console.WriteLine("gotten screen {0}", screenId);
                        if (_screens.Any(s => s.Id == screenId))
                        {
                            throw new Exception($"screen id already exists at {screenId}");
                        }

                        var ussdScreen = new UssdScreen
                        {
                            Id = screenId,

                        };

                        foreach (XmlNode screenItem in ussdElement.ChildNodes)
                        {
                            #region Menu Element
                            if (screenItem.Name.Equals("menu"))
                            {
                                var menuNodes = screenItem.ChildNodes;

                                var ussdMenu = new UssdMenu();
                                if (screenItem.Attributes.Count > 0)
                                {
                                    if (screenItem.Attributes["bindfrom"] != null)
                                    {
                                        //todo: always check if there's a corresponding bindto in another screen
                                        ussdMenu.BindFrom = new UssdBoundModel
                                        {
                                            Name = screenItem.Attributes["bindfrom"].InnerText
                                        };

                                    }
                                    if (screenItem.Attributes["bindaction"] != null)
                                    {

                                        ussdMenu.BindAction = new UssdBoundAction(screenItem.Attributes["bindaction"].InnerText);

                                    }

                                }

                                foreach (XmlNode menuNode in menuNodes)
                                {

                                    if (menuNode.Name == "title")
                                    {
                                        ussdMenu.Title = menuNode.InnerText.ToUssdString(simulator);
                                    }

                                    if (menuNode.Name == "option")
                                    {
                                        var menuOption = new UssdMenuOption
                                        {
                                            Text = menuNode.InnerText,
                                            Value = menuNode.Attributes["value"].InnerText.ToUssdString(simulator),
                                            OnSelect = new UssdNavigator(menuNode.Attributes["onselect"].InnerText)
                                        };

                                        ussdMenu.Options.Add(menuOption);


                                    }
                                }


                                ussdScreen.UssdItems.Add(ussdMenu);
                                _menus.Add(ussdMenu);
                                Console.WriteLine("added menu {0}", ussdMenu.Title);
                            }
                            #endregion

                            #region Form Element
                            if (screenItem.Name.Equals("form"))
                            {
                                var formNodes = screenItem.ChildNodes;

                                var ussdForm = new UssdForm();
                                if (screenItem.Attributes.Count > 0)
                                {
                                    if (screenItem.Attributes["bindto"] != null)
                                    {
                                        ussdForm.BindTo = new UssdBoundModel
                                        {
                                            Name = screenItem.Attributes["bindto"].InnerText
                                        };
                                        //_boundModels[screenItem.Attributes["bindto"].InnerText] = new Dictionary<string, string>();

                                    }
                                    if (screenItem.Attributes["onsubmit"] != null)
                                    {
                                        ussdForm.OnSubmit = new UssdNavigator(screenItem.Attributes["onsubmit"].InnerText);

                                    }
                                    else
                                    {
                                        throw new Exception("form attribute \"onsubmit\" is required");
                                    }


                                }

                                foreach (XmlNode formNode in formNodes)
                                {

                                    if (formNode.Name == "title")
                                    {
                                        ussdForm.Title = formNode.InnerText.ToUssdString(simulator);
                                    }

                                    if (formNode.Name == "input")
                                    {
                                        var ussdFormInput = new UssdFormInput
                                        {

                                            Id = formNode.Attributes["id"].InnerText,
                                            Type = formNode.Attributes["type"].InnerText,
                                            Display = formNode.Attributes["display"].Value.ToUssdString(simulator),
                                            Required =
                                                                 bool.Parse(formNode.Attributes["required"].InnerText),
                                            MaxLength = formNode.Attributes["maxlength"] == null ? (int?)null : int.Parse(formNode.Attributes["maxlength"].InnerText)
                                        };

                                        if (ussdFormInput.Type == "list")
                                        {
                                            ussdFormInput.IsList = true;
                                            ussdFormInput.ListId = formNode.Attributes["listid"].InnerText;
                                        }



                                        ussdForm.Inputs.Add(ussdFormInput);


                                    }
                                    if (formNode.Name == "list")
                                    {


                                        var formListNodes = formNode.ChildNodes;

                                        var ussdFormList = new UssdFormList { Id = formNode.Attributes["id"].InnerText };



                                        bool hasRepeater = formNode.Attributes.GetNamedItem("repeater") != null;

                                        if (hasRepeater)
                                        {
                                       
                                         
                                            ussdFormList.Repeater = formNode.Attributes["repeater"].InnerText;

                                            var scriptItem =
                                                _scripts.FirstOrDefault(s => s.Id == ussdFormList.RepeaterScriptId);
                                            if (scriptItem == null)
                                            {
                                                throw new Exception(
                                                    $"script id \"{ussdFormList.RepeaterScriptId}\" not found");
                                            }

                                            
                                        }
                                        else
                                        {
                                            foreach (XmlNode formListNode in formListNodes)
                                            {

                                                ussdFormList.Choices.Add(new UssdFormListChoice
                                                {
                                                    Text = formListNode.InnerText,
                                                    Selector = formListNode.Attributes["selector"].InnerText,
                                                    Value = formListNode.Attributes["value"].InnerText
                                                });
                                            }
                                        }

                                        _formLists.Add(ussdFormList);

                                    }
                                }


                                ussdScreen.UssdItems.Add(ussdForm);
                                _forms.Add(ussdForm);

                                Console.WriteLine("added form {0}", ussdForm.Title);
                            }

                            #endregion

                            #region Text Element
                            if (screenItem.Name.Equals("text"))
                            {
                                var ussdText = new UssdText(screenItem.InnerText.ToUssdString(simulator));
                                if (screenItem.Attributes.Count > 0)
                                {
                                    if (screenItem.Attributes["bindfrom"] != null)
                                    {
                                        //todo: always check if there's a corresponding bindto in another screen
                                        ussdText.BindFrom = new UssdBoundModel
                                        {
                                            Name = screenItem.Attributes["bindfrom"].InnerText
                                        };

                                    }
                                    if (screenItem.Attributes["bindaction"] != null)
                                    {

                                        ussdText.BindAction = new UssdBoundAction(screenItem.Attributes["bindaction"].InnerText);

                                    }

                                }
                                ussdScreen.UssdItems.Add(ussdText);
                            }
                            #endregion

                            #region Action Element
                            if (screenItem.Name.Equals("action"))
                            {
                                var actionNode = screenItem.ChildNodes;

                                var ussdAction = new UssdAction();
                                if (screenItem.Attributes.Count > 0)
                                {
                                    if (screenItem.Attributes["id"] != null)
                                    {
                                        ussdAction.Id = screenItem.Attributes["id"].InnerText;

                                    }
                                    else
                                    {
                                        throw new Exception("action attribute id is required");
                                    }

                                    if (screenItem.Attributes["bindto"] != null)
                                    {
                                        ussdAction.BindTo = new UssdBoundModel
                                        {
                                            Name = screenItem.Attributes["bindto"].InnerText
                                        };
                                     //   _boundModels[screenItem.Attributes["bindto"].InnerText] = new Dictionary<string, string>();

                                    }
                                    else
                                    {
                                        throw new Exception("action attribute bindto is required");
                                    }

                                    if (screenItem.Attributes["cycle"] != null)
                                    {
                                        UssdActionCycle cycle;
                                        if (!Enum.TryParse(screenItem.Attributes["cycle"].InnerText, true, out cycle))
                                        {
                                            throw new Exception(String.Format("action attribute cycle has an unknown value. Allowed values are: {0} or {1}", UssdActionCycle.Always, UssdActionCycle.Once));
                                        }

                                        ussdAction.Http.Cycle = cycle;
                                    }
                                }
                                else
                                {
                                    throw new Exception("form attribute id and bindto is required");
                                }

                                foreach (XmlNode xmlNode in actionNode)
                                {


                                    if (xmlNode.Attributes.Count > 0)
                                    {
                                        if (xmlNode.Attributes["url"] != null)
                                        {
                                            ussdAction.Http.Url = xmlNode.Attributes["url"].InnerText.ToUssdString(simulator);

                                        }
                                        else
                                        {
                                            throw new Exception("http attribute url is required");
                                        }
                                        if (xmlNode.Attributes["method"] != null)
                                        {
                                            UssdActionHttpMethod httpMethod;
                                            if (!Enum.TryParse(xmlNode.Attributes["method"].InnerText, true, out httpMethod))
                                            {
                                                throw new Exception("http attribute method is invalid");
                                            }

                                            ussdAction.Http.Method = httpMethod;
                                        }
                                        else
                                        {
                                            throw new Exception("http attribute method is required");
                                        }
                                    }


                                    if (xmlNode.Name == "http")
                                    {
                                        var httpNodes = xmlNode.ChildNodes;

                                        foreach (XmlNode httpNode in httpNodes)
                                        {
                                            if (httpNode.Name == "header")
                                            {
                                                var headers = httpNode.ChildNodes;

                                                foreach (XmlNode header in headers)
                                                {
                                                    if (header.Name == "add")
                                                    {
                                                        var ussdActionHttpHeader = new UssdActionHttpHeader
                                                        {

                                                            Name = header.Attributes["key"].InnerText,
                                                            Value = header.Attributes["value"].InnerText,

                                                        };

                                                        ussdAction.Http.Headers.Add(ussdActionHttpHeader);
                                                    }

                                                }
                                            }
                                            if (httpNode.Name == "body")
                                            {
                                                if (httpNode.Attributes["bindfrom"] != null)
                                                {

                                                    ussdAction.Http.Body.BindFrom = new UssdBoundModel
                                                    {
                                                        Name = httpNode.Attributes["bindfrom"].InnerText
                                                    };

                                                }

                                                var headers = httpNode.ChildNodes;

                                                foreach (XmlNode header in headers)
                                                {
                                                    if (header.Name == "add")
                                                    {
                                                        var ussdActionHttpBody = new UssdActionHttpBodyParam
                                                        {

                                                            Name = header.Attributes["key"].InnerText,
                                                            Value = header.Attributes["value"].InnerText,

                                                        };

                                                        ussdAction.Http.Body.Params.Add(ussdActionHttpBody);
                                                    }

                                                }
                                            }
                                            if (httpNode.Name == "response")
                                            {
                                                var responses = httpNode.ChildNodes;

                                                foreach (XmlNode response in responses)
                                                {
                                                    if (response.Name == "code")
                                                    {
                                                        var ussdActionHttpResponse = new UssdActionHttpResponse
                                                        {

                                                            Value = int.Parse(response.Attributes["value"].InnerText),
                                                            Goto = new UssdNavigator(response.Attributes["goto"].InnerText),

                                                        };

                                                        ussdAction.Http.Responses.Add(ussdActionHttpResponse);
                                                    }

                                                }
                                            }
                                        }



                                    }
                                }


                                ussdScreen.UssdItems.Add(ussdAction);
                                _actions.Add(ussdAction);
                                Console.WriteLine("added action {0}", ussdAction.Id);
                            }

                            #endregion

                        }

                        if (ussdElement.Attributes["main"] != null
                            )
                        {
                            var mainString = ussdElement.Attributes["main"].InnerText;

                            bool isMain;

                            if (bool.TryParse(mainString, out isMain))
                            {
                                ussdScreen.Main = isMain;
                            }
                        }

                        _screens.Add(ussdScreen);

                        Console.WriteLine("{0} screens", _screens.Count);
                    }
                   
                    #endregion

                }
            }

            if (_screens.Count == 0)
            {
                throw new Exception("no screens");
            }

            if (!_screens.Any(s=>s.Main))
            {
                throw new Exception("no main attribute on any of the screens. Please include main=\"true\" as attribute on your main screen element");
            }
        }

        public void RunSimulator()
        {
            //var firstScreen = _screens[0];
            var firstScreen = _screens.FirstOrDefault(x=>x.Main);

            if (firstScreen==null)
            {
                firstScreen = _screens[0];
            }
            ShowScreen(firstScreen);
        }


        private readonly Assembly[] _scriptAssemblies = {
            typeof(Dictionary<,>).Assembly,
            typeof(JsonConvert).Assembly,
            typeof(HttpClient).Assembly,
            typeof(Task).Assembly,
            typeof(ScriptInput).Assembly,
            
        };

        private readonly string[] _scriptImports = {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Net",
            "System.Net.Http",
            "System.Net.Http.Headers",
            "System.Threading.Tasks",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq",
            "XmlToUssdCompiler.ScriptModels"
        };
        private void ShowScreen(UssdScreen firstScreen)
        {
            
            foreach (var ussdItem in firstScreen.UssdItems)
            {

#region Menu
                if (ussdItem.GetType() == typeof (UssdMenu))
                {
                    var ussdMenu = (UssdMenu) ussdItem;

                    if (!string.IsNullOrEmpty(ussdMenu.BindAction.Name)) //bindaction is priority now
                    {

                        //menu action is not always a UssdAction
                        if (ussdMenu.BindAction.NavType == UssdNavigatorTypes.ToScript)
                        {
                            var script = _scripts.FirstOrDefault(s => s.Id == ussdMenu.BindAction.Name);

                            if (script == null)
                            {
                                throw new Exception($"script with ID {ussdMenu.BindAction.Name} not found");
                            }

                            ScriptOutput scriptResult;
                            try
                            {
                                var scriptOptions = ScriptOptions.Default.
                                    WithReferences(_scriptAssemblies)
                                    .WithImports(_scriptImports)
                                    .WithSourceResolver(new SourceFileResolver(new[] { "" },
                                        AppDomain.CurrentDomain.BaseDirectory));

                                dynamic eo2 = null;
                                var eo = new ExpandoObject();

                                var eoColl = (ICollection<KeyValuePair<string, dynamic>>)eo;

                                var scriptContent = script.Content;
                                if (!string.IsNullOrEmpty(script.BindFrom))
                                {
                                    //a hack to ensure that the proper class instance name is called instead of dev's "user.name"
                                    scriptContent = scriptContent.Replace(script.BindFrom + ".", "BindFrom.");

                                    var bindFromDict = _boundModels[script.BindFrom];

                                    foreach (var kvp in bindFromDict)
                                    {
                                        eoColl.Add(new KeyValuePair<string, dynamic>(kvp.Key, kvp.Value));
                                    }
                                   // eo2 = bindFromDict;
                                }

                                dynamic eoDynamic = eo;
                              

                                scriptResult = CSharpScript.EvaluateAsync<ScriptOutput>(scriptContent, scriptOptions, new ScriptInput(new LussdRequestContext(), eoDynamic), typeof(ScriptInput)).Result;

                                if (!string.IsNullOrEmpty(script.BindTo))
                                {
                                    //var sr = JsonConvert.SerializeObject(scriptResult.Response);

                                    //var dictionaryOfScriptResult =JsonConvert.DeserializeObject<Dictionary<string, string>>(sr);

                                    _boundModels[script.BindTo] = scriptResult.Response;

                                }
                                if (!string.IsNullOrEmpty(scriptResult.NextScreen))
                                {
                                    var nextScreen = _screens.FirstOrDefault(s => s.Id == scriptResult.NextScreen);
                                    if (nextScreen == null)
                                    {
                                        throw new Exception($"unknown screen \"{scriptResult.NextScreen}\"");
                                    }
                                    ShowScreen(nextScreen);
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                        if (ussdMenu.BindAction.NavType == UssdNavigatorTypes.ToScreen)
                        {
                            var screenToNavigate = _screens.FirstOrDefault(s => s.Id == ussdMenu.BindAction.Name);

                            if (screenToNavigate == null)
                            {
                                throw new Exception($"screen id \"{ussdMenu.BindAction.Name}\" does not exist");
                            }

                            ShowScreen(screenToNavigate);
                        }

                        if (ussdMenu.BindAction.NavType == UssdNavigatorTypes.ToAction)
                        {
                            UssdAction actionItem = _actions.FirstOrDefault(a => a.Id == ussdMenu.BindAction.Name);

                            if (actionItem == null)
                            {
                                throw new Exception($"undefined action {ussdMenu.BindAction.Name}");
                            }

                            if (actionItem.Http.Cycle == UssdActionCycle.Once)
                            {
                                //todo: might throw an error
                                if (_boundModels[actionItem.BindTo.Name].Any()) //todo: this might not be so necessary, cos a developer may just want to call an API, no binding
                                {

                                }
                                else
                                {


                                    var actionResponse = GrabActionResponse(actionItem).Result;


                                    var bdict = new Dictionary<string, dynamic>();

                                    foreach (var property in actionResponse.Item2.Properties())
                                    {
                                        bdict.Add(property.Name, property.Value);
                                    }

                                    ussdMenu.BindAction.Value = bdict;

                                    _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;

                                    Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                                    if (actionItem.Http.Responses.Any())
                                    {
                                        var navigatorLabel =
                                            actionItem.Http.Responses.FirstOrDefault(
                                                r => r.Value == (int)actionResponse.Item1);

                                        if (navigatorLabel != null)
                                        {
                                            //todo: we need to discourage "ussd:action" for now....
                                            ShowScreen(
                                                _screens.FirstOrDefault(
                                                    s => s.Id == navigatorLabel.Goto.Id));
                                            return;
                                        }
                                    }
                                    //no replacement occured....HTTP failed.

                                }

                            }
                            else if (actionItem.Http.Cycle == UssdActionCycle.Always)
                            {


                                var actionResponse = GrabActionResponse(actionItem).Result;


                                var bdict = new Dictionary<string, dynamic>();

                                foreach (var property in actionResponse.Item2.Properties())
                                {
                                    bdict.Add(property.Name, property.Value);
                                }

                                ussdMenu.BindAction.Value = bdict;

                                _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;

                                Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                                if (actionItem.Http.Responses.Any())
                                {
                                    var navigatorLabel =
                                        actionItem.Http.Responses.FirstOrDefault(
                                            r => r.Value == (int)actionResponse.Item1);

                                    if (navigatorLabel != null)
                                    {
                                        //todo: we need to discourage "ussd:action" for now....
                                        ShowScreen(
                                            _screens.FirstOrDefault(
                                                s => s.Id == navigatorLabel.Goto.Id));
                                        return;
                                    }
                                }
                            }
                        }

                    }

                    var model = _boundModels.Keys.FirstOrDefault(k => k == ussdMenu.BindFrom.Name);

                    if (model == null)
                    {
                        Console.WriteLine(ussdMenu.Title.Trim());
                    }
                    else
                    {
                        var title = ussdMenu.Title.Trim();
                        var boundModel = _boundModels[ussdMenu.BindFrom.Name];

                        title = title.ToUssdString();

                        dynamic dynamicExpression = boundModel;
                        //end

                        try
                        {
                            var sContent = "var " + ussdMenu.BindFrom.Name + " = LussdDynamicExpression; " + "\r\n return $\"" + title + "\";";
                            var scriptOptions = ScriptOptions.Default.
                                        WithReferences(_scriptAssemblies)
                                        .WithImports(_scriptImports)
                                        .WithSourceResolver(new SourceFileResolver(new[] { "" },
                                            AppDomain.CurrentDomain.BaseDirectory));
                            var scriptResult = CSharpScript.EvaluateAsync<string>(sContent, scriptOptions, new LussdDynamicExpressionObject { LussdDynamicExpression = dynamicExpression }, typeof(LussdDynamicExpressionObject)).Result;

                            Console.WriteLine(scriptResult);
                        }
                        catch (Exception exception)
                        {
                            throw new Exception(exception.Message);
                        }

                        
                    }


                    foreach (var ussdMenuOption in ussdMenu.Options)
                    {
                        //todo: check for variable expressions
                        if (model != null)
                        {
                            var title = ussdMenuOption.Text;
                            var boundModel = _boundModels[ussdMenu.BindFrom.Name];

                            title = title.ToUssdString();

                            dynamic dynamicExpression = boundModel;
                            //end

                            try
                            {
                                var sContent = "var " + ussdMenu.BindFrom.Name + " = LussdDynamicExpression; " + "\r\n return $\"" + title + "\";";
                                var scriptOptions = ScriptOptions.Default.
                                            WithReferences(_scriptAssemblies)
                                            .WithImports(_scriptImports)
                                            .WithSourceResolver(new SourceFileResolver(new[] { "" },
                                                AppDomain.CurrentDomain.BaseDirectory));
                                var scriptResult = CSharpScript.EvaluateAsync<string>(sContent, scriptOptions, new LussdDynamicExpressionObject { LussdDynamicExpression = dynamicExpression }, typeof(LussdDynamicExpressionObject)).Result;

                                Console.WriteLine(scriptResult);
                            }
                            catch (Exception exception)
                            {
                                throw new Exception(exception.Message);
                            }
                        }
                        else
                        {
                            Console.WriteLine(ussdMenuOption.Text);
                        }
                      
                    }

                    var resp = Console.ReadLine();
                    while (true)
                    {
                        if (ussdMenu.Options.FirstOrDefault(s => s.Value == resp) != null)
                        {
                            break;
                        }
                        resp = Console.ReadLine();
                    }

                    var menuOption = ussdMenu.Options.FirstOrDefault(s => s.Value == resp);
                    if (menuOption.OnSelect.NavType== UssdNavigatorTypes.ToScript)
                    {
                        var script = _scripts.FirstOrDefault(s => s.Id == menuOption.OnSelect.Id);

                        if (script == null)
                        {
                            throw new Exception($"script with ID {menuOption.OnSelect.Id} not found");
                        }

                        ScriptOutput scriptResult;
                        try
                        {
                            var scriptOptions = ScriptOptions.Default.
                                WithReferences(_scriptAssemblies)
                                .WithImports(_scriptImports)
                                .WithSourceResolver(new SourceFileResolver(new[] { "" },
                                    AppDomain.CurrentDomain.BaseDirectory));


                           // var eo = new ExpandoObject();
                            dynamic eo2 = null;
                          //  var eoColl = (ICollection<KeyValuePair<string, dynamic>>)eo;

                            if (!string.IsNullOrEmpty(script.BindFrom))
                            {
                                var bindFromDict = _boundModels[script.BindFrom];

                                
                                //foreach (var kvp in bindFromDict)
                                //{
                                //    eoColl.Add(new KeyValuePair<string, dynamic>(kvp.Key, kvp.Value));
                                //}

                                eo2 = bindFromDict;
                            }

                            dynamic eoDynamic = eo2;
                            var scriptContent = script.Content;
                            //a hack to ensure that the proper class instance name is called instead of dev's "user.name"
                            scriptContent = scriptContent.Replace(script.BindFrom + ".", "BindFrom.");

                            scriptResult = CSharpScript.EvaluateAsync<ScriptOutput>(scriptContent, scriptOptions, new ScriptInput(new LussdRequestContext(), eoDynamic), typeof(ScriptInput)).Result;

                            if (!string.IsNullOrEmpty(script.BindTo))
                            {
                                //var sr = JsonConvert.SerializeObject(scriptResult.Response);

                               // var dictionaryOfScriptResult =
                                 //   JsonConvert.DeserializeObject<Dictionary<string, string>>(sr);

                                _boundModels[script.BindTo] = scriptResult.Response;

                            }
                            if (!string.IsNullOrEmpty(scriptResult.NextScreen))
                            {
                                var nextScreen = _screens.FirstOrDefault(s => s.Id == scriptResult.NextScreen);
                                if (nextScreen == null)
                                {
                                    throw new Exception($"unknown screen \"{scriptResult.NextScreen}\"");
                                }
                                ShowScreen(nextScreen);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }
                    }
                    if (menuOption.OnSelect.NavType==UssdNavigatorTypes.ToAction)
                    {
                 
                        var actionItem = _actions.FirstOrDefault(a => a.Id == menuOption.OnSelect.Id);
                        if (actionItem == null)
                        {
                            throw new Exception($"undefined action {ussdMenu.BindAction.Name}");
                        }

                        if (actionItem.Http.Cycle == UssdActionCycle.Once)
                        {
                            if (_boundModels[actionItem.BindTo.Name].Any()) //todo: this might not be so necessary, cos a developer may just want to call an API, no binding
                            {

                            }
                            else
                            {

                                var actionResponse = GrabActionResponse(actionItem).Result;


                                var bdict = new Dictionary<string, dynamic>();

                                foreach (var property in actionResponse.Item2.Properties())
                                {
                                    bdict.Add(property.Name, property.Value);
                                }

                                ussdMenu.BindAction.Value = bdict;

                                _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;

                                Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                                if (actionItem.Http.Responses.Any())
                                {
                                    var navigatorLabel =
                                        actionItem.Http.Responses.FirstOrDefault(
                                            r => r.Value == (int)actionResponse.Item1);

                                    if (navigatorLabel != null)
                                    {
                                        //todo: we need to discourage "ussd:action" for now....
                                        ShowScreen(
                                            _screens.FirstOrDefault(
                                                s => s.Id == navigatorLabel.Goto.Id));
                                        return;
                                    }
                                }
        

                            }

                        }
                        else if (actionItem.Http.Cycle == UssdActionCycle.Always)
                        {
                         
                            //should always do fresh bind
                                var actionResponse = GrabActionResponse(actionItem).Result;


                            var bdict = new Dictionary<string, dynamic>();

                            foreach (var property in actionResponse.Item2.Properties())
                            {
                                bdict.Add(property.Name, property.Value);
                            }

                            ussdMenu.BindAction.Value = bdict;

                                _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;

                                Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                                if (actionItem.Http.Responses.Any())
                                {
                                    var navigatorLabel =
                                        actionItem.Http.Responses.FirstOrDefault(
                                            r => r.Value == (int)actionResponse.Item1);

                                    if (navigatorLabel != null)
                                    {
                                        //todo: we need to discourage "ussd:action" for now....
                                        ShowScreen(
                                            _screens.FirstOrDefault(
                                                s => s.Id == navigatorLabel.Goto.Id));
                                        return;
                                    }
                                }

                        }
                    }
                    if (menuOption.OnSelect.NavType== UssdNavigatorTypes.ToScreen)
                    {
                       // Console.WriteLine("navigating to {0}", menuOption.OnSelect.UssdScreen.Id);
                        var screenToNavigate = _screens.FirstOrDefault(s => s.Id == menuOption.OnSelect.UssdScreen.Id);

                        if (screenToNavigate == null)
                        {
                            throw new Exception($"screen id \"{menuOption.OnSelect.UssdScreen.Id}\" does not exist");
                        }

                        ShowScreen(screenToNavigate);
                        
                    }
                  
                }

                #endregion Menu

#region Text
                if (ussdItem.GetType() == typeof (UssdText))
                {
                    var ussdText = (UssdText) ussdItem;

                    if (!string.IsNullOrEmpty(ussdText.BindAction.Name))
                    {

                        if (ussdText.BindAction.NavType== UssdNavigatorTypes.ToScreen)
                        {
                            var screenToNavigate = _screens.FirstOrDefault(s => s.Id == ussdText.BindAction.Name);

                            if (screenToNavigate == null)
                            {
                                throw new Exception($"screen id \"{ussdText.BindAction.Name}\" does not exist");
                            }

                            ShowScreen(screenToNavigate);
                        }
                        if (ussdText.BindAction.NavType == UssdNavigatorTypes.ToScript)
                        {
                            var script = _scripts.FirstOrDefault(s => s.Id == ussdText.BindAction.Name);

                            if (script == null)
                            {
                                throw new Exception($"script with ID {ussdText.BindAction.Name} not found");
                            }

                            ScriptOutput scriptResult;
                            try
                            {
                                var scriptOptions = ScriptOptions.Default.
                                    WithReferences(_scriptAssemblies)
                                    .WithImports(_scriptImports)
                                    .WithSourceResolver(new SourceFileResolver(new[] { "" },
                                        AppDomain.CurrentDomain.BaseDirectory));


                                //var eo = new ExpandoObject();
                             

                                //var eoColl = (ICollection<KeyValuePair<string, dynamic>>)eo;

                                var scriptContent = script.Content;
                                dynamic eo2 = null;
                                if (!string.IsNullOrEmpty(script.BindFrom))
                                {
                                    //a hack to ensure that the proper class instance name is called instead of dev's "user.name"
                                    scriptContent = scriptContent.Replace(script.BindFrom + ".", "BindFrom.");


                                    var bindFromDict = _boundModels[script.BindFrom];

                                    //foreach (var kvp in bindFromDict)
                                    //{
                                    //    eoColl.Add(new KeyValuePair<string, dynamic>(kvp.Key, kvp.Value));
                                    //}

                                    eo2 = bindFromDict;
                                }

                                dynamic eoDynamic = eo2;
                               
                                scriptResult = CSharpScript.EvaluateAsync<ScriptOutput>(scriptContent, scriptOptions, new ScriptInput(new LussdRequestContext(), eoDynamic), typeof(ScriptInput)).Result;

                                if (!string.IsNullOrEmpty(script.BindTo))
                                {
                                    //var sr = JsonConvert.SerializeObject(scriptResult.Response);

                                    //var dictionaryOfScriptResult =
                                    //    JsonConvert.DeserializeObject<Dictionary<string, string>>(sr);

                                    _boundModels[script.BindTo] = scriptResult.Response;

                                }
                                if (!string.IsNullOrEmpty(scriptResult.NextScreen))
                                {
                                    var nextScreen = _screens.FirstOrDefault(s => s.Id == scriptResult.NextScreen);
                                    if (nextScreen == null)
                                    {
                                        throw new Exception($"unknown screen \"{scriptResult.NextScreen}\"");
                                    }
                                    ShowScreen(nextScreen);
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message);
                            }
                        }
                        if (ussdText.BindAction.NavType == UssdNavigatorTypes.ToAction)
                        {
                            var actionItem = _actions.FirstOrDefault(a => a.Id == ussdText.BindAction.Name);


                            if (actionItem == null)
                            {
                                throw new Exception($"undefined action {ussdText.BindAction.Name}");
                            }

                            if (actionItem.Http.Cycle == UssdActionCycle.Once)
                            {
                                if (_boundModels[actionItem.BindTo.Name].Any())
                                {

                                }
                                else
                                {

                                    var actionResponse = GrabActionResponse(actionItem).Result;

                                    var bdict = new Dictionary<string, dynamic>();

                                    foreach (var property in actionResponse.Item2.Properties())
                                    {
                                        bdict.Add(property.Name, property.Value);
                                    }

                                    ussdText.BindAction.Value =bdict;

                                    _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;



                                    Console.WriteLine("HTTP response {0}", actionResponse.Item1);
                                    //no replacement occured....HTTP failed.
                                    if (actionItem.Http.Responses.Any())
                                    {
                                        var navigatorLabel =
                                            actionItem.Http.Responses.FirstOrDefault(
                                                r => r.Value == (int)actionResponse.Item1);

                                        if (navigatorLabel != null)
                                        {
                                            //todo: check which type of label it is
                                            ShowScreen(
                                                _screens.FirstOrDefault(
                                                    s => s.Id == navigatorLabel.Goto.Id));
                                            return;
                                        }
                                    }

                                }

                            }
                            else if (actionItem.Http.Cycle == UssdActionCycle.Always)
                            {


                                var actionResponse = GrabActionResponse(actionItem).Result;


                                var bdict = new Dictionary<string, dynamic>();

                                foreach (var property in actionResponse.Item2.Properties())
                                {
                                    bdict.Add(property.Name, property.Value);
                                }

                                ussdText.BindAction.Value = bdict;

                                _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;

                                Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                                if (actionItem.Http.Responses.Any())
                                {
                                    var navigatorLabel =
                                        actionItem.Http.Responses.FirstOrDefault(
                                            r => r.Value == (int)actionResponse.Item1);

                                    if (navigatorLabel != null)
                                    {
                                        //todo: we need to discourage "ussd:action" for now....
                                        ShowScreen(
                                            _screens.FirstOrDefault(
                                                s => s.Id == navigatorLabel.Goto.Id));
                                        return;
                                    }
                                }




                            }
                        }
                        

                    }

                    var model = _boundModels.Keys.FirstOrDefault(k => k == ussdText.BindFrom.Name);

                    if (model == null)
                    {
                        Console.WriteLine(ussdText.Text.Trim());
                    }
                    else
                    {
                       

                        var title = ussdText.Text.Trim();
                        var boundModel = _boundModels[ussdText.BindFrom.Name];

                        title = title.ToUssdString();

                        //var eo = new ExpandoObject();

                        //var eoColl = (ICollection<KeyValuePair<string, dynamic>>)eo;
                        
                        //foreach (var kvp in boundModel)
                        //{
                        //    eoColl.Add(new KeyValuePair<string, dynamic>(kvp.Key, kvp.Value));
                        //}

                        dynamic dynamicExpression = boundModel;
                        //end

                        try
                        {
                            var sContent = "var " + ussdText.BindFrom.Name + " = LussdDynamicExpression; " + "\r\n return $\"" + title + "\";";
                            var scriptOptions = ScriptOptions.Default.
                                        WithReferences(_scriptAssemblies)
                                        .WithImports(_scriptImports)
                                        .WithSourceResolver(new SourceFileResolver(new[] { "" },
                                            AppDomain.CurrentDomain.BaseDirectory));
                            var scriptResult = CSharpScript.EvaluateAsync<string>(sContent, scriptOptions, new LussdDynamicExpressionObject { LussdDynamicExpression = dynamicExpression }, typeof(LussdDynamicExpressionObject)).Result;

                            Console.WriteLine(scriptResult);
                        }
                        catch (Exception exception)
                        {
                            throw new Exception(exception.Message);
                        }
                    
                        
                    }
                   
                }

                #endregion Text

#region Form
                if (ussdItem.GetType() == typeof (UssdForm))
                {
                    var ussdForm = (UssdForm) ussdItem;

                    Console.WriteLine(ussdForm.Title.Trim()); //todo: parse the title, include bindfrom for form

                    foreach (var ussdFormInput in ussdForm.Inputs)
                    {


                        Console.WriteLine(ussdFormInput.Display);

                        if (ussdFormInput.IsList)
                        {
                            var listItem = _formLists.FirstOrDefault(l => l.Id == ussdFormInput.ListId);

                            if (listItem == null)
                            {
                                throw new Exception($"list with id {ussdFormInput.ListId} not found");
                            }

                            if (listItem.HasRepeater)
                            {
                                
                                var script = _scripts.FirstOrDefault(s => s.Id == listItem.RepeaterScriptId);

                                if (script == null)
                                {
                                    throw new Exception($"script with ID {listItem.RepeaterScriptId} not found");
                                }


                                ScriptOutput scriptResult;
                                try
                                {
                                    var scriptOptions = ScriptOptions.Default.
                                        WithReferences(_scriptAssemblies)
                                        .WithImports(_scriptImports)
                                        .WithSourceResolver(new SourceFileResolver(new[] {""},
                                            AppDomain.CurrentDomain.BaseDirectory));


                                    var eo = new ExpandoObject();

                                    var eoColl = (ICollection<KeyValuePair<string, dynamic>>)eo;

                                    foreach (var kvp in ussdForm.BindTo.Value)
                                    {
                                        //eoColl.Add(new KeyValuePair<string, dynamic>(kvp.Key, kvp.Value));
                                        eoColl.Add(new KeyValuePair<string, dynamic>(kvp.Key, kvp.Value));
                                    }

                                    dynamic eoDynamic = eo;
                                    var scriptContent = script.Content;

                                    //a hack to ensure that the proper class instance name is called instead of dev's "user.name"
                                    scriptContent = scriptContent.Replace(script.BindFrom + ".", "BindFrom.");

                                    scriptResult = CSharpScript.EvaluateAsync<ScriptOutput>(scriptContent,scriptOptions,new ScriptInput(new LussdRequestContext(), eoDynamic),typeof(ScriptInput)).Result;
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception(ex.Message);
                                }


                                listItem.Choices.Clear();
                                if (scriptResult.Response==null)
                                {
                                    throw new Exception("List response is null");
                                }
                                foreach (var item in scriptResult.Response)
                                {
                                    listItem.Choices.Add(new UssdFormListChoice
                                    {
                                        Value = item.value,
                                        Selector = item.selector,
                                        Text = item.text
                                    });
                                }
                            }

                            listItem.Choices.ForEach(Console.WriteLine);
                        }

                        if (ussdFormInput.Required)
                        {
                            while (true)
                            {
                             
                                var resp = Console.ReadLine();

                                if (!string.IsNullOrEmpty(resp))
                                {
                                    ussdForm.BindTo.Value[ussdFormInput.Id] = resp;
                                    break;
                                }
                            }
                        }
                        if (ussdFormInput.Type == "number")
                        {
                            var val = ussdForm.BindTo.Value[ussdFormInput.Id];
                            while (true)
                            {


                                if (IsNumber(val))
                                {
                                    ussdForm.BindTo.Value[ussdFormInput.Id] = val;
                                    break;
                                }
                                Console.WriteLine("INCORRECT INPUT (NUMBERS ONLY)- " + ussdFormInput.Display);
                                val = Console.ReadLine();
                            }
                        }
                        if (ussdFormInput.Type == "money")
                        {
                            var val = ussdForm.BindTo.Value[ussdFormInput.Id];
                            while (true)
                            {

                                decimal money;
                                if (decimal.TryParse(val, out money))
                                {
                                    ussdForm.BindTo.Value[ussdFormInput.Id] = val;
                                    break;
                                }
                                Console.WriteLine("INCORRECT INPUT (MONEY VALUE ONLY)- " + ussdFormInput.Display);
                                val = Console.ReadLine();
                            }
                        }
                        if (ussdFormInput.IsList)
                        {
                            var listItem = _formLists.FirstOrDefault(l => l.Id == ussdFormInput.ListId);

                            if (listItem==null)
                            {
                                throw new Exception($"list with id {ussdFormInput.ListId} not found");
                            }

                           
                            var val = ussdForm.BindTo.Value[ussdFormInput.Id];
                            while (true)
                            {
                                var listChoice = listItem.Choices.FirstOrDefault(s => s.Selector == val);
                                if (listChoice!=null)
                                {
                                    ussdForm.BindTo.Value[ussdFormInput.Id] = listChoice.Value;
                                    break;
                                }
                                Console.WriteLine("INCORRECT INPUT -- " + ussdFormInput.Display);
                                val = Console.ReadLine();
                            }
                        }

                        if (ussdFormInput.MaxLength.HasValue)
                        {
                            var val = ussdForm.BindTo.Value[ussdFormInput.Id];
                            while (true)
                            {


                                if (val.Length <= ussdFormInput.MaxLength.Value)
                                {
                                    ussdForm.BindTo.Value[ussdFormInput.Id] = val;
                                    break;
                                }
                                Console.WriteLine("INCORRECT INPUT (MAX LENGTH SHOULD BE " +
                                                  ussdFormInput.MaxLength.Value + ")- " + ussdFormInput.Display);
                                val = Console.ReadLine();
                            }
                        }

                    }

                    _boundModels[ussdForm.BindTo.Name] = ussdForm.BindTo.Value;

                    if (ussdForm.OnSubmit.NavType== UssdNavigatorTypes.ToScreen)
                    {
                      
                        var screenToNavigate = _screens.FirstOrDefault(s => s.Id == ussdForm.OnSubmit.UssdScreen.Id);

                        if (screenToNavigate == null)
                        {
                            throw new Exception($"screen id \"{ussdForm.OnSubmit.UssdScreen.Id}\" does not exist");
                        }

                        ShowScreen(screenToNavigate);
                    }
                    if (ussdForm.OnSubmit.NavType== UssdNavigatorTypes.ToScript)
                    {
                        
                        var script = _scripts.FirstOrDefault(s => s.Id == ussdForm.OnSubmit.Id);

                        if (script==null)
                        {
                            throw new Exception($"script with id {ussdForm.OnSubmit.Id} not found");
                        }
                        ScriptOutput scriptResult;
                        try
                        {
                            var scriptOptions = ScriptOptions.Default.
                                WithReferences(_scriptAssemblies).WithImports(_scriptImports);

                            var eo = new ExpandoObject();

                            var eoColl = (ICollection<KeyValuePair<string, dynamic>>)eo;

                            foreach (var kvp in ussdForm.BindTo.Value)
                            {
                                eoColl.Add(new KeyValuePair<string, dynamic>(kvp.Key, kvp.Value));
                            }

                            dynamic eoDynamic = eo;
                            var scriptContent = script.Content;
                            scriptContent = scriptContent.Replace(script.BindFrom + ".", "BindFrom.");

                            scriptResult = CSharpScript.EvaluateAsync<ScriptOutput>(scriptContent, scriptOptions,new ScriptInput(new LussdRequestContext(), eoDynamic),typeof(ScriptInput)).Result;


                            if (!string.IsNullOrEmpty(script.BindTo))
                            {
                                //var sr = JsonConvert.SerializeObject(scriptResult.Response);

                                //var dictionaryOfScriptResult =
                                //    JsonConvert.DeserializeObject<Dictionary<string, string>>(sr);

                                _boundModels[script.BindTo] = scriptResult.Response;

                            }
                            if (!string.IsNullOrEmpty(scriptResult.NextScreen))
                            {
                                var nextScreen = _screens.FirstOrDefault(s => s.Id == scriptResult.NextScreen);
                                if (nextScreen==null)
                                {
                                    throw new Exception($"unknown screen \"{scriptResult.NextScreen}\"");
                                }
                                ShowScreen(nextScreen);
                                return;
                            }

                        }
                        catch (Exception ex)
                        {
                            throw new Exception(ex.Message);
                        }

                    }

                    if (ussdForm.OnSubmit.NavType== UssdNavigatorTypes.ToAction)
                    {
                        var actionItem = _actions.FirstOrDefault(a => a.Id ==ussdForm.OnSubmit.Id);
                        if (actionItem == null)
                        {
                            throw new Exception($"undefined action {ussdForm.OnSubmit.Id}");
                        }

                        if (actionItem.Http.Cycle == UssdActionCycle.Once)
                        {
                            if (_boundModels[actionItem.BindTo.Name].Any()) //todo: this might not be so necessary, cos a developer may just want to call an API, no binding
                            {

                            }
                            else
                            {

                                var actionResponse = GrabActionResponse(actionItem).Result;


                                var bdict = new Dictionary<string, dynamic>();

                                foreach (var property in actionResponse.Item2.Properties())
                                {
                                    bdict.Add(property.Name, property.Value);
                                }

                                ussdForm.BindTo.Value = bdict;

                                _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;

                                Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                                if (actionItem.Http.Responses.Any())
                                {
                                    var navigatorLabel =
                                        actionItem.Http.Responses.FirstOrDefault(
                                            r => r.Value == (int)actionResponse.Item1);

                                    if (navigatorLabel != null)
                                    {
                                        //todo: we need to discourage "ussd:action" for now....
                                        //it might cause recursive actions
                                        ShowScreen(
                                            _screens.FirstOrDefault(
                                                s => s.Id == navigatorLabel.Goto.Id));
                                        return;
                                    }
                                }


                            }

                        }
                        else if (actionItem.Http.Cycle == UssdActionCycle.Always)
                        {

                            //should always do fresh bind
                            var actionResponse = GrabActionResponse(actionItem).Result;


                            var bdict = new Dictionary<string, dynamic>();

                            foreach (var property in actionResponse.Item2.Properties())
                            {
                                bdict.Add(property.Name, property.Value);
                            }
                            ussdForm.BindTo.Value = bdict;
                            

                            _boundModels[actionItem.BindTo.Name] = actionResponse.Item2;

                            Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                            if (actionItem.Http.Responses.Any())
                            {
                                var navigatorLabel =
                                    actionItem.Http.Responses.FirstOrDefault(
                                        r => r.Value == (int)actionResponse.Item1);

                                if (navigatorLabel != null)
                                {
                                    //todo: we need to discourage "ussd:action" for now....
                                    ShowScreen(
                                        _screens.FirstOrDefault(
                                            s => s.Id == navigatorLabel.Goto.Id));
                                    return;
                                }
                            }

                        }
                    }
                    
                }

#endregion Form
            }


        }



        private async Task<Tuple<HttpStatusCode, JObject>> GrabActionResponse(UssdAction actionItem)
        {
            var resp = new Tuple<HttpStatusCode, JObject>(HttpStatusCode.Gone, JObject.FromObject(new {Name="fake"}));
            try
            {
                

                var restClient = new HttpClient();
         
                restClient.DefaultRequestHeaders.Clear();

                foreach (var ussdActionHttpHeader in actionItem.Http.Headers)
                {
                    restClient.DefaultRequestHeaders.Add(ussdActionHttpHeader.Name,ussdActionHttpHeader.Value);
                }

                HttpResponseMessage result = null;

                var formItems = _boundModels.FirstOrDefault(b => b.Key == actionItem.Http.Body.BindFrom.Name);

                Dictionary<string, string> f = new Dictionary<string, string>();
                if (formItems.Value!=null)
                {
                    f = formItems.Value;
                }

                if (actionItem.Http.Body.Params.Any())
                {
                    foreach (var ussdActionHttpBodyParam in actionItem.Http.Body.Params)
                    {
                        f.Add(ussdActionHttpBodyParam.Name, ussdActionHttpBodyParam.Value);
                    }
                }


                HttpContent content = new StringContent(JsonConvert.SerializeObject(f));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); //todo: document that by default content-type is application/json

                switch (actionItem.Http.Method)
                {
                    case UssdActionHttpMethod.Post:
                        result = await restClient.PostAsync(new Uri(actionItem.Http.Url), content);
                        break;
                    case UssdActionHttpMethod.Put:
                        result = await restClient.PutAsync(new Uri(actionItem.Http.Url), content);
                      
                        break;
                    case UssdActionHttpMethod.Get:
                        result = await restClient.GetAsync(new Uri(actionItem.Http.Url));
                        break;
                    case UssdActionHttpMethod.Delete:
                        result = await restClient.DeleteAsync(new Uri(actionItem.Http.Url));
                      
                        break;
                    case UssdActionHttpMethod.Patch:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (result==null)
                {
                    return resp;
                }

                
                var serverResp = await result.Content.ReadAsStringAsync();
                
                if (result.IsSuccessStatusCode)
                {
                    resp = new Tuple<HttpStatusCode, JObject>(HttpStatusCode.OK, JObject.Parse(serverResp));
                }
                else
                {
                    resp = new Tuple<HttpStatusCode, JObject>(result.StatusCode, JObject.Parse(serverResp));
                }
            }
            catch (Exception exception)
            {
                resp = new Tuple<HttpStatusCode, JObject>(HttpStatusCode.ExpectationFailed, JObject.FromObject(new {exception.Message}));
            }

            return resp;
        }

        private bool IsNumber(string val)
        {
            var chararr = val.ToCharArray();

            foreach (var a in chararr)
            {
                if (!char.IsNumber(a))
                {
                    return false;
                }
            }

            return true;
        }

   

    }

    public enum UssdNavigatorTypes
    {
        ToScreen,
        ToAction,
        ToScript
    }


    public enum UssdActionCycle
    {
        Once,
        Always
    }

    public enum UssdActionHttpMethod
    {
        Post,
        Put,
        Get,
        Delete,
        Patch
    }
}