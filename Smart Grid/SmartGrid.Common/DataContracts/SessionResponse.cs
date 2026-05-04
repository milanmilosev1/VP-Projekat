using System.Runtime.Serialization;

namespace SmartGrid.Common
{
    public enum Status
    {
        ACK = 0,
        NAK = 1
    }

    public enum Progress
    {
        IN_PROGRESS = 0,
        COMPLETED = 1
    }

    [DataContract]
    public class SessionResponse
    {
        [DataMember] 
        public Status Status { get; set; }
        
        [DataMember]
        public Progress Progress { get; set; }
        
        [DataMember] 
        public string Message { get; set; }
    }
}
