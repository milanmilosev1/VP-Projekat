using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartGrid.Common;

namespace SmartGrid.Client
{
    public class CSVLoader : IDisposable
    {
        private bool disposed = false;
        private readonly StreamReader _reader;
        private readonly StreamWriter _logger;

        public CSVLoader(string filePath, string logPath)
        {
            _reader = new StreamReader(filePath);
            _logger = new StreamWriter(logPath, append: false);
        }

        public List<Measurement> LoadFirst100()
        {
            List<Measurement> measurements = new List<Measurement>();
            var culture = CultureInfo.InvariantCulture;
            string line;
            bool headerSkipped = false;
            
            while((line = _reader.ReadLine()) != null) 
            {
                if(!headerSkipped) 
                {
                    headerSkipped = true;
                    continue;
                }
                try
                {
                    var parts = line.Split(',');
                    if (parts.Length < 6)
                    {
                        _logger.WriteLine($"Nedovoljan broj kolona.");
                        continue;
                    }

                    Measurement m = new Measurement
                    {
                        Timestamp = DateTime.Parse(parts[0], culture),
                        Voltage = double.Parse(parts[1], culture),
                        Current = double.Parse(parts[2], culture),
                        PowerUsage = double.Parse(parts[3], culture),
                        Frequency = double.Parse(parts[4], culture),
                        FaultIndicator = (FaultType)Enum.Parse(typeof(FaultType), parts[5]),
                        FftValues = new List<double>()
                    };

                    for (int i = 6; i < parts.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(parts[i]))
                        {
                            m.FftValues.Add(double.Parse(parts[i], culture));
                        }
                    }
                    measurements.Add(m);
                    if (measurements.Count >= 100)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteLine($"Nije validan red: {line}. Greška: {ex.Message}");
                }
            }

            return measurements;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _logger?.Dispose();
                    _reader?.Dispose();
                    Console.WriteLine("CSVLoader resorces (reader, logger) succesfully closed and freed.");
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
