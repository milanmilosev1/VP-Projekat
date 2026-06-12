using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SmartGrid.Common
{
    [DataContract]
    public class AnalyticsRecord
    {
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public int SampleIndex { get; set; }

        [DataMember]
        public string Direction { get; set; }

        [DataMember]
        public double ActualValue { get; set; }

        [DataMember]
        public double ReferenceValue { get; set; }

        [DataMember]
        public double Delta { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    [DataContract]
    public class AnalyticsReport
    {
        public AnalyticsReport()
        {
            Records = new List<AnalyticsRecord>();
        }

        [DataMember]
        public int ProcessedSamples { get; set; }

        [DataMember]
        public int AcceptedSamples { get; set; }

        [DataMember]
        public int RejectedSamples { get; set; }

        [DataMember]
        public double AverageVoltage { get; set; }

        [DataMember]
        public double AverageCurrent { get; set; }

        [DataMember]
        public double AveragePowerUsage { get; set; }

        [DataMember]
        public double AverageFrequency { get; set; }
        
        [DataMember]
        public double MaxVoltage { get; set; }

        [DataMember]
        public double MaxCurrent { get; set; }

        [DataMember]
        public List<AnalyticsRecord> Records { get; set; }
    }
}
