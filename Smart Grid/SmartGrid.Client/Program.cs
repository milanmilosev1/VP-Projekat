using SmartGrid.Common;
using System;
using System.Configuration;
using System.IO;
using System.ServiceModel;

namespace SmartGrid.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string databaseUrl = ConfigurationManager.AppSettings["DatabaseURL"];
            string invalidSamplesUrl = ConfigurationManager.AppSettings["InvalidSamplesRecordURL"];
            string loggerUrl = ConfigurationManager.AppSettings["LoggerURL"];

            using (var loader = new CSVLoader(databaseUrl, loggerUrl))
            {
                try
                {
                    var data = loader.LoadFirst100();

                    using (var factory = new ChannelFactory<ISessionControl>("SessionControlEndpoint"))
                    {
                        var proxy = factory.CreateChannel();

                        var meta = new MetaHeader
                        {
                            Timestamp = DateTime.Now,
                            Voltage = 230,
                            Current = 10,
                            PowerUsage = 2300,
                            Frequency = 50,
                            FaultIndicator = FaultType.NO_FAULT
                        };

                        var startResponse = proxy.StartSession(meta);
                        Console.WriteLine($"StartSession: {startResponse.Status} - {startResponse.Message}");

                        foreach (var item in data)
                        {
                            var pushResponse = proxy.PushSample(item);
                            Console.WriteLine($"PushSample: {pushResponse.Status} - {pushResponse.Message}");
                            if(pushResponse.Status != Status.ACK)
                            {
                                using (var stream = new StreamWriter(invalidSamplesUrl, true))
                                {
                                    stream.WriteLine($"Status: {pushResponse.Status}");
                                    stream.WriteLine($"Progress: {pushResponse.Progress}");
                                    stream.WriteLine($"Message: {pushResponse.Message}");
                                    stream.WriteLine("--------------------------------");
                                }
                            }
                        }

                        var endResponse = proxy.EndSession();
                        Console.WriteLine($"EndSession: {endResponse.Status} - {endResponse.Message}");
                    }
                }
                catch (FaultException<ValidationFault> vf)
                {
                    Console.WriteLine($"Validation Fault: {vf.Detail.Reason}");
                }
                catch (FaultException<DataFormatFault> df)
                {
                    Console.WriteLine($"Data Format Fault: {df.Detail.Reason}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }
            } 

            Console.ReadKey();
        }
    }
}
