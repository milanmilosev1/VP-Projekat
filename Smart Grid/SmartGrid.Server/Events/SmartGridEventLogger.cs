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
            eventHub.OnVoltageSpike += LogVoltageSpike;
        }

        private void LogTransferStarted(object sender, TransferEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLog($"[{e.Timestamp:O}] OnTransferStarted: {e.Message}");
            Console.ResetColor();
        }

        private void LogSampleReceived(object sender, SampleReceivedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLog($"[{e.Timestamp:O}] OnSampleReceived: Sample #{e.SequenceNumber}, V={e.Sample.Voltage}, I={e.Sample.Current}, F={e.Sample.Frequency}");
            Console.ResetColor();
        }
            
        private void LogTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteLog($"\n[{e.Timestamp:O}] OnTransferCompleted: {e.Message} Processed={e.ProcessedSamples}, Accepted={e.AcceptedSamples}, Rejected={e.RejectedSamples}");
            Console.ResetColor();
        }

        private void LogWarningRaised(object sender, WarningRaisedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLog($"[{e.Timestamp:O}] OnWarningRaised: {e.WarningType}, {e.Direction}, Actual={e.ActualValue}, Expected={e.ExpectedValue}, Threshold={e.Threshold:P0}");
            Console.ResetColor();
        }

        private void LogValidationWarning(object sender, ValidationWarningEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLog($"[{e.Timestamp:O}] OnValidationWarningRaised: {e.WarningType.ToString()}, Voltage={e.Sample.Voltage}, Current={e.Sample.Current}, Power Usage={e.Sample.PowerUsage}, Frequency={e.Sample.Frequency}");
            Console.ResetColor();
        }

        private void LogVoltageSpike(object sender, VoltageSpikeEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            WriteLog($"[{e.Timestamp:O}] OnVoltageSpike: Direction: {e.Direction}, Actual Voltage: {e.ActualVoltage}, Previous Voltage: {e.PreviousVoltage}");
            Console.ResetColor();
        }

        private void WriteLog(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(_logPath, message + Environment.NewLine);
        }
    }
}
