using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class HealthDataStorage(HealthApiDbContext db)
{
    public async Task SaveAsync(IEnumerable<HealthDataPoint> points, CancellationToken ct)
    {
        var pointList = points.ToList();

        var incoming = pointList
            .Where(p => p.ExternalId != null && p.DeviceId != null)
            .Select(p => (p.DeviceId!, p.ExternalId!))
            .ToList();

        if (incoming.Count > 0)
        {
            var deviceIds = incoming.Select(x => x.Item1).Distinct().ToList();
            var externalIds = incoming.Select(x => x.Item2).Distinct().ToList();

            var existing = await db.HealthDataPoints
                .Where(p => p.DeviceId != null && deviceIds.Contains(p.DeviceId)
                         && p.ExternalId != null && externalIds.Contains(p.ExternalId))
                .Select(p => new { p.DeviceId, p.ExternalId })
                .ToListAsync(ct);

            var existingKeys = existing.Select(p => (p.DeviceId!, p.ExternalId!)).ToHashSet();

            pointList = pointList
                .Where(p => p.ExternalId == null || !existingKeys.Contains((p.DeviceId!, p.ExternalId!)))
                .ToList();
        }

        if (pointList.Count > 0)
        {
            db.HealthDataPoints.AddRange(pointList);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<HealthDataPoint>> GetAsync(
        string userId,
        HealthMetricType? metricType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct
    )
    {
        var query = db.HealthDataPoints.Where(p => p.UserId == userId);

        if (metricType is not null)
            query = query.Where(p => p.MetricType == metricType);

        if (from is not null)
            query = query.Where(p => p.RecordedAt >= from);

        if (to is not null)
            query = query.Where(p => p.RecordedAt <= to);

        return await query.OrderByDescending(p => p.RecordedAt).ToListAsync(ct);
    }
}
