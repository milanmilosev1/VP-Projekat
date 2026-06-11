using System;
using System.IO;

namespace SmartGrid.Server.Events
{
    public class SmartGridEventLogger
    {
        private readonly string _logPath;

        public SmartGridEventLogger(SmartGridEventHub eventHub, string logPath)
        {
            _logPath = logPath;

            eventHub.OnTransferStarted += LogTransferStarted;
            eventHub.OnSampleReceived += LogSampleReceived;
            eventHub.OnTransferCompleted += LogTransferCompleted;
            eventHub.OnWarningRaised += LogWarningRaised;
            eventHub.OnValidationWarningRaised += LogValidationWarning;
        }

        private void LogTransferStarted(object sender, TransferEventArgs e)
        {
            WriteLog($"[{e.Timestamp:O}] OnTransferStarted: {e.Message}");
        }

        private void LogSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            WriteLog($"[{e.Timestamp:O}] OnSampleReceived: Sample #{e.SequenceNumber}, V={e.Sample.Voltage}, I={e.Sample.Current}, F={e.Sample.Frequency}");
        }

        private void LogTransferCompleted(object sender, TransferEventArgs e)
        {
            WriteLog($"\n[{e.Timestamp:O}] OnTransferCompleted: {e.Message} Processed={e.ProcessedSamples}, Accepted={e.AcceptedSamples}, Rejected={e.RejectedSamples}");
        }

        private void LogWarningRaised(object sender, WarningRaisedEventArgs e)
        {
            WriteLog($"[{e.Timestamp:O}] OnWarningRaised: {e.WarningType}, {e.Direction}, Actual={e.ActualValue}, Expected={e.ExpectedValue}, Threshold={e.Threshold:P0}");
        }

        private void LogValidationWarning(object sender, WarningRaisedEventArgs e)
        {
            WriteLog($"[{e.Timestamp:O}] OnValidationWarningRaised: {e.WarningType}, Voltage={e.Sample.Voltage}, Current={e.Sample.Current}, Power Usage={e.Sample.PowerUsage}, Frequency={e.Sample.Frequency}");
        }

        private void WriteLog(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(_logPath, message + Environment.NewLine);
        }
    }
}
