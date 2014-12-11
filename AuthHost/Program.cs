using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var hostUrl = "http://localhost:" + ConfigurationManager.AppSettings["Port"];
            HostConfiguration hostConfigs = new HostConfiguration();
            hostConfigs.UrlReservations.CreateAutomatically = true;
            var nancyHost = new Nancy.Hosting.Self.NancyHost(hostConfigs, new Uri( hostUrl ) );
            nancyHost.Start();

            Console.WriteLine("Nancy host listening on " + hostUrl);

            Console.ReadLine();
            nancyHost.Stop();
        }
    }
}
