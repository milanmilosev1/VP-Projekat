using System.ServiceModel;

namespace SmartGrid.Common.Validators
{
    public class SampleValidator : IValidator<Measurement>
    {
        public void Validate(Measurement sample)
        {
            if (sample is null)
                throw new FaultException<DataFormatFault> (new DataFormatFault { Reason = "Sample payload is null." });

            if (sample.Voltage <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Voltage must be > 0." });

            if (sample.Current <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Current must be > 0." });

            if (sample.Frequency <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Frequency must be > 0." });
        }
    }
}
