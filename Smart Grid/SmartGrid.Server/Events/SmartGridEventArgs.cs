using SmartGrid.Common;
using System;

namespace SmartGrid.Server.Events
{
    public class TransferEventArgs : EventArgs
    {
        public string Message { get; }
        public int ProcessedSamples { get; }
        public int AcceptedSamples { get; }
        public int RejectedSamples { get; }
        public DateTime Timestamp { get; }

        public TransferEventArgs(string message, int processedSamples, int acceptedSamples, int rejectedSamples)
        {
            Message = message;
            ProcessedSamples = processedSamples;
            AcceptedSamples = acceptedSamples;
            RejectedSamples = rejectedSamples;
            Timestamp = DateTime.Now;
        }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public Measurement Sample { get; }
        public int SequenceNumber { get; }
        public DateTime Timestamp { get; }

        public SampleReceivedEventArgs(Measurement sample, int sequenceNumber)
        {
            Sample = sample;
            SequenceNumber = sequenceNumber;
            Timestamp = DateTime.Now;
        }
    }

    public class WarningRaisedEventArgs : EventArgs
    {
        public string WarningType { get; }
        public string Direction { get; }
        public double ActualValue { get; }
        public double ExpectedValue { get; }
        public double Threshold { get; }
        public Measurement Sample { get; }
        public DateTime Timestamp { get; }

        public WarningRaisedEventArgs(string warningType, string direction, double actualValue, double expectedValue, double threshold, Measurement sample)
        {
            WarningType = warningType;
            Direction = direction;
            ActualValue = actualValue;
            ExpectedValue = expectedValue;
            Threshold = threshold;
            Sample = sample;
            Timestamp = DateTime.Now;
        }
    }

    public class ValidationWarningEventArgs
    {
        public string WarningType { get; }
        public Measurement Sample { get; }
        public DateTime Timestamp { get; }

        public ValidationWarningEventArgs(string warningType, Measurement sample)
        {
            WarningType = warningType;
            Sample = sample;
            Timestamp = DateTime.Now;
        }
    }

    public class VoltageSpikeEventArgs
    {
        public DateTime Timestamp { get; }
        public double ActualVoltage { get; }
        public double PreviousVoltage { get; }
        public string Direction { get; }

        public VoltageSpikeEventArgs(double actualVoltage, double previousVoltage, string direction)
        {
            Timestamp = DateTime.Now;
            ActualVoltage = actualVoltage;
            PreviousVoltage = previousVoltage;
            Direction = direction;
        }
    }
}
