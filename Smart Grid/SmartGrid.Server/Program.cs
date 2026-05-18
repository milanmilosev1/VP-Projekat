using System;
using System.ServiceModel;

namespace SmartGrid.Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(SessionControl)))
            {
                host.Open();
                Console.WriteLine("Server started at net.tcp://localhost:8080/SessionControl");
                Console.WriteLine("Press [ENTER] to stop...");
                Console.ReadLine();
            }
        }
    }
}
