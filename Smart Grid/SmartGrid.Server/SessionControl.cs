using SmartGrid.Common;
using SmartGrid.Common.Validators;
using System.Collections.Generic;
using System.ServiceModel;

namespace SmartGrid.Server
{
    public class SessionControl : ISessionControl
    {
        private MetaHeader _meta;
        private readonly List<Measurement> _sampleMeasurements = new List<Measurement>();
        private bool _sessionActive = false;

        private readonly double _iThreshold;
        private readonly double _vThreshold;

        private static int _sampleCount = 0;

        [OperationBehavior(AutoDisposeParameters = true)]
        public SessionResponse StartSession(MetaHeader metaHeader)
        {
            var metaValidator = new MetaValidator();
            
            metaValidator.Validate(metaHeader);

            _meta = metaHeader;
            _sampleMeasurements.Clear();
            _sessionActive = true;

            return new SessionResponse
            {
                Status = Status.ACK,
                Progress = Progress.IN_PROGRESS,
                Message = $"Session started at {metaHeader.Timestamp:O}"
            };

        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public SessionResponse PushSample(Measurement sample)
        {
            if (!_sessionActive)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "No active session. Call StartSession first." });

            var sampleValidator = new SampleValidator();

            sampleValidator.Validate(sample);

            _sampleMeasurements.Add(sample);

            return new SessionResponse
            {
                Status = Status.ACK,
                Progress = Progress.IN_PROGRESS,
                Message = $"Sample #{_sampleCount++} accepted."
            };

        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public SessionResponse EndSession()
        {
            throw new System.NotImplementedException();
        }
    }
}
