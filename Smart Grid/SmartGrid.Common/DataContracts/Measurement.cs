using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartGrid.Common
{
    [DataContract(Name = "SmartGridSample")]
    public class Measurement
    {
        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public double Voltage { get; set; }

        [DataMember]
        public double Current { get; set; }

        [DataMember]
        public double PowerUsage { get; set; }

        [DataMember]
        public FaultType FaultIndicator { get; set; }

        [DataMember]
        public double Frequency { get; set; }

        [DataMember]
        public List<double> FftValues { get; set; }

        public override string ToString()
        {
            return $"Timestamp: {Timestamp} | Voltage: {Voltage} | Current: {Current} | PowerUsage: {PowerUsage} | FaultIndicator {FaultIndicator} | Frequency: {Frequency}";
        }
    }
}
