using SmartGrid.Common;
using System;

namespace SmartGrid.Server.Events
{
    public class TransferEventArgs : EventArgs
    {
        public TransferEventArgs(string message, int processedSamples, int acceptedSamples, int rejectedSamples)
        {
            Message = message;
            ProcessedSamples = processedSamples;
            AcceptedSamples = acceptedSamples;
            RejectedSamples = rejectedSamples;
            Timestamp = DateTime.Now;
        }

        public string Message { get; }
        public int ProcessedSamples { get; }
        public int AcceptedSamples { get; }
        public int RejectedSamples { get; }
        public DateTime Timestamp { get; }
    }

    public class SampleReceivedEventArgs : EventArgs
    {
        public SampleReceivedEventArgs(Measurement sample, int sequenceNumber)
        {
            Sample = sample;
            SequenceNumber = sequenceNumber;
            Timestamp = DateTime.Now;
        }

        public Measurement Sample { get; }
        public int SequenceNumber { get; }
        public DateTime Timestamp { get; }
    }

    public class WarningRaisedEventArgs : EventArgs
    {
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

        public string WarningType { get; }
        public string Direction { get; }
        public double ActualValue { get; }
        public double ExpectedValue { get; }
        public double Threshold { get; }
        public Measurement Sample { get; }
        public DateTime Timestamp { get; }
    }
}
