using SmartGrid.Common;
using SmartGrid.Common.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Xml.Linq;

namespace SmartGrid.Server
{
    public class SessionControl : ISessionControl
    {
        private MetaHeader _meta;
        private readonly List<Measurement> _sampleMeasurements = new List<Measurement>();
        private bool _sessionActive = true;

        private readonly double _iThreshold;
        private readonly double _vThreshold;

        private static int _sampleCount = 0;
        private static int _acceptedSampleCount = 0;
        private static int _rejectedSampleCount = 0;

        public SessionControl()
        {
            try 
            {
                var doc = XDocument.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                var settings = doc.Descendants("appSettings").Descendants("add");
                _iThreshold = double.Parse(settings.FirstOrDefault(x => x.Attribute("key")?.Value == "I_threshold")?.Attribute("value")?.Value ?? "15.0", System.Globalization.CultureInfo.InvariantCulture);
                _vThreshold = double.Parse(settings.FirstOrDefault(x => x.Attribute("key")?.Value == "V_threshold")?.Attribute("value")?.Value ?? "230.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch 
            {
                _iThreshold = 15.0;
                _vThreshold = 230.0;
            }
        }

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
            try
            {
                if (!_sessionActive)
                    throw new FaultException<ValidationFault>(new ValidationFault { Reason = "No active session. Call StartSession first." });

                var sampleValidator = new SampleValidator(_iThreshold, _vThreshold);
                sampleValidator.Validate(sample);


                if (_sampleMeasurements.Count > 0)
                {
                    double avgV = _sampleMeasurements.Average(m => m.Voltage);
                    double avgI = _sampleMeasurements.Average(m => m.Current);

                    if (Math.Abs(sample.Voltage - avgV) > 0.25 * avgV)
                    {
                        _rejectedSampleCount++; 
                        return new SessionResponse { Status = Status.NAK, Progress = Progress.IN_PROGRESS, Message = "Voltage deviates > 25% from average." };
                    }

                    if (Math.Abs(sample.Current - avgI) > 0.25 * avgI)
                    {
                        _rejectedSampleCount++;
                        return new SessionResponse { Status = Status.NAK, Progress = Progress.IN_PROGRESS, Message = "Current deviates > 25% from average." };
                    }
                }

                //Odkometarisati za testiranje prekida komunikacije
                //if (_sampleCount == 50)
                //    throw new CommunicationException("Test test 1 2");

                _sampleMeasurements.Add(sample);
                _acceptedSampleCount++;

                return new SessionResponse
                {
                    Status = Status.ACK,
                    Progress = Progress.IN_PROGRESS,
                    Message = $"Sample #{++_sampleCount} accepted."
                };
            }
            catch (FaultException e)
            {
                Console.WriteLine($"[SAMPLE #{++_sampleCount}]" + e.Reason);
                _rejectedSampleCount++;
                return new SessionResponse
                {
                    Status = Status.NAK,
                    Progress = Progress.COMPLETED,
                    Message = $"Sample #{_sampleCount} not accepted."
                };
            }
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public SessionResponse EndSession()
        {
            if (!_sessionActive)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "No active session to end." }, new FaultReason("No active session to end."));

            _sessionActive = false;

            return new SessionResponse
            {
                Status = Status.ACK,
                Progress = Progress.COMPLETED,
                Message = $"Session ended successfully. Processed {_sampleCount} samples.\n\nAccepted: {_acceptedSampleCount}\nRejected: {_rejectedSampleCount}\n"
            };
        }
    }
}
