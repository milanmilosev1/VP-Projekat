using System.Runtime.Serialization;

namespace SmartGrid.Common
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Reason { get; set; }
    }

    [DataContract]
    public class DataFormatFault 
    {
        [DataMember]
        public string Reason { get; set; }
    }
}
