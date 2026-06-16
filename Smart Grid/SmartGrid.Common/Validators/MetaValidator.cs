using System;
using System.ServiceModel;

namespace SmartGrid.Common.Validators
{
    public class MetaValidator : IValidator<MetaHeader>
    {
        public void Validate(MetaHeader meta)
        {
            if (meta is null)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Meta header is null." }, new FaultReason("Meta header is null."));

            if (meta?.Voltage == null || meta?.Current == null || meta?.PowerUsage == null || meta?.FaultIndicator == null || meta?.Frequency == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault { Reason = "Some of the fields are null." }, new FaultReason("Some of the fields are null."));

            if (meta.Voltage <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Voltage must be > 0." }, new FaultReason("Voltage must be > 0."));

            if (meta.Current <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Current must be > 0." }, new FaultReason("Current must be > 0."));

            if (meta.Frequency <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Frequency must be > 0." }, new FaultReason("Frequency must be > 0."));
        }
    }
}