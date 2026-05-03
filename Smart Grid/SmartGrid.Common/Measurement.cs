using System;
using System.Collections.Generic;

namespace SmartGrid.Common
{
    public class Measurement
    {
        public DateTime Timestamp { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double PowerUsage { get; set; }
        public double Frequency { get; set; }
        public FaultType FaultIndicator { get; set; }
        public List<double> FftValues { get; set; } = new List<double>();
    }
}
