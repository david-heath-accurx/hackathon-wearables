using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class HealthDataStorage(HealthApiDbContext db)
{
    public async Task SaveAsync(IEnumerable<HealthDataPoint> points, CancellationToken ct)
    {
        db.HealthDataPoints.AddRange(points);
        await db.SaveChangesAsync(ct);
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
