using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
           
            _doc= new XmlDocument();
            
            _doc.Load(Filename);
        }


        public List<UssdScreen> GetScreens()
        {
            return _screens;
        }

        public Dictionary<string, Dictionary<string, string>> GetBoundModels()
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

        List<UssdScreen> _screens;
        List<UssdAction> _actions;
        List<UssdFormList> _formLists;
        List<UssdForm> _forms;
        List<UssdMenu> _menus;
        Dictionary<string,Dictionary<string,string>> _boundModels;
        public void Parse(bool simulator=true)
        {
            var rootNode = _doc.SelectNodes("ussd");
           
            _screens = new List<UssdScreen>();
            _actions = new List<UssdAction>();
            _formLists = new List<UssdFormList>();
            _forms = new List<UssdForm>();
            _menus = new List<UssdMenu>();
            _boundModels = new  Dictionary<string, Dictionary<string, string>>();
            foreach (XmlNode node in rootNode)
            {
                foreach (XmlNode screen in node.ChildNodes)
                {

                    if (screen.Attributes["id"]==null)
                    {
                        throw new Exception("screen id is required");
                    }
                    var screenId = screen.Attributes["id"].InnerText;
                    Console.WriteLine("gotten screen {0}",screenId);
                    if (_screens.Any(s=>s.Id==screenId))
                    {
                        throw new Exception("screen id already exists at {0}");
                    }

                    var ussdScreen = new UssdScreen
                                     {
                                         Id = screenId
                                     };

                    foreach (XmlNode screenItem in screen.ChildNodes)
                    {
                        if (screenItem.Name.Equals("menu"))
                        {
                            var menuNodes = screenItem.ChildNodes;

                            var ussdMenu = new UssdMenu();
                            if (screenItem.Attributes.Count>0)
                            {
                                if (screenItem.Attributes["bindfrom"]!=null)
                                {
                                    //todo: always check if there's a corresponding bindto in another screen
                                    ussdMenu.BindFrom = new UssdBoundModel
                                                        {
                                                            Name = screenItem.Attributes["bindfrom"].InnerText
                                                        };
                             
                                }
                                if (screenItem.Attributes["bindaction"] != null)
                                {
                             
                                    ussdMenu.BindAction = new UssdBoundAction
                                    {
                                        Name = screenItem.Attributes["bindaction"].InnerText
                                    };

                                }
                              
                            }

                            foreach (XmlNode menuNode in menuNodes)
                            {

                                if (menuNode.Name=="title")
                                {
                                    ussdMenu.Title = menuNode.InnerText.ToUssdString(simulator);
                                }

                                if (menuNode.Name=="option")
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
                            Console.WriteLine("added menu {0}",ussdMenu.Title);
                        }
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
                                    _boundModels[screenItem.Attributes["bindto"].InnerText] = new Dictionary<string, string>();
                                                     
                                }
                                if (screenItem.Attributes["onsubmit"] != null)
                                {
                                    ussdForm.OnSubmit = new UssdNavigator(screenItem.Attributes["onsubmit"].InnerText);
                                    
                                } 
                                else
                                {
                                    throw new Exception("form attribute bindto is required");
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
                                                         MaxLength = formNode.Attributes["maxlength"]==null?(int?)null:int.Parse(formNode.Attributes["maxlength"].InnerText)
                                                     };

                                    if (ussdFormInput.Type=="list")
                                    {
                                        ussdFormInput.IsList = true;
                                        ussdFormInput.ListId = formNode.Attributes["listid"].InnerText;
                                    }

                                    ussdForm.Inputs.Add(ussdFormInput);


                                }
                                if (formNode.Name=="list")
                                {
                                    var formListNodes = formNode.ChildNodes;

                                    var ussdFormList = new UssdFormList {Id = formNode.Attributes["id"].InnerText};
                                    foreach (XmlNode formListNode in formListNodes)
                                    {
                                        ussdFormList.Choices.Add(new UssdFormListChoice
                                        {
                                            Text = formListNode.InnerText,
                                            Selector=formListNode.Attributes["selector"].InnerText,
                                            Value=formListNode.Attributes["value"].InnerText
                                        });
                                    }
                                    _formLists.Add(ussdFormList);

                                }
                            }


                            ussdScreen.UssdItems.Add(ussdForm);
                            _forms.Add(ussdForm);

                            Console.WriteLine("added form {0}", ussdForm.Title);
                        }
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

                                    ussdText.BindAction = new UssdBoundAction
                                    {
                                        Name = screenItem.Attributes["bindaction"].InnerText
                                    };

                                }

                            }
                            ussdScreen.UssdItems.Add(ussdText);
                        }


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
                                    _boundModels[screenItem.Attributes["bindto"].InnerText] = new Dictionary<string, string>();

                                }
                                else
                                {
                                    throw new Exception("action attribute bindto is required");
                                }

                                if (screenItem.Attributes["cycle"] != null)
                                {
                                    UssdActionCycle cycle;
                                    if(!Enum.TryParse(screenItem.Attributes["cycle"].InnerText,true,out cycle))
                                    {
                                        throw new Exception(String.Format("action attribute cycle has an unknown value. Allowed values are: {0} or {1}",UssdActionCycle.Always,UssdActionCycle.Once));
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


                                if (xmlNode.Attributes.Count>0)
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
                                        if (!Enum.TryParse(xmlNode.Attributes["method"].InnerText,true,out httpMethod))
                                        {
                                            throw new Exception("http attribute method is invalid");
                                        }
                                        
                                        ussdAction.Http.Method = httpMethod;
                                    }
                                    else
                                    {
                                        throw new Exception("http attribute url is required");
                                    }
                                }

                          
                                if (xmlNode.Name == "http")
                                {
                                    var httpNodes = xmlNode.ChildNodes;

                                    foreach (XmlNode httpNode in httpNodes)
                                    {
                                        if (httpNode.Name=="header")
                                        {
                                            var headers = httpNode.ChildNodes;

                                            foreach (XmlNode header in headers)
                                            {
                                                if (header.Name=="add")
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
                        
                    }

                    
                    _screens.Add(ussdScreen);

                    Console.WriteLine("{0} screens",_screens.Count);
                }
            }

            if (_screens.Count == 0)
            {
                throw new Exception("no screens");
            }
        }

        public void RunSimulator()
        {
            var firstScreen = _screens[0];

            ShowScreen(firstScreen);
        }

        private void ShowScreen(UssdScreen firstScreen)
        {
            foreach (var ussdItem in firstScreen.UssdItems)
            {
                if (ussdItem.GetType() == typeof (UssdMenu))
                {
                    var ussdMenu = (UssdMenu) ussdItem;

                    var model = _boundModels.Keys.FirstOrDefault(k => k == ussdMenu.BindFrom.Name);

                    if (model == null)
                    {
                        Console.WriteLine(ussdMenu.Title.Trim());
                    }
                    else
                    {

                        if (!string.IsNullOrEmpty(ussdMenu.BindAction.Name))
                        {
                            UssdAction actionItem = _actions.FirstOrDefault(a => a.Id == ussdMenu.BindAction.Name);

                            //var actionItem =
                            //    (UssdAction)
                            //        firstScreen.UssdItems.FirstOrDefault(a => a.GetType() == typeof (UssdAction));

                            if (actionItem == null)
                            {
                                throw new Exception(String.Format("undefined action {0}", ussdMenu.BindAction.Name));
                            }
                            
                            if (actionItem.Http.Cycle == UssdActionCycle.Once)
                            {
                                if (_boundModels[actionItem.BindTo.Name].Any()) //todo: this might not be so necessary, cos a developer may just want to call an API, no binding
                                {

                                }
                                else
                                {

                                    var actionResponse = GrabActionResponse(actionItem).Result;


                                    var bdict = new Dictionary<string, string>();

                                    foreach (var property in actionResponse.Item2.Properties())
                                    {
                                        bdict.Add(property.Name, property.Value.ToString());
                                    }

                                    ussdMenu.BindAction.Value = bdict;

                                    _boundModels[actionItem.BindTo.Name] = bdict;

                                    Console.WriteLine("HTTP response {0}", actionResponse.Item1);

                                    if (actionItem.Http.Responses.Any())
                                    {
                                        var navigatorLabel =
                                            actionItem.Http.Responses.FirstOrDefault(
                                                r => r.Value == (int) actionResponse.Item1);

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


                                    var bdict = new Dictionary<string, string>();

                                    foreach (var property in actionResponse.Item2.Properties())
                                    {
                                        bdict.Add(property.Name, property.Value.ToString());
                                    }

                                    ussdMenu.BindAction.Value = bdict;

                                    _boundModels[actionItem.BindTo.Name] = bdict;

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

                        var title = ussdMenu.Title.Trim();
                        var boundModel = _boundModels[ussdMenu.BindFrom.Name];


                        foreach (var item in boundModel.Keys)
                        {
                            if (ussdMenu.Title.Trim().Contains("." + item))
                            {
                                title = title.Replace("." + item + "}", boundModel[item]);
                            }
                        }
                        if (boundModel.Any())
                        {
                            title = title.Replace("{" + ussdMenu.BindFrom.Name, "");
                        }
                        Console.WriteLine(title);
                    }


                    foreach (var ussdMenuOption in ussdMenu.Options)
                    {
                        Console.WriteLine(ussdMenuOption.Text);
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

                    if (menuOption.OnSelect.NavType==UssdNavigatorTypes.ToAction)
                    {
                 
                        var actionItem = _actions.FirstOrDefault(a => a.Id == menuOption.OnSelect.Id);
                        if (actionItem == null)
                        {
                            throw new Exception(String.Format("undefined action {0}", ussdMenu.BindAction.Name));
                        }

                        if (actionItem.Http.Cycle == UssdActionCycle.Once)
                        {
                            if (_boundModels[actionItem.BindTo.Name].Any()) //todo: this might not be so necessary, cos a developer may just want to call an API, no binding
                            {

                            }
                            else
                            {

                                var actionResponse = GrabActionResponse(actionItem).Result;


                                var bdict = new Dictionary<string, string>();

                                foreach (var property in actionResponse.Item2.Properties())
                                {
                                    bdict.Add(property.Name, property.Value.ToString());
                                }

                                ussdMenu.BindAction.Value = bdict;

                                _boundModels[actionItem.BindTo.Name] = bdict;

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


                                var bdict = new Dictionary<string, string>();

                                foreach (var property in actionResponse.Item2.Properties())
                                {
                                    bdict.Add(property.Name, property.Value.ToString());
                                }

                                ussdMenu.BindAction.Value = bdict;

                                _boundModels[actionItem.BindTo.Name] = bdict;

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
                    Console.WriteLine("navigating to {0}", menuOption.OnSelect.UssdScreen.Id);
                    ShowScreen(_screens.FirstOrDefault(s => s.Id == menuOption.OnSelect.UssdScreen.Id));
                }

                if (ussdItem.GetType() == typeof (UssdText))
                {
                    var ussdText = (UssdText) ussdItem;


                    var model = _boundModels.Keys.FirstOrDefault(k => k == ussdText.BindFrom.Name);

                    if (model == null)
                    {
                        Console.WriteLine(ussdText.Text.Trim());
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(ussdText.BindAction.Name))
                        {
                            var actionItem = _actions.FirstOrDefault(a => a.Id == ussdText.BindAction.Name);

                            //var actionItem =
                            //    (UssdAction)
                            //        firstScreen.UssdItems.FirstOrDefault(a => a.GetType() == typeof (UssdAction));

                            if (actionItem == null)
                            {
                                throw new Exception(String.Format("undefined action {0}", ussdText.BindAction.Name));
                            }

                            if (actionItem.Http.Cycle == UssdActionCycle.Once)
                            {
                                if (_boundModels[actionItem.BindTo.Name].Any())
                                {

                                }
                                else
                                {

                                    var actionResponse = GrabActionResponse(actionItem).Result;

                                    var bdict = new Dictionary<string, string>();

                                    foreach (var property in actionResponse.Item2.Properties())
                                    {
                                        bdict.Add(property.Name, property.Value.ToString());
                                    }

                                    ussdText.BindAction.Value = bdict;

                                    _boundModels[actionItem.BindTo.Name] = bdict;



                                    Console.WriteLine("HTTP response {0}", actionResponse.Item1);
                                    //no replacement occured....HTTP failed.
                                    if (actionItem.Http.Responses.Any())
                                    {
                                        var navigatorLabel =
                                            actionItem.Http.Responses.FirstOrDefault(
                                                r => r.Value == (int) actionResponse.Item1);

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


                                    var bdict = new Dictionary<string, string>();

                                    foreach (var property in actionResponse.Item2.Properties())
                                    {
                                        bdict.Add(property.Name, property.Value.ToString());
                                    }

                                    ussdText.BindAction.Value = bdict;

                                    _boundModels[actionItem.BindTo.Name] = bdict;

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

                        var title = ussdText.Text.Trim();
                        var boundModel = _boundModels[ussdText.BindFrom.Name];
                        foreach (var item in boundModel.Keys)
                        {
                            if (ussdText.Text.Trim().Contains("." + item))
                            {
                                title = title.Replace("." + item + "}", boundModel[item]);
                                //break;
                            }
                        }
                        Console.WriteLine(title.Replace("{" + ussdText.BindFrom.Name, ""));
                    }


                    Console.WriteLine("done :)");
                }

                if (ussdItem.GetType() == typeof (UssdForm))
                {
                    var ussdForm = (UssdForm) ussdItem;

                    Console.WriteLine(ussdForm.Title.Trim());

                    foreach (var ussdFormInput in ussdForm.Inputs)
                    {


                        Console.WriteLine(ussdFormInput.Display);

                        if (ussdFormInput.IsList)
                        {
                            var listItem = _formLists.FirstOrDefault(l => l.Id == ussdFormInput.ListId);

                            if (listItem == null)
                            {
                                throw new Exception(string.Format("list with id {0} not found", ussdFormInput.ListId));
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
                                throw new Exception(string.Format("list with id {0} not found",ussdFormInput.ListId));
                            }

                            //listItem.Choices.ForEach(Console.WriteLine);

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
                    ShowScreen(_screens.FirstOrDefault(s => s.Id == ussdForm.OnSubmit.UssdScreen.Id));
                }
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


                //result.EnsureSuccessStatusCode();

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

        /*StringBuilder _codeBuilder;
        public void GenerateUssdFile(string controllerName="")
        {
            var rawName = string.IsNullOrEmpty(controllerName)
                ? Filename.Replace(".xml", string.Empty)
                : controllerName;

            var controllerFilename = rawName + "Controller";

            _codeBuilder = new StringBuilder();

            _codeBuilder.Append("using System;");
            _codeBuilder.Append("using System.Threading.Tasks;");
            _codeBuilder.Append("using Smsgh.UssdFramework;");
            _codeBuilder.Append(string.Format("namespace {0}Library.UssdControllers", controllerFilename));
            _codeBuilder.Append("{");


            _codeBuilder.Append(string.Format("public class {0} : UssdController",controllerFilename));
            _codeBuilder.Append("{");
            _codeBuilder.Append("\r\n");
            _codeBuilder.Append("\r\n");
            _codeBuilder.Append("\t");



            var firstScreen = _screens.FirstOrDefault();
            foreach (var ussdScreen in _screens)
            {
                GenerateScreenCode(ussdScreen);
                
            }
            _codeBuilder.Append("\r\n");
            _codeBuilder.Append("\t");
            _codeBuilder.Append("}"); //end of class
            _codeBuilder.Append("}"); //end of controller


            var handler = BuildDynamicUssdHandler(rawName,"testroute",firstScreen.Id);


            File.WriteAllText(controllerFilename + ".cs", _codeBuilder.ToString());
            File.WriteAllText(string.Format("Main{0}Controller", rawName) + ".cs", handler);

            BuildDll(string.Format("Main{0}Controller", rawName), string.Format("Main{0}Controller.cs", rawName),
                controllerFilename + ".cs");


        }

        private string BuildDynamicUssdHandler(string controller,string routePrefix,string startAction)
        {
            var handler = new StringBuilder();

            handler.Append("using System.Threading.Tasks;");
            handler.Append("using System.Web.Http;");
            handler.Append("using Smsgh.UssdFramework;");
            handler.Append("using Smsgh.UssdFramework.Stores;");
            handler.Append(string.Format("namespace {0}UssdHandler", controller));
            handler.Append("{");

            handler.Append(string.Format("[RoutePrefix(\"{0}\"),AllowAnonymous]",routePrefix));
            handler.Append(string.Format("public class Main{0}Controller:ApiController", controller));
            handler.Append("{");
            handler.Append("[Route,HttpPost]");
            handler.Append("public async Task<IHttpActionResult> Index(UssdRequest request)");
            handler.Append("{");
            handler.Append(string.Format(" return Ok(await Ussd.Process(new RedisStore(), request, \"{0}\", \"{1}\"));",controller,startAction));
            handler.Append("}");
            handler.Append("}");
            handler.Append("}");

            return handler.ToString();
        }

        private void GenerateScreenCode(UssdScreen firstScreen)
        {
            _codeBuilder.Append(string.Format("public async Task<UssdResponse> {0}()",firstScreen.Id));
            _codeBuilder.Append("{");
            _codeBuilder.Append("\r\n");
            _codeBuilder.Append("\r\n");
            _codeBuilder.Append("\t");
            _codeBuilder.Append("\t");
            foreach (var ussdItem in firstScreen.UssdItems)
            {
                if (ussdItem.GetType() == typeof(UssdMenu))
                {
                    var ussdMenu = (UssdMenu)ussdItem;
                    _codeBuilder.Append(ussdMenu.ToCode());
                }

                if (ussdItem.GetType() == typeof(UssdText))
                {
                    var ussdText = (UssdText)ussdItem;

                    _codeBuilder.Append(ussdText.ToCode(_boundModels));
                }

                if (ussdItem.GetType() == typeof(UssdForm))
                {
                    var ussdForm = (UssdForm)ussdItem;

                    _codeBuilder.Append(ussdForm.ToCode(_formLists));

               }
            }

            _codeBuilder.Append("}");
            _codeBuilder.Append("\r\n");
            _codeBuilder.Append("\r\n");
            _codeBuilder.Append("\t");
        }

        private void BuildDll(string dllname,string handlerFilename, string controllerFilename)
        {
            IDictionary<string, string> compParams =
     new Dictionary<string, string>() { { "CompilerVersion", "v4.0" } };
            CodeDomProvider codeProvider = CodeDomProvider.CreateProvider("CSharp", compParams);
            
            string outputDll = dllname + ".dll";

            var parameters = new CompilerParameters();
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = outputDll;
            
            parameters.ReferencedAssemblies.Add(@"System.Net.Http.dll");
            parameters.ReferencedAssemblies.Add(@"System.Net.Http.WebRequest.dll");
            parameters.ReferencedAssemblies.Add(@"System.Net.Http.Formatting.dll");
            parameters.ReferencedAssemblies.Add(@"System.Web.Http.dll");
            parameters.ReferencedAssemblies.Add(@"Smsgh.UssdFramework.dll");

            CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, controllerFilename, handlerFilename);
            if (results.Errors.Count > 0)
            {
                Console.WriteLine("Build Failed");
                foreach (CompilerError compErr in results.Errors)
                {
                    Console.WriteLine(
                    "Line number " + compErr.Line +
                    ", Error Number: " + compErr.ErrorNumber +
                    ", '" + compErr.ErrorText + ";" +
                    Environment.NewLine + Environment.NewLine);
                }
            }
            else
            {
                Console.WriteLine("Build Succeeded");
                //return Assembly.LoadFrom(outputDll);
            }
        }
        */

    }

    public enum UssdNavigatorTypes
    {
        ToScreen,
        ToAction

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