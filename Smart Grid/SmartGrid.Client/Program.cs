using SmartGrid.Common;
using System;
using System.ServiceModel;

namespace SmartGrid.Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var loader = new CSVLoader("smart_grid_dataset.csv", "invalid.log"))
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
