using System.ServiceModel;

namespace SmartGrid.Common
{
    [ServiceContract]
    public interface ISessionControl
    {

        [OperationContract]
        SessionResponse StartSession(MetaHeader metaHeader);

        [OperationContract]
        SessionResponse PushSample(Measurement sample);

        [OperationContract]
        SessionResponse EndSession();
    }
}
