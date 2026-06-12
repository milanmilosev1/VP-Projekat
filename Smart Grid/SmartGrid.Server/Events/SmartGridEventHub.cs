using SmartGrid.Common;

namespace SmartGrid.Server.Events
{
    public class SmartGridEventHub
    {
        public event TransferEventHandler OnTransferStarted;
        public event SampleReceivedEventHandler OnSampleReceived;
        public event TransferEventHandler OnTransferCompleted;
        public event WarningRaisedEventHandler OnWarningRaised;
        public event ValidationWarningEventHandler OnValidationWarningRaised;
        public event VoltageSpikeEventHandler OnVoltageSpike;

        public void RaiseTransferStarted(string message)
        {
            OnTransferStarted?.Invoke(this, new TransferEventArgs(message, 0, 0, 0));
        }

        public void RaiseSampleReceived(Measurement sample, int sequenceNumber)
        {
            OnSampleReceived?.Invoke(this, new SampleReceivedEventArgs(sample, sequenceNumber));
        }

        public void RaiseTransferCompleted(string message, int processedSamples, int acceptedSamples, int rejectedSamples)
        {
            OnTransferCompleted?.Invoke(this, new TransferEventArgs(message, processedSamples, acceptedSamples, rejectedSamples));
        }

        public void RaiseWarningRaised(string warningType, string direction, double actualValue, double expectedValue, double threshold, Measurement sample)
        {
            OnWarningRaised?.Invoke(this, new WarningRaisedEventArgs(warningType, direction, actualValue, expectedValue, threshold, sample));
        }

        public void RaiseValidationWarning(string warningType, Measurement sample)
        {
            OnValidationWarningRaised?.Invoke(this, new ValidationWarningEventArgs(warningType, sample));
        }

        public void RaiseVoltageSpike(double actualVoltage, double previousVoltage, string direction)
        {
            OnVoltageSpike?.Invoke(this, new VoltageSpikeEventArgs(actualVoltage, previousVoltage, direction));
        }
    }
}
