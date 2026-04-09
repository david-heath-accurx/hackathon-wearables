using HealthApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace HealthApi.EntityFramework;

public class DeviceRegistrationStorage(HealthApiDbContext db)
{
    public async Task<bool> RegisterAsync(string patientIdentifier, DateOnly dateOfBirth, string deviceId, CancellationToken ct)
    {
        var alreadyRegistered = await db.DeviceRegistrations
            .AnyAsync(r => r.DeviceId == deviceId, ct);

        if (alreadyRegistered)
            return false;

        db.DeviceRegistrations.Add(new DeviceRegistration
        {
            PatientIdentifier = patientIdentifier,
            DateOfBirth = dateOfBirth,
            DeviceId = deviceId,
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> IsRegisteredAsync(string patientIdentifier, DateOnly dateOfBirth, string deviceId, CancellationToken ct)
    {
        return await db.DeviceRegistrations.AnyAsync(
            r => r.DeviceId == deviceId
              && r.PatientIdentifier == patientIdentifier
              && r.DateOfBirth == dateOfBirth,
            ct
        );
    }

    public Task<DeviceRegistration?> GetByDeviceIdAsync(string deviceId, CancellationToken ct)
    {
        return db.DeviceRegistrations.FirstOrDefaultAsync(r => r.DeviceId == deviceId, ct);
    }

    public Task<bool> PatientExistsAsync(string patientIdentifier, DateOnly dateOfBirth, CancellationToken ct)
    {
        return db.DeviceRegistrations.AnyAsync(
            r => r.PatientIdentifier == patientIdentifier && r.DateOfBirth == dateOfBirth,
            ct
        );
    }

    public async Task<bool> DeregisterAllAsync(string patientIdentifier, CancellationToken ct)
    {
        var deviceIds = await db.DeviceRegistrations
            .Where(r => r.PatientIdentifier == patientIdentifier)
            .Select(r => r.DeviceId)
            .ToListAsync(ct);

        if (deviceIds.Count == 0)
            return false;

        await db.HealthDataPoints
            .Where(p => deviceIds.Contains(p.DeviceId!))
            .ExecuteDeleteAsync(ct);

        await db.DeviceRegistrations
            .Where(r => r.PatientIdentifier == patientIdentifier)
            .ExecuteDeleteAsync(ct);

        return true;
    }

    public async Task<bool> DeregisterAsync(string deviceId, CancellationToken ct)
    {
        var registration = await db.DeviceRegistrations
            .FirstOrDefaultAsync(r => r.DeviceId == deviceId, ct);

        if (registration is null)
            return false;

        await db.HealthDataPoints
            .Where(p => p.DeviceId == deviceId)
            .ExecuteDeleteAsync(ct);

        db.DeviceRegistrations.Remove(registration);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
