using System;
using System.Runtime.Serialization;

namespace SmartGrid.Common
{
    [DataContract]
    public class MetaHeader
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
    }
}
