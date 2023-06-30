using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace CardReaderWindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;

            SelfHostService _selfHostService = new SelfHostService();

            _selfHostService.ServiceName = "CardReaderWindowsService";
             
            ServicesToRun = new ServiceBase[] 
            { 
                _selfHostService
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
