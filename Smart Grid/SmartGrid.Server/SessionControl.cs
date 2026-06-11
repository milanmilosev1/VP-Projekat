using SmartGrid.Common;
using SmartGrid.Common.Validators;
using SmartGrid.Server.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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
        private readonly double _averageDeviationThreshold;

        private readonly SmartGridEventHub _events;
        private readonly SmartGridEventLogger _eventLogger;

        private static int _sampleCount = 0;
        private static int _acceptedSampleCount = 0;
        private static int _rejectedSampleCount = 0;

        public SessionControl()
        {
            try 
            {
                _iThreshold = double.Parse(ConfigurationManager.AppSettings["I_Threshold"]);
                _vThreshold = double.Parse(ConfigurationManager.AppSettings["V_Threshold"]);
                _averageDeviationThreshold = double.Parse(ConfigurationManager.AppSettings["AverageDeviationThreshold"]);
                _events = new SmartGridEventHub();
                _eventLogger = new SmartGridEventLogger(_events, ConfigurationManager.AppSettings["EventLogURL"]);
            }
            catch 
            {
                _iThreshold = 15.0;
                _vThreshold = 230.0;
                _averageDeviationThreshold = 0.25;
                _events = new SmartGridEventHub();
                _eventLogger = new SmartGridEventLogger(_events, "server_events.log");
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
            _events.RaiseTransferStarted($"Transfer started at {metaHeader.Timestamp:O}.");

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

                _events.RaiseSampleReceived(sample, _sampleCount + 1);

                if (_sampleMeasurements.Count > 0)
                {
                    double avgV = _sampleMeasurements.Average(m => m.Voltage);
                    double avgI = _sampleMeasurements.Average(m => m.Current);

                    if (Math.Abs(sample.Voltage - avgV) > _averageDeviationThreshold * avgV)
                    {
                        _events.RaiseWarningRaised("VoltageAverageDeviation", GetDirection(sample.Voltage, avgV), sample.Voltage, avgV, _averageDeviationThreshold, sample);
                        _rejectedSampleCount++; 
                        return new SessionResponse { Status = Status.NAK, Progress = Progress.IN_PROGRESS, Message = "Voltage deviates > 25% from average." };
                    }

                    if (Math.Abs(sample.Current - avgI) > _averageDeviationThreshold * avgI)
                    {
                        _events.RaiseWarningRaised("CurrentAverageDeviation", GetDirection(sample.Current, avgI), sample.Current, avgI, _averageDeviationThreshold, sample);
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
            _events.RaiseTransferCompleted($"Transfer completed. Processed {_sampleCount} samples.", _sampleCount, _acceptedSampleCount, _rejectedSampleCount);

            return new SessionResponse
            {
                Status = Status.ACK,
                Progress = Progress.COMPLETED,
                Message = $"Session ended successfully. Processed {_sampleCount} samples.\n\nAccepted: {_acceptedSampleCount}\nRejected: {_rejectedSampleCount}\n"
            };
        }

        private static string GetSetting(IEnumerable<XElement> settings, string key, string defaultValue)
        {
            return settings.FirstOrDefault(x => x.Attribute("key")?.Value == key)?.Attribute("value")?.Value ?? defaultValue;
        }

        private static string GetDirection(double actualValue, double expectedValue)
        {
            return actualValue > expectedValue ? "iznad ocekivanog" : "ispod ocekivanog";
        }
    }
}
