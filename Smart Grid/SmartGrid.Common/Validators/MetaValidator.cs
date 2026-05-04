using System;
using System.ServiceModel;

namespace SmartGrid.Common.Validators
{
    public class MetaValidator : IValidator<MetaHeader>
    {
        public void Validate(MetaHeader meta)
        {
            if (meta is null)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Meta header is null." });

            if (meta.Voltage <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Voltage must be > 0." });

            if (meta.Current <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Current must be > 0." });

            if (meta.Frequency <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault { Reason = "Frequency must be > 0." });
        }
    }
}