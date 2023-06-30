using System.ServiceProcess;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace CardReaderWindowsService
{
    partial class SelfHostService : ServiceBase
    {
        public SelfHostService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var config = new HttpSelfHostConfiguration("http://localhost:8996");
            config.MessageHandlers.Add(new CustomHeaderHandler());

            config.Routes.MapHttpRoute(
               name: "API",
               routeTemplate: "{controller}/{action}/{value}",
               defaults: new { value = RouteParameter.Optional }
           );

            HttpSelfHostServer server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }
    }
}
