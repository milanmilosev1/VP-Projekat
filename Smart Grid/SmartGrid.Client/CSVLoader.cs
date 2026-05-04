using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Messaging;
using SmartGrid.Common;

namespace SmartGrid.Client
{
    public class CSVLoader
    {
        public static List<Measurement> LoadFirst100(string filePath,string logPath)
        {
            List<Measurement> measurements = new List<Measurement>();
            var culture = CultureInfo.InvariantCulture;

            using(StreamReader reader = new StreamReader(filePath)) 
            using(StreamWriter log = new StreamWriter(logPath))
            {
                string line;
                bool headerSkipped = false;
                while((line = reader.ReadLine()) != null) 
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

                            log.WriteLine($"Nedovoljan broj kolona.");
                            continue;
                        }

                        Measurement m = new Measurement();
                        
                        m.Timestamp = DateTime.Parse(parts[0], culture);    
                        m.Voltage = double.Parse(parts[1], culture);
                        m.Current = double.Parse(parts[2], culture);
                        m.PowerUsage = double.Parse(parts[3], culture);
                        m.Frequency = double.Parse(parts[4], culture);
                        m.FaultIndicator = (FaultType)Enum.Parse(typeof(FaultType), parts[5]);
                        m.FftValues = new List<double>();
                        
                        for(int i = 6; i < parts.Length; i++) 
                        {
                            if(!string.IsNullOrWhiteSpace(parts[i])) 
                            {
                                m.FftValues.Add(double.Parse(parts[i], culture));
                            }
                        }
                        measurements.Add(m);
                        if(measurements.Count > 100) 
                        {
                            break;
                        }
                    } 
                    catch(Exception ex) 
                    {
                        log.WriteLine($"Nije validan red: {line}. Greška: {ex.Message}");
                    }
                }
            }
            return measurements;

        }


    }
}
