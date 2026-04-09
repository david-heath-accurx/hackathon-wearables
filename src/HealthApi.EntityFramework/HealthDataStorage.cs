using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class HealthDataStorage(HealthApiDbContext db)
{
    public async Task SaveAsync(IEnumerable<HealthDataPoint> points, CancellationToken ct)
    {
        var pointList = points.ToList();

        var incomingKeys = pointList
            .Select(p => (p.DeviceRegistrationId, p.ExternalId))
            .ToList();

        var registrationIds = incomingKeys.Select(x => x.DeviceRegistrationId).Distinct().ToList();
        var externalIds = incomingKeys.Select(x => x.ExternalId).Distinct().ToList();

        var existing = await db.HealthDataPoints
            .Where(p => registrationIds.Contains(p.DeviceRegistrationId)
                     && externalIds.Contains(p.ExternalId))
            .Select(p => new { p.DeviceRegistrationId, p.ExternalId })
            .ToListAsync(ct);

        var existingKeys = existing.Select(p => (p.DeviceRegistrationId, p.ExternalId)).ToHashSet();

        var newPoints = pointList
            .Where(p => !existingKeys.Contains((p.DeviceRegistrationId, p.ExternalId)))
            .ToList();

        if (newPoints.Count > 0)
        {
            db.HealthDataPoints.AddRange(newPoints);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<HealthDataPoint>> GetAsync(
        string patientIdentifier,
        HealthMetricType? metricType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct
    )
    {
        var query = db.HealthDataPoints
            .Include(p => p.DeviceRegistration)
            .Where(p => p.DeviceRegistration.Patient.PatientIdentifier == patientIdentifier);

        if (metricType is not null)
            query = query.Where(p => p.MetricType == metricType);

        if (from is not null)
            query = query.Where(p => p.RecordedAt >= from);

        if (to is not null)
            query = query.Where(p => p.RecordedAt <= to);

        return await query.OrderByDescending(p => p.RecordedAt).ToListAsync(ct);
    }

    public Task<List<string>> GetPatientsWithRecentDataAsync(DateTimeOffset since, CancellationToken ct)
    {
        return db.HealthDataPoints
            .Where(p => p.CreatedAt >= since)
            .Select(p => p.DeviceRegistration.Patient.PatientIdentifier)
            .Distinct()
            .ToListAsync(ct);
    }
}
