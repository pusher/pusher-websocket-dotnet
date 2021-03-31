using System;
using System.Configuration;
using Nancy.Hosting.Self;

namespace AuthHost
{
    class Program
    {
        static void Main()
        {
            var hostUrl = "http://localhost:" + ConfigurationManager.AppSettings["Port"];
            HostConfiguration hostConfigs = new HostConfiguration();
            hostConfigs.UrlReservations.CreateAutomatically = true;
            var nancyHost = new NancyHost(hostConfigs, new Uri( hostUrl ) );
            nancyHost.Start();

            Console.WriteLine("Nancy host listening on " + hostUrl);

            Console.ReadLine();
            nancyHost.Stop();
        }
    }
}