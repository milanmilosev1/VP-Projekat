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

        //zadatak 6 - snimanje fajlovi
        private StreamWriter _measurementsWriter;
        private StreamWriter _rejectsWriter;
        private string _measurementsFilePath;
        private string _rejectsFilePath;

        //zadatak 10 - 
        private double _currentSum = 0.0;
        private int _currentCount = 0;
        private double _previousCurrent = double.NaN;



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
        //zadatak 6 - pomocna struktura,za formatiranje redova u csv 
        private string MeasurementToCsvRow(Measurement m)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5}",
                m.Timestamp.ToString("o"),
                m.Voltage,
                m.Current,
                m.PowerUsage,
                m.FaultIndicator,
                m.Frequency);
        }

        // Zadatak 10 - analitika nagle promene struje i odstupanja od proseka
        private void AnalyzeCurrent(int sampleIndex, double current)
        {
            _currentSum += current;
            _currentCount++;
            double imean = _currentSum / _currentCount;

            // Detekcija nagle promene - CurrentSpike
            if (!double.IsNaN(_previousCurrent))
            {
                double deltaI = current - _previousCurrent;
                if (Math.Abs(deltaI) > _iThreshold)
                {
                    string direction = deltaI > 0 ? "iznad ocekivanog" : "ispod ocekivanog";
                    Console.WriteLine($"[CURRENT SPIKE] Uzorak #{sampleIndex}: deltaI={deltaI:F4}, smer={direction}");
                }
            }

            // Detekcija odstupanja od tekuceg proseka +/-25%
            if (_currentCount > 1)
            {
                if (current < 0.75 * imean)
                    Console.WriteLine($"[OUT-OF-BAND] Uzorak #{sampleIndex}: I={current:F4} ispod ocekivane vrednosti (Imean={imean:F4})");
                else if (current > 1.25 * imean)
                    Console.WriteLine($"[OUT-OF-BAND] Uzorak #{sampleIndex}: I={current:F4} iznad ocekivane vrednosti (Imean={imean:F4})");
            }

            _previousCurrent = current;
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
            // Reset analitike (Zadatak 10)
            _currentSum = 0.0;
            _currentCount = 0;
            _previousCurrent = double.NaN;

            // Zadatak 6 - kreiranje foldera i fajlova sesije
            string sessionId = metaHeader.Timestamp.ToString("yyyyMMdd_HHmmss");
            string sessionDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions", sessionId);
            Directory.CreateDirectory(sessionDir);

            _measurementsFilePath = Path.Combine(sessionDir, "measurements_session.csv");
            _rejectsFilePath = Path.Combine(sessionDir, "rejects.csv");

            _measurementsWriter = new StreamWriter(new FileStream(_measurementsFilePath, FileMode.Create, FileAccess.Write, FileShare.None), System.Text.Encoding.UTF8);
            _rejectsWriter = new StreamWriter(new FileStream(_rejectsFilePath, FileMode.Create, FileAccess.Write, FileShare.None), System.Text.Encoding.UTF8);

            _measurementsWriter.WriteLine("Timestamp,Voltage,Current,PowerUsage,FaultIndicator,Frequency");
            _rejectsWriter.WriteLine("Reason,Timestamp,Voltage,Current,PowerUsage,FaultIndicator,Frequency");

            Console.WriteLine($" Fajlovi sesije kreirani u: {sessionDir}");

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
                        _rejectedSampleCount++;
                        //zadatak 6 - snimanje odbacenih uzoraka u poseban fajl
                        _rejectsWriter?.WriteLine("VoltageDeviation," + MeasurementToCsvRow(sample));
                        _rejectsWriter?.Flush();
                        return new SessionResponse { Status = Status.NAK, Progress = Progress.IN_PROGRESS, Message = "Voltage deviates > 25% from average." };
                    }

                    if (Math.Abs(sample.Current - avgI) > _averageDeviationThreshold * avgI)
                    {
                        _events.RaiseWarningRaised("CurrentAverageDeviation", GetDirection(sample.Current, avgI), sample.Current, avgI, _averageDeviationThreshold, sample);
                        _rejectedSampleCount++;
                        //zadatak 6 - snimanje odbacenih uzoraka u poseban fajl
                        _rejectsWriter?.WriteLine("CurrentDeviation," + MeasurementToCsvRow(sample));
                        _rejectsWriter?.Flush();
                        return new SessionResponse { Status = Status.NAK, Progress = Progress.IN_PROGRESS, Message = "Current deviates > 25% from average." };
                    }
                }

                //Odkometarisati za testiranje prekida komunikacije
                //if (_sampleCount == 50)
                //    throw new CommunicationException("Test test 1 2");

                _sampleMeasurements.Add(sample);
                _acceptedSampleCount++;
                // Zadatak 6 - upisivanje prihvacenih uzoraka u measurements_session.csv
                _measurementsWriter?.WriteLine(MeasurementToCsvRow(sample));
                _measurementsWriter?.Flush();

                // Zadatak 10 - analitika struje
                _sampleCount++;
                AnalyzeCurrent(_sampleCount, sample.Current);

                return new SessionResponse
                {
                    Status = Status.ACK,
                    Progress = Progress.IN_PROGRESS,
                    Message = $"Sample #{_sampleCount} accepted."
                };
            }
            catch (FaultException e)
            {
                Console.WriteLine($"[SAMPLE #{++_sampleCount}]" + e.Reason);
                _rejectedSampleCount++;
                // Zadatak 6 - upisi odbaceni uzorak u rejects.csv
                _rejectsWriter?.WriteLine("ValidationError," + MeasurementToCsvRow(sample));
                _rejectsWriter?.Flush();
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

            // zadatak 6 resetovanje broja odbacenih i prihvacenih uzoraka
            _measurementsWriter?.Flush();
            _measurementsWriter?.Dispose();
            _measurementsWriter = null;

            _rejectsWriter?.Flush();
            _rejectsWriter?.Dispose();
            _rejectsWriter = null;

            Console.WriteLine($" Merenja sacuvana u: {_measurementsFilePath}");
            Console.WriteLine($" Odbacena merenja sacuvana u: {_rejectsFilePath}");

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
