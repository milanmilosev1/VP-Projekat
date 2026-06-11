using SmartGrid.Common;
using SmartGrid.Common.Validators;
using SmartGrid.Server.Analytics;
using SmartGrid.Server.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;

namespace SmartGrid.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
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
        private readonly CurrentAnalytics _currentAnalytics;
        private readonly VoltageAnalytics _voltageAnalytics;

        private static int _sampleCount = 0;
        private static int _acceptedSampleCount = 0;
        private static int _rejectedSampleCount = 0;

        private CSVWriter _measurementsWriter;
        private CSVWriter _rejectsWriter;
        private string _measurementsFilePath;
        private string _rejectsFilePath;

        public SessionControl()
        {
            try 
            {
                _iThreshold = double.Parse(ConfigurationManager.AppSettings["I_Threshold"]);
                _vThreshold = double.Parse(ConfigurationManager.AppSettings["V_Threshold"]);
                _averageDeviationThreshold = double.Parse(ConfigurationManager.AppSettings["AverageDeviationThreshold"]);
                _events = new SmartGridEventHub();
                _eventLogger = new SmartGridEventLogger(_events, ConfigurationManager.AppSettings["EventLogURL"]);
                _currentAnalytics = new CurrentAnalytics(_iThreshold, _averageDeviationThreshold);
                _voltageAnalytics = new VoltageAnalytics(_vThreshold);
                _measurementsFilePath = ConfigurationManager.AppSettings["SessionMeasurementsURL"];
                _rejectsFilePath = ConfigurationManager.AppSettings["RejectsURL"];  
            }
            catch 
            {
                _iThreshold = 15.0;
                _vThreshold = 230.0;
                _averageDeviationThreshold = 0.25;
                _events = new SmartGridEventHub();
                _eventLogger = new SmartGridEventLogger(_events, "server_events.log");
                _currentAnalytics = new CurrentAnalytics(_iThreshold, _averageDeviationThreshold);
                _voltageAnalytics = new VoltageAnalytics(_vThreshold);
                _measurementsFilePath = "measurements_session.csv";
                _rejectsFilePath = "rejects.csv";
            }
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public SessionResponse StartSession(MetaHeader metaHeader)
        {
            var metaValidator = new MetaValidator();
            
            metaValidator.Validate(metaHeader);

            _meta = metaHeader;
            _sampleMeasurements.Clear();
            _sampleCount = 0;
            _acceptedSampleCount = 0;
            _rejectedSampleCount = 0;
            _sessionActive = true;
            _events.RaiseTransferStarted($"Transfer started at {metaHeader.Timestamp:O}.");
            _currentAnalytics.Reset();
            _voltageAnalytics.Reset();

            string sessionId = metaHeader.Timestamp.ToString("yyyyMMdd_HHmmss");
            string sessionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions", sessionId);
            Directory.CreateDirectory(sessionDir);

            _measurementsFilePath = Path.Combine(sessionDir, _measurementsFilePath);
            _rejectsFilePath = Path.Combine(sessionDir, _rejectsFilePath);

            _measurementsWriter = new CSVWriter(_measurementsFilePath);
            _rejectsWriter = new CSVWriter(_rejectsFilePath);

            _measurementsWriter.WriteLine("Timestamp,Voltage,Current,PowerUsage,FaultIndicator,Frequency");
            _rejectsWriter.WriteLine("Reason,Timestamp,Voltage,Current,PowerUsage,FaultIndicator,Frequency");

            Console.WriteLine($"Session files created in: {sessionDir}");

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
                        _sampleCount++;
                        _rejectsWriter?.WriteReject("VoltageDeviation", sample);
                        return new SessionResponse { Status = Status.NAK, Progress = Progress.IN_PROGRESS, Message = $"Voltage deviates > {_averageDeviationThreshold * 100}% from average." };
                    }

                    if (Math.Abs(sample.Current - avgI) > _averageDeviationThreshold * avgI)
                    {
                        _events.RaiseWarningRaised("CurrentAverageDeviation", GetDirection(sample.Current, avgI), sample.Current, avgI, _averageDeviationThreshold, sample);
                        _rejectedSampleCount++;
                        _sampleCount++;
                        _rejectsWriter?.WriteReject("CurrentDeviation", sample);
                        return new SessionResponse { Status = Status.NAK, Progress = Progress.IN_PROGRESS, Message = $"Current deviates > {_averageDeviationThreshold * 100}% from average." };
                    }
                }

                //Odkometarisati za testiranje prekida komunikacije
                //if (_sampleCount == 50)
                //    throw new CommunicationException("Test test 1 2");

                _sampleMeasurements.Add(sample);
                _acceptedSampleCount++;

                _measurementsWriter?.WriteMeasurement(sample);

                _sampleCount++;
                _voltageAnalytics.Analyze(_sampleCount, sample.Voltage);
                _currentAnalytics.Analyze(_sampleCount, sample.Current);

                return new SessionResponse
                {
                    Status = Status.ACK,
                    Progress = Progress.IN_PROGRESS,
                    Message = $"Sample #{_sampleCount} accepted."
                };
            }
            catch (FaultException e)
            {
                _events.RaiseValidationWarning(e.Reason.ToString(), sample);
                _rejectedSampleCount++;
                
                _rejectsWriter?.WriteReject("ValidationError", sample);

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

            Console.WriteLine("-----------------------------------------------------------------------------------------");
            Console.WriteLine($"Measurements saved in: {_measurementsFilePath}");
            Console.WriteLine($"Rejects saved in: {_rejectsFilePath}");
            Console.WriteLine("-----------------------------------------------------------------------------------------");

            _measurementsWriter?.Dispose();
            _rejectsWriter?.Dispose();

            return new SessionResponse
            {
                Status = Status.ACK,
                Progress = Progress.COMPLETED,
                Message = $"Session ended successfully. Processed {_sampleCount} samples.\n\nAccepted: {_acceptedSampleCount}\nRejected: {_rejectedSampleCount}\n"
            };
        }

        private static string GetDirection(double actualValue, double expectedValue)
        {
            return actualValue > expectedValue ? "above expected" : "under expected";
        }

        [OperationBehavior(AutoDisposeParameters = true)]
        public AnalyticsReport GetAnalyticsReport()
        {
            var report = new AnalyticsReport
            {
                ProcessedSamples = _sampleCount,
                AcceptedSamples = _acceptedSampleCount,
                RejectedSamples = _rejectedSampleCount,
                AverageVoltage = _sampleMeasurements.Count > 0 ? _sampleMeasurements.Average(m => m.Voltage) : 0,
                AverageCurrent = _sampleMeasurements.Count > 0 ? _sampleMeasurements.Average(m => m.Current) : 0,
                AveragePowerUsage = _sampleMeasurements.Count > 0 ? _sampleMeasurements.Average(m => m.PowerUsage) : 0,
                AverageFrequency = _sampleMeasurements.Count > 0 ? _sampleMeasurements.Average(m => m.Frequency) : 0
            };

            report.Records.AddRange(_voltageAnalytics.GetRecords());
            report.Records.AddRange(_currentAnalytics.GetRecords());

            return report;
        }
    }
}
