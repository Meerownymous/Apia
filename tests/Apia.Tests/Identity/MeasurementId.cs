using Apia.Tests.Entities;

namespace Apia.Tests.Identity;

public sealed class MeasurementId : IIdentity<Measurement>
{
    public Guid Of(Measurement entity) => entity.MeasurementId;
}
