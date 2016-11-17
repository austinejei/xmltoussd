using System;
using System.ComponentModel.Design;
using System.Text;
using System.Threading.Tasks;
using XmlToUssdCompiler;

namespace xmltoussd.app
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            if (args.Length==0)
            {
                throw new ArgumentNullException(nameof(args),"no file to parse");
            }
            //var ussd = new UssdXml("myapp.xml");
            var ussd = new UssdXml(args[0]);

            ussd.Parse();

            Console.Clear();

            //ussd.GenerateUssdControllerDll("SimpleProgram");
           // ussd.GenerateUssdControllerDll();
            ussd.RunSimulator();

            Console.ReadLine();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("============error occured=================");
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.ReadLine();
        }

        
    }
}
