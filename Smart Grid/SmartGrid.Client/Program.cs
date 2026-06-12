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
                            if(pushResponse.Status != Status.ACK)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"PushSample: {pushResponse.Status} - {pushResponse.Message}");
                                using (var stream = new StreamWriter(invalidSamplesUrl, true))
                                {
                                    stream.WriteLine($"Status: {pushResponse.Status}");
                                    stream.WriteLine($"Progress: {pushResponse.Progress}");
                                    stream.WriteLine($"Message: {pushResponse.Message}");
                                    stream.WriteLine("--------------------------------");
                                }
                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"PushSample: {pushResponse.Status} - {pushResponse.Message}");
                                Console.ResetColor();
                            }
                        }

                        var endResponse = proxy.EndSession();
                        Console.WriteLine($"EndSession: {endResponse.Status} - {endResponse.Message}");

                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("Do you want to see analytics? (y/n): ");
                        Console.ResetColor();
                        string analyticsAnswer = Console.ReadLine();
                        if (string.Equals(analyticsAnswer, "y", StringComparison.OrdinalIgnoreCase))
                        {
                            var analyticsReport = proxy.GetAnalyticsReport();
                            Console.Clear();
                            PrintAnalyticsReport(analyticsReport);
                        }
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

        private static void PrintAnalyticsReport(AnalyticsReport report)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\n\nSMART GRID ANALYTICS REPORT");
            Console.WriteLine("========================================");
            Console.WriteLine($"Processed samples: {report.ProcessedSamples}");
            Console.WriteLine($"Accepted samples:  {report.AcceptedSamples}");
            Console.WriteLine($"Rejected samples:  {report.RejectedSamples}");
            Console.WriteLine($"Average voltage:   {report.AverageVoltage:F4}");
            Console.WriteLine($"Average current:   {report.AverageCurrent:F4}");
            Console.WriteLine($"Average power:     {report.AveragePowerUsage:F4}");
            Console.WriteLine($"Average frequency: {report.AverageFrequency:F4}");
            Console.WriteLine($"Max voltage:       {report.MaxVoltage:F4}");
            Console.WriteLine($"Max current:       {report.MaxCurrent:F4}");
            Console.ResetColor();
            Console.WriteLine();

            if (report.Records == null || report.Records.Count == 0)
            {
                Console.WriteLine("No analytics warnings detected for this session.");
                return;
            }

            PrintRecordsByType(report, "VoltageSpike", "Voltage spikes");
            PrintRecordsByType(report, "CurrentSpike", "Current spikes");
            PrintRecordsByType(report, "OutOfBandWarning", "Out-of-band current warnings");
        }

        private static void PrintRecordsByType(AnalyticsReport report, string type, string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(title);
            Console.WriteLine(new string('=', Console.WindowWidth));

            bool hasRecords = false;
            foreach (var record in report.Records)
            {
                if (record.Type != type)
                    continue;

                hasRecords = true;
                Console.WriteLine($"Sample #{record.SampleIndex}: {record.Message}");
                Console.WriteLine($"  Actual={record.ActualValue:F4}; Reference={record.ReferenceValue:F4}; Delta={record.Delta:F4}; Direction={record.Direction}");
                Console.WriteLine("--------------------------------------------------------------------------------------------------");
            }

            if (!hasRecords)
            {
                Console.WriteLine("No records.");
            }

            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
