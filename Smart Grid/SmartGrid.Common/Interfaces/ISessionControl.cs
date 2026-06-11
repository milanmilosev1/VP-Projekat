using System.ServiceModel;

namespace SmartGrid.Common
{
    [ServiceContract]
    public interface ISessionControl
    {

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        SessionResponse StartSession(MetaHeader metaHeader);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        SessionResponse PushSample(Measurement sample);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        SessionResponse EndSession();

        [OperationContract]
        AnalyticsReport GetAnalyticsReport();
    }
}
