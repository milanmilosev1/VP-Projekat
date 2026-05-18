using System.ServiceModel;

namespace SmartGrid.Common.Validators
{
    public class SampleValidator : IValidator<Measurement>
    {
        private readonly double _iThreshold;
        private readonly double _vThreshold;
        public SampleValidator(double iThreshold, double vThreshold)
        {
            _iThreshold = iThreshold;
            _vThreshold = vThreshold;
        }
        public void Validate(Measurement sample)
        {
            if (sample is null)
                throw new FaultException<DataFormatFault>(new DataFormatFault { Reason = "Sample payload is null." }, new FaultReason("Sample payload is null."));

            if (sample.Voltage <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Voltage must be > 0." }, new FaultReason("Voltage must be > 0."));

            if (sample.Current <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Current must be > 0." }, new FaultReason("Current must be > 0."));

            if (sample.Frequency <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Frequency must be > 0." }, new FaultReason("Frequency must be > 0."));

            if (sample.Current > _iThreshold)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Current exceeds threshold." }, new FaultReason("Current exceeds threshold."));

            if (sample.Voltage > _vThreshold)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Voltage exceeds threshold." }, new FaultReason("Voltage exceeds threshold."));
        }
    }
}
