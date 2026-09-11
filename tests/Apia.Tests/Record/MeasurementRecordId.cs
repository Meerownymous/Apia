using Apia;

namespace Apia.Tests.Record;

public sealed class MeasurementRecordId : IIdentity<MeasurementRecord>
{
    public Guid Of(MeasurementRecord entity) => entity.MeasurementId;
}
